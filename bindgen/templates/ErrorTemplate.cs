{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let type_name = type_|as_error|type_name %}
{%- let ffi_converter_name = type_|as_error|ffi_converter_name %}
{%- let canonical_type_name = type_|as_error|canonical_name %}

{% if e.is_flat() %}
{%- call cs::docstring(e, 0) %}
public class {{ type_name }}: UniffiException {
    {{ type_name }}(string message): base(message) {}

    // Each variant is a nested class
    // Flat enums carries a string error message, so no special implementation is necessary.
    {% for variant in e.variants() -%}
    {%- call cs::docstring(variant, 4) %}
    public class {{ variant.name()|exception_name }}: {{ type_name }} {
        public {{ variant.name()|exception_name }}(string message): base(message) {}
    }
    {% endfor %}
}

class {{ ffi_converter_name }} : FfiConverterRustBuffer<{{ type_name }}>, CallStatusErrorHandler<{{ type_name }}> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        var value = stream.ReadInt();
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ loop.index }}: return new {{ type_name }}.{{ variant.name()|exception_name }}({{ Type::String.borrow()|read_fn }}(stream));
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{}' in {{ ffi_converter_name }}.Read()", value));
        }
    }

    public override int AllocationSize({{ type_name }} value) {
        return 4 + {{ Type::String.borrow()|allocation_size_fn }}(value.Message);
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant.name()|exception_name }}:
                stream.WriteInt({{ loop.index }});
                break;
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{}' in {{ ffi_converter_name }}.Write()", value));
        }
    }
}

{%- else %}
{%- call cs::docstring(e, 0) %}
public class {{ type_name }}: UniffiException{% if contains_object_references %}, IDisposable {% endif %} {
    // Each variant is a nested class
    {% for variant in e.variants() -%}
    {%- call cs::docstring(variant, 4) %}
    {% if !variant.has_fields() -%}
    public class {{ variant.name()|exception_name }} : {{ type_name }} {}
    {% else %}
    public class {{ variant.name()|exception_name }} : {{ type_name }} {
        // Members
        {%- for field in variant.fields() %}
        public {{ field|type_name}} {{ field.name()|var_name }};
        {%- endfor %}

        // Constructor
        public {{ variant.name()|exception_name }}(
                {%- for field in variant.fields() %}
                {{ field|type_name}} {{ field.name()|var_name }}{% if loop.last %}{% else %}, {% endif %}
                {%- endfor %}) {
            {%- for field in variant.fields() %}
            this.{{ field.name()|var_name }} = {{ field.name()|var_name }};
            {%- endfor %}
        }
    }
    {%- endif %}
    {% endfor %}

    {% if contains_object_references %}
    public void Dispose() {
        switch (this) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant.name()|exception_name }} variant_value:
                {%- if variant.has_fields() %}
                {% call cs::destroy_fields(variant, "variant_value") %}
                {%- endif %}
                break;
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{}' in {{ type_name }}.Dispose()", this));
        }
    }
    {% endif %}
}

class {{ ffi_converter_name }} : FfiConverterRustBuffer<{{ type_name }}>, CallStatusErrorHandler<{{ type_name }}> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        var value = stream.ReadInt();
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ loop.index }}:
                return new {{ type_name }}.{{ variant.name()|exception_name }}(
                    {%- for field in variant.fields() %}
                    {{ field|read_fn }}(stream){% if !loop.last %},{% endif %}
                    {%- endfor %});
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{}' in {{ ffi_converter_name }}.Read()", value));
        }
    }

    public override int AllocationSize({{ type_name }} value) {
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant.name()|exception_name }} variant_value:
                return 4
                    {%- for field in variant.fields() %}
                    + {{ field|allocation_size_fn }}(variant_value.{{ field.name()|var_name }})
                    {%- endfor %};
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{}' in {{ ffi_converter_name }}.AllocationSize()", value));
        }
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant.name()|exception_name }} variant_value:
                stream.WriteInt({{ loop.index }});
                {%- for field in variant.fields() %}
                {{ field|write_fn }}(variant_value.{{ field.name()|var_name }}, stream);
                {%- endfor %}
                break;
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{}' in {{ ffi_converter_name }}.Write()", value));
        }
    }
}
{%- endif %}
