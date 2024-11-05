{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let obj = ci.get_object_definition(name).unwrap() %}
{%- let safe_handle_type = format!("{}SafeHandle", type_name) %}
{%- if self.include_once_check("ObjectRuntime.cs") %}{% include "ObjectRuntime.cs" %}{% endif %}

{%- call cs::docstring(obj, 0) %}
{{ config.access_modifier() }} interface I{{ type_name }}
    {%- for tm in obj.uniffi_traits() -%}
    {%- if loop.first -%} : {% endif -%}
    {%- match tm -%}
    {%- when UniffiTrait::Eq { eq, ne } -%}
    IEquatable<{{type_name}}>
    {%- else -%}
    {%- endmatch -%}
    {%- endfor %} {
    {% for meth in obj.methods() -%}
    {%- call cs::docstring(meth, 4) %}
    {%- call cs::method_throws_annotation(meth.throws_type()) %}
    {% match meth.return_type() -%} {%- when Some with (return_type) -%} {{ return_type|type_name }} {%- when None %}void{%- endmatch %} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %});
    {% endfor %}
}

{{ config.access_modifier() }} class {{ safe_handle_type }}: FFISafeHandle {
    public {{ safe_handle_type }}(): base() {
    }
    public {{ safe_handle_type }}(IntPtr pointer): base(pointer) {
    }
    override protected bool ReleaseHandle() {
        _UniffiHelpers.RustCall((ref RustCallStatus status) => {
            _UniFFILib.{{ obj.ffi_object_free().name() }}(this.handle, ref status);
        });
        return true;
    }
}

{%- call cs::docstring(obj, 0) %}
{{ config.access_modifier() }} class {{ type_name }}: FFIObject<{{ safe_handle_type }}>, I{{ type_name }} {
    public {{ type_name }}({{ safe_handle_type }} pointer): base(pointer) {}

    {%- match obj.primary_constructor() %}
    {%- when Some with (cons) %}
    {%- call cs::docstring(cons, 4) %}
    public {{ type_name }}({% call cs::arg_list_decl(cons) -%}) :
        this({% call cs::to_ffi_call(cons) %}) {}
    {%- when None %}
    {%- endmatch %}

    {% for meth in obj.methods() -%}
    {%- call cs::docstring(meth, 4) %}
    {%- call cs::method_throws_annotation(meth.throws_type()) %}
    {%- match meth.return_type() -%}

    {%- when Some with (return_type) %}
    public {{ return_type|type_name }} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %}) {
        return {{ return_type|lift_fn }}({%- call cs::to_ffi_call_with_prefix("this.GetHandle()", meth) %});
    }

    {%- when None %}
    public void {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %}) {
        {%- call cs::to_ffi_call_with_prefix("this.GetHandle()", meth) %};
    }
    {% endmatch %}
    {% endfor %}

    {%- for tm in obj.uniffi_traits() -%}
    {%- match tm %}
    {%- when UniffiTrait::Display { fmt } %}
    public override string ToString() {
        return {{ Type::String.borrow()|lift_fn }}({%- call cs::to_ffi_call_with_prefix("this.GetHandle()", fmt) %});
    }
    {%- when UniffiTrait::Eq { eq, ne } %}
    public bool Equals({{type_name}}? other)
    {
        if (other is null) return false;
        return {{ Type::Boolean.borrow()|lift_fn }}({%- call cs::to_ffi_call_with_prefix("this.GetHandle()", eq) %});
    }
    public override bool Equals(object? obj)
    {
        if (obj is null || !(obj is {{type_name}})) return false;
        return Equals(obj as {{type_name}});
    }
    public static bool operator == ({{type_name}}? a, {{type_name}}? b)
    {
        if (a is null || b is null) return Object.Equals(a, b);
        return a.Equals(b);
    }
    public static bool operator != ({{type_name}}? a, {{type_name}}? b)
    {
        if (a is null || b is null) return !Object.Equals(a, b);
        return !(a.Equals(b));
    }
    {%- when UniffiTrait::Hash  { hash }  %}
    public override int GetHashCode() { 
        return (int){{ Type::UInt64.borrow()|lift_fn }}({%- call cs::to_ffi_call_with_prefix("this.GetHandle()", hash)  %});
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

class {{ obj|ffi_converter_name }}: FfiConverter<{{ type_name }}, {{ safe_handle_type }}> {
    public static {{ obj|ffi_converter_name }} INSTANCE = new {{ obj|ffi_converter_name }}();

    public override {{ safe_handle_type }} Lower({{ type_name }} value) {
        return value.GetHandle();
    }

    public override {{ type_name }} Lift({{ safe_handle_type }} value) {
        return new {{ type_name }}(value);
    }

    public override {{ type_name }} Read(BigEndianStream stream) {
        return Lift(new {{ safe_handle_type }}(new IntPtr(stream.ReadLong())));
    }

    public override int AllocationSize({{ type_name }} value) {
        return 8;
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        stream.WriteLong(Lower(value).DangerousGetRawFfiValue().ToInt64());
    }
}
