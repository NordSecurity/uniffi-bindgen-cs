{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let cbi = ci.get_callback_interface_definition(name).unwrap() %}
{%- let type_name = cbi|type_name(ci) %}
{%- let callback_impl_name = type_name|ffi_callback_impl %}

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

{% include "CallbackInterfaceImpl.cs" %}

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
