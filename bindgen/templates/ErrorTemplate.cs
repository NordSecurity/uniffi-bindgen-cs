{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let type_name = type_|type_name(ci) %}
{%- let ffi_converter_name = type_|ffi_converter_name %}
{%- let canonical_type_name = type_|canonical_name %}

{% if e.is_flat() %}
{%- call cs::docstring(e, 0) %}
{{ config.access_modifier() }} class {{ type_name }}: UniffiException {
    {{ type_name }}(string message): base(message) {}

    // Each variant is a nested class
    // Flat enums carries a string error message, so no special implementation is necessary.
    {% for variant in e.variants() -%}
    {%- call cs::docstring(variant, 4) %}
    public class {{ variant|error_variant_name }}: {{ type_name }} {
        public {{ variant|error_variant_name }}(string message): base(message) {}
    }
    {% endfor %}
}

class {{ ffi_converter_name }} : FfiConverterRustBuffer<{{ type_name }}>, CallStatusErrorHandler<{{ type_name }}> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        var value = stream.ReadInt();
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ loop.index }}: return new {{ type_name }}.{{ variant|error_variant_name }}({{ Type::String.borrow()|read_fn }}(stream));
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid error value '{0}' in {{ ffi_converter_name }}.Read()", value));
        }
    }

    public override int AllocationSize({{ type_name }} value) {
        return 4 + {{ Type::String.borrow()|allocation_size_fn }}(value.Message);
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant|error_variant_name }}:
                stream.WriteInt({{ loop.index }});
                break;
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid error value '{0}' in {{ ffi_converter_name }}.Write()", value));
        }
    }
}

{%- else %}
{%- call cs::docstring(e, 0) %}
{{ config.access_modifier() }} class {{ type_name }}: UniffiException{% if contains_object_references %}, IDisposable {% endif %} {
    {{ type_name }}() : base() {}
    {{ type_name }}(String @Message) : base(@Message) {}

    // Each variant is a nested class
    {% for variant in e.variants() -%}
    {%- call cs::docstring(variant, 4) %}
    {% if !variant.has_fields() -%}
    {%- if variant.name() == "InnerException" %}
    public new class {{ variant|error_variant_name }} : {{ type_name }} {
        public {{ variant|error_variant_name }}() : base() {}
    }
    {%- else %}
    public class {{ variant|error_variant_name }} : {{ type_name }} {
        public {{ variant|error_variant_name }}() : base() {}
    }
    {%- endif %}
    {% else %}
    {%- if variant.name() == "InnerException" %}
    public new class {{ variant|error_variant_name }} : {{ type_name }} {
    {%- else %}
    public class {{ variant|error_variant_name }} : {{ type_name }} {
    {%- endif %}
        // Members
        {%- for field in variant.fields() %}
        {%- let field_name = field.name()|or_pos_var(loop.index)|var_name %}
        public {% call cs::enum_parameter_type_name(field|type_name(ci), variant|error_variant_name) %} {{ field_name }};
        {%- endfor %}

        // Constructor
        public {{ variant|error_variant_name }}(
                {%- for field in variant.fields() %}
                {%- let field_name = field.name()|or_pos_var(loop.index)|var_name %}
                {% call cs::enum_parameter_type_name(field|type_name(ci), variant|error_variant_name) %} {{ field_name }}{% if loop.last %}{% else %}, {% endif %}
                {%- endfor %}) : base(
                {%- for field in variant.fields() -%}
                {%- let field_name = field.name()|or_pos_var(loop.index)|var_name %}
                "{{ field_name }}" + "=" + {{ field_name }}{%- if loop.last -%}{%- else -%} + ", " + {%- endif -%}
                {%- endfor -%}
                ) {
            {%- for field in variant.fields() %}
            {%- let field_name = field.name()|or_pos_var(loop.index)|var_name %}

            this.{{ field_name }} = {{ field_name }};
            {%- endfor %}
        }
    }
    {%- endif %}
    {% endfor %}

    {% if contains_object_references %}
    public void Dispose() {
        switch (this) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant|error_variant_name }} variant_value:
                {%- if variant.has_fields() %}
                {% call cs::destroy_fields(variant, "variant_value") %}
                {%- endif %}
                break;
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid error value '{0}' in {{ type_name }}.Dispose()", this));
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
                return new {{ type_name }}.{{ variant|error_variant_name }}(
                    {%- for field in variant.fields() %}
                    {{ field|read_fn }}(stream){% if !loop.last %},{% endif %}
                    {%- endfor %});
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid error value '{0}' in {{ ffi_converter_name }}.Read()", value));
        }
    }

    public override int AllocationSize({{ type_name }} value) {
        switch (value) {
            {%- for variant in e.variants() %}

            case {{ type_name }}.{{ variant|error_variant_name }} variant_value:
                return 4
                    {%- for field in variant.fields() %}
                    {%- let field_name = field.name()|or_pos_var(loop.index)|var_name %}
                    + {{ field|allocation_size_fn }}(variant_value.{{ field_name }})
                    {%- endfor %};
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid error value '{0}' in {{ ffi_converter_name }}.AllocationSize()", value));
        }
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant|error_variant_name }} variant_value:
                stream.WriteInt({{ loop.index }});
                {%- for field in variant.fields() %}
                {%- let field_name = field.name()|or_pos_var(loop.index)|var_name %}
                {{ field|write_fn }}(variant_value.{{ field_name }}, stream);
                {%- endfor %}
                break;
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid error value '{0}' in {{ ffi_converter_name }}.Write()", value));
        }
    }
}
{%- endif %}
