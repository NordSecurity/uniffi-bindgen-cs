{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let cbi = ci.get_callback_interface_definition(name).unwrap() %}
{%- let type_name = cbi|type_name(ci) %}
{%- let foreign_callback = format!("ForeignCallback{}", type_name) %}

{%- let vtable = cbi.vtable() %}
{%- let vtable_methods = cbi.vtable_methods() %}

{%- let ffi_converter_var = format!("{}.INSTANCE", ffi_converter_name) %}
{%- let ffi_init_callback = cbi.ffi_init_callback() %}

{%- call cs::docstring(cbi, 0) %}
{{ config.access_modifier() }} interface {{ type_name }} {
    {%- for meth in cbi.methods() %}
    {%- call cs::docstring(meth, 4) %}
    {%- call cs::method_throws_annotation(meth.throws_type()) %}
    {%- match meth.return_type() %}
    {%- when Some with (return_type) %}
    {{ return_type|type_name(ci) }} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %});
    {%- else %}
    void {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %});
    {%- endmatch %}
    {%- endfor %}
}

{% if self.include_once_check("ConcurrentHandleMap.cs") %}{% include "ConcurrentHandleMap.cs" %}{% endif %}

// The ForeignCallback that is passed to Rust.
class {{ foreign_callback }} {
    {%- for (ffi_callback, meth) in vtable_methods.iter() %}
    static {% call cs::ffi_return_type(ffi_callback) %} {{ meth.name()|fn_name }}({% call cs::arg_list_ffi_decl_xx(ffi_callback) %}) {
        // Add logic
        Console.WriteLine("I'm here! Trying to call a callback from callback interface");
        var handle = @uniffiHandle;
        if ({{ ffi_converter_var }}.handleMap.TryGet(handle, out var uniffiObject)) {
            // TODO: Handle errors better
            {%- match meth.throws_type() %}
            {%- when Some with (error_type) %}
            try {
            {%- when None %}
            {%- endmatch %}

            {%- match meth.return_type() %}
            {%- when Some with (return_type) %}
            var result =
            {%- when None %}
            {%- endmatch %}
            uniffiObject.{{ meth.name()|fn_name }}(
                {%- for arg in meth.arguments() %}
                {{ arg|lift_fn }}({{ arg.name()|var_name }}){%- if !loop.last %}, {% endif -%}
                {%- endfor %});

            {%- match meth.return_type() %}
            {%- when Some with (return_type) %}
            @uniffiOutReturn = {{ return_type|ffi_converter_name }}.INSTANCE.Lower(result);
            {%- when None %}
            {%- endmatch%}

            {%- match meth.throws_type() %}
            {%- when Some with (error_type) %}
            }
            catch ({{ error_type|type_name(ci) }} e) {
                _uniffi_out_err.code = 1;
                _uniffi_out_err.error_buf = {{ error_type|ffi_converter_name }}.INSTANCE.Lower(e);
            }
            catch {
                _uniffi_out_err.code = 2;
            }
            {%- when None %}

            {%- endmatch %}

        } else {
            // TODO: Panic
        }
    }
    {%- endfor %}

    static void UniffiFree(ulong handle) {
        {{ ffi_converter_name }}.INSTANCE.handleMap.Remove(handle);
    }

    {%- for (ffi_callback, meth) in vtable_methods.iter() %}
    {%- let fn_type = format!("_UniFFILib.UniffiCallbackInterface{}Method", type_name) %}
    static {{ fn_type }}{{ loop.index0 }} _m{{ loop.index0 }} = new {{ fn_type }}{{ loop.index0 }}({{ meth.name()|fn_name }});
    {%- endfor %}
    static _UniFFILib.UniffiCallbackInterfaceFree _f0 = new _UniFFILib.UniffiCallbackInterfaceFree(UniffiFree);


    public static _UniFFILib.{{ vtable|ffi_type_name }} _vtable = new _UniFFILib.{{ vtable|ffi_type_name }} {
        {%- for (ffi_callback, meth) in vtable_methods.iter() %}
        {%- let fn_type = format!("_UniFFILib.{}Method", type_name) %}
        {{ meth.name()|var_name() }} = Marshal.GetFunctionPointerForDelegate(_m{{ loop.index0 }}),
        {%- endfor %}
        @uniffiFree = Marshal.GetFunctionPointerForDelegate(_f0)
    };

    public static void Register() {
        Console.WriteLine("I'm here! Register Vtable in callback interface");
        _UniFFILib.{{ ffi_init_callback.name() }}(ref {{ foreign_callback }}._vtable);
    }
}

// The ffiConverter which transforms the Callbacks in to Handles to pass to Rust.
class {{ ffi_converter_name }}: FfiConverter<{{ type_name }}, ulong> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public ConcurrentHandleMap<{{ type_name }}> handleMap = new ConcurrentHandleMap<{{ type_name }}>();

    public override ulong Lower({{ type_name }} value) {
        return handleMap.Insert(value);
    }

    public override {{ type_name }} Lift(ulong value) {
        if (handleMap.TryGet(value, out var uniffiCallback)) {
            return uniffiCallback;
        } else {
            throw new InternalException($"No callback in handlemap '{value}'");
        }
    }

    public override {{ type_name }} Read(BigEndianStream stream) {
        return Lift(stream.ReadULong());
    }

    public override int AllocationSize({{ type_name }} value) {
        return 8;
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        stream.WriteULong(Lower(value));
    }
}
