{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}
{{- self.add_import("System.Threading")}}

{%- let obj = ci.get_object_definition(name).unwrap() %}
{%- let (interface_name, impl_name) = obj|object_names(ci) %}
{%- if self.include_once_check("ObjectRuntime.cs") %}{% include "ObjectRuntime.cs" %}{% endif %}

{%- call cs::docstring(obj, 0) %}
{{ config.access_modifier() }} interface {{ interface_name }}
    {%- for tm in obj.uniffi_traits() -%}
    {%- match tm -%}
    {%- when UniffiTrait::Eq { eq, ne } -%}
    : IEquatable<{{impl_name}}>
    {%- else -%}
    {%- endmatch -%}
    {%- endfor %} {
    {%- for meth in obj.methods() %}
    {%- call cs::docstring(meth, 4) %}
    {%- call cs::method_throws_annotation(meth.throws_type()) %}
    {%  call cs::return_type(meth) %} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %});
    {%- endfor %}
}

{%- call cs::docstring(obj, 0) %}
{{ config.access_modifier() }} class {{ impl_name }}: FFIObject, {{ interface_name }} {
    public {{ impl_name }}(IntPtr pointer) : base(pointer) {}

    {%- match obj.primary_constructor() %}
    {%- when Some with (cons) %}
    {%- call cs::docstring(cons, 4) %}
    public {{ impl_name }}({% call cs::arg_list_decl(cons) -%}) :
        this({% call cs::to_ffi_call(cons) %}) {}
    {%- when None %}
    {%- endmatch %}

    protected override void FreeRustArcPtr() {
        _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            _UniFFILib.{{ obj.ffi_object_free().name() }}(this.pointer, ref status);
        });
    }

    protected override IntPtr CloneRustArcPtr() {
        return _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            return _UniFFILib.{{ obj.ffi_object_clone().name() }}(this.pointer, ref status);
        });
    }

    {% for meth in obj.methods() -%}
    {%- call cs::docstring(meth, 4) %}
    {%- call cs::method_throws_annotation(meth.throws_type()) %}
    {%- if meth.is_async() %}
    public async {% call cs::return_type(meth) %} {{ meth.name()|fn_name }}({%- call cs::arg_list_decl(meth) -%}) {
        {%- if meth.return_type().is_some() %}
        return {% endif %}await _UniFFIAsync.UniffiRustCallAsync(
            // Get rust future
            CallWithPointer(thisPtr => {
                return _UniFFILib.{{ meth.ffi_func().name()  }}(thisPtr{%- if meth.arguments().len() > 0 %}, {% endif -%}{% call cs::lower_arg_list(meth) %});
            }),
            // Poll
            (IntPtr future, IntPtr continuation) => _UniFFILib.{{ meth.ffi_rust_future_poll(ci) }}(future, continuation),
            // Complete
            (IntPtr future, ref UniffiRustCallStatus status) => {
                {%- if meth.return_type().is_some() %}
                return {% endif %}_UniFFILib.{{ meth.ffi_rust_future_complete(ci) }}(future, ref status);
            },
            // Free
            (IntPtr future) => _UniFFILib.{{ meth.ffi_rust_future_free(ci) }}(future),
            {%- match meth.return_type() %}
            {%- when Some(return_type) %}
            // Lift
            (result) => {{ return_type|lift_fn }}(result),
            {% else %}
            {% endmatch -%}
            // Error
            {%- match meth.throws_type() %}
            {%- when Some(e)  %}
            {{ e|ffi_converter_name }}.INSTANCE
            {%- when None %}
            NullCallStatusErrorHandler.INSTANCE
            {% endmatch %}
       );
    }

    {%- else %}

    {%- match meth.return_type() -%}
    {%- when Some with (return_type) %}
    public {{ return_type|type_name(ci) }} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %}) {
        return CallWithPointer(thisPtr => {{ return_type|lift_fn }}({%- call cs::to_ffi_call_with_prefix("thisPtr", meth) %}));
    }

    {%- when None %}
    public void {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %}) {
        CallWithPointer(thisPtr => {%- call cs::to_ffi_call_with_prefix("thisPtr", meth) %});
    }
    {% endmatch %}
    {% endif %}
    {% endfor %}

    {%- for tm in obj.uniffi_traits() -%}
    {%- match tm %}
    {%- when UniffiTrait::Display { fmt } %}
    public override string ToString() {
        return CallWithPointer(thisPtr => {{ Type::String.borrow()|lift_fn  }}({%- call cs::to_ffi_call_with_prefix("thisPtr", fmt)  %}));
    }
    {%- when UniffiTrait::Eq { eq, ne } %}
    public bool Equals({{ impl_name }}? other)
    {
        if (other is null) return false;
        return CallWithPointer(thisPtr => {{ Type::Boolean.borrow()|lift_fn }}({%- call cs::to_ffi_call_with_prefix("thisPtr", eq) %}));
    }
    public override bool Equals(object? obj)
    {
        if (obj is null || !(obj is {{ impl_name }})) return false;
        return Equals(obj as {{ impl_name }});
    }
    {%- when UniffiTrait::Hash  { hash }  %}
    public override int GetHashCode() { 
        return (int)CallWithPointer(thisPtr => {{ Type::UInt64.borrow()|lift_fn }}({%- call cs::to_ffi_call_with_prefix("thisPtr", hash)  %}));
    }
    {%- else %}
    {%- endmatch %}
    {%- endfor %}

    {% if !obj.alternate_constructors().is_empty() -%}
    {% for cons in obj.alternate_constructors() -%}
    {%- call cs::docstring(cons, 4) %}
    {%- call cs::method_throws_annotation(cons.throws_type()) %}
    public static {{ impl_name }} {{ cons.name()|fn_name }}({% call cs::arg_list_decl(cons) %}) {
        return new {{ impl_name }}({% call cs::to_ffi_call(cons) %});
    }
    {% endfor %}
    {% endif %}
}

{%- let ffi_converter_type = obj|ffi_converter_name %}
{%- let ffi_converter_var = format!("{}.INSTANCE", ffi_converter_type)%}

{%- if obj.has_callback_interface() %}
{%- let vtable = obj.vtable().expect("Trait interface should have a vtable") %}
{%- let vtable_methods = obj.vtable_methods() %}
{%- let ffi_init_callback = obj.ffi_init_callback() %}

{%- let trait_impl=format!("UniffiCallbackInterface{}", name) %}

class {{ trait_impl }} {
    {%- for (ffi_callback, meth) in vtable_methods.iter() %}
    static {% call cs::ffi_return_type(ffi_callback) %} {{ meth.name()|fn_name }}({% call cs::arg_list_ffi_decl_xx(ffi_callback) %}) {
        // Add logic
        Console.WriteLine("I'm here! Trying to call a callback");
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
            catch {
                _uniffi_out_err.code = 1;
            }
            {%- when None %}

            {%- endmatch %}

        } else {
            // TODO: Panic
        }
    }
    {%- endfor %}

    static void UniffiFree(ulong @handle) {
        {{ ffi_converter_var }}.handleMap.Remove(@handle);
    }

    {%- for (ffi_callback, meth) in vtable_methods.iter() %}
    {%- let fn_type = format!("_UniFFILib.{}Method", trait_impl) %}
    static {{ fn_type }}{{ loop.index0 }} _m{{ loop.index0 }} = new {{ fn_type }}{{ loop.index0 }}({{ meth.name()|fn_name }});
    {%- endfor %}
    static _UniFFILib.UniffiCallbackInterfaceFree _f0 = new _UniFFILib.UniffiCallbackInterfaceFree(UniffiFree);

    public static _UniFFILib.{{ vtable|ffi_type_name }} _vtable = new _UniFFILib.{{ vtable|ffi_type_name }} {
        {%- for (ffi_callback, meth) in vtable_methods.iter() %}
        {%- let fn_type = format!("_UniFFILib.{}Method", trait_impl) %}
        {{ meth.name()|var_name() }} = Marshal.GetFunctionPointerForDelegate(_m{{ loop.index0 }}),
        {%- endfor %}
        @uniffiFree = Marshal.GetFunctionPointerForDelegate(_f0)
    };

    public static void Register() {
        Console.WriteLine("I'm here! Register Vtable for object trait");
        _UniFFILib.{{ ffi_init_callback.name() }}(ref {{ trait_impl }}._vtable);
    }
}

{% if self.include_once_check("ConcurrentHandleMap.cs") %}{% include "ConcurrentHandleMap.cs" %}{% endif %}

{%- endif %}

class {{ ffi_converter_type }}: FfiConverter<{{ impl_name }}, IntPtr> {
    {%- if obj.has_callback_interface() %}
    public ConcurrentHandleMap<{{ impl_name }}> handleMap = new ConcurrentHandleMap<{{ impl_name }}>();
    {%- endif %}
    
    public static {{ ffi_converter_type }} INSTANCE = new {{ ffi_converter_type }}();


    public override IntPtr Lower({{ impl_name }} value) {
        {%- if obj.has_callback_interface() %}
        return (IntPtr)handleMap.Insert(value);
        {%- else %}
        return value.CallWithPointer(thisPtr => thisPtr);
        {%- endif %}
    }

    public override {{ impl_name }} Lift(IntPtr value) {
        return new {{ impl_name }}(value);
    }

    public override {{ impl_name }} Read(BigEndianStream stream) {
        return Lift(new IntPtr(stream.ReadLong()));
    }

    public override int AllocationSize({{ impl_name }} value) {
        return 8;
    }

    public override void Write({{ impl_name }} value, BigEndianStream stream) {
        stream.WriteLong(Lower(value).ToInt64());
    }
}
