{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}
{{- self.add_import("System.Threading")}}

{%- let obj = ci.get_object_definition(name).unwrap() %}
{%- if self.include_once_check("ObjectRuntime.cs") %}{% include "ObjectRuntime.cs" %}{% endif %}

{%- call cs::docstring(obj, 0) %}
{{ config.access_modifier() }} interface I{{ type_name }}
    {%- for tm in obj.uniffi_traits() -%}
    {%- match tm -%}
    {%- when UniffiTrait::Eq { eq, ne } -%}
    : IEquatable<{{type_name}}>
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
{{ config.access_modifier() }} class {{ type_name }}: FFIObject, I{{ type_name }} {
    public {{ type_name }}(IntPtr pointer) : base(pointer) {}

    {%- match obj.primary_constructor() %}
    {%- when Some with (cons) %}
    {%- call cs::docstring(cons, 4) %}
    public {{ type_name }}({% call cs::arg_list_decl(cons) -%}) :
        this({% call cs::to_ffi_call(cons) %}) {}
    {%- when None %}
    {%- endmatch %}

    protected override void FreeRustArcPtr() {
        _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            _UniFFILib.{{ obj.ffi_object_free().name() }}(this.pointer, ref status);
        });
    }

    protected override void CloneRustArcPtr() {
        _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            _UniFFILib.{{ obj.ffi_object_clone().name() }}(this.pointer, ref status);
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
    public bool Equals({{type_name}}? other)
    {
        if (other is null) return false;
        return CallWithPointer(thisPtr => {{ Type::Boolean.borrow()|lift_fn }}({%- call cs::to_ffi_call_with_prefix("thisPtr", eq) %}));
    }
    public override bool Equals(object? obj)
    {
        if (obj is null || !(obj is {{type_name}})) return false;
        return Equals(obj as {{type_name}});
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
    public static {{ type_name }} {{ cons.name()|fn_name }}({% call cs::arg_list_decl(cons) %}) {
        return new {{ type_name }}({% call cs::to_ffi_call(cons) %});
    }
    {% endfor %}
    {% endif %}
}

class {{ obj|ffi_converter_name }}: FfiConverter<{{ type_name }}, IntPtr> {
    public static {{ obj|ffi_converter_name }} INSTANCE = new {{ obj|ffi_converter_name }}();

    public override IntPtr Lower({{ type_name }} value) {
        return value.CallWithPointer(thisPtr => thisPtr);
    }

    public override {{ type_name }} Lift(IntPtr value) {
        return new {{ type_name }}(value);
    }

    public override {{ type_name }} Read(BigEndianStream stream) {
        return Lift(new IntPtr(stream.ReadLong()));
    }

    public override int AllocationSize({{ type_name }} value) {
        return 8;
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        stream.WriteLong(Lower(value).ToInt64());
    }
}
