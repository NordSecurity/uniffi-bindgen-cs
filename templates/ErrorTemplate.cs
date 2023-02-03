{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let e = ci.get_error_definition(name).unwrap() %}

{% if e.is_flat() %}
public class {{ type_name }}: UniffiException {
    {{ type_name }}(string message): base(message) {}

    // Each variant is a nested class
    // Flat enums carries a string error message, so no special implementation is necessary.
    {% for variant in e.variants() -%}
    public class {{ variant.name()|exception_name }}: {{ type_name }} {
        public {{ variant.name()|exception_name }}(string message): base(message) {}
    }
    {% endfor %}
}
{%- else %}
public class {{ type_name }}: UniffiException{% if contains_object_references %}, IDisposable {% endif %} {
    // Each variant is a nested class
    {% for variant in e.variants() -%}
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
            case {{ type_name }}.{{ variant.name()|class_name }} variant_value:
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
{%- endif %}

class {{ e|ffi_converter_name }} : FfiConverterRustBuffer<{{ type_name }}>, CallStatusErrorHandler<{{ type_name }}> {
    public static {{ e|ffi_converter_name }} INSTANCE = new {{ e|ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        var value = stream.ReadInt();
        {% if e.is_flat() %}
            switch (value) {
                {%- for variant in e.variants() %}
                case {{ loop.index }}: return new {{ type_name }}.{{ variant.name()|exception_name }}({{ TypeIdentifier::String.borrow()|read_fn }}(stream));
                {%- endfor %}
                default:
                    throw new InternalException(String.Format("invalid enum value '{}' in {{ e|ffi_converter_name }}.Read()", value));
            }
        {% else %}
            switch (value) {
                {%- for variant in e.variants() %}
                case {{ loop.index }}:
                    return new {{ type_name }}.{{ variant.name()|exception_name }}(
                        {%- for field in variant.fields() %}
                        {{ field|read_fn }}(stream){% if !loop.last %},{% endif %}
                        {%- endfor %});
                {%- endfor %}
                default:
                    throw new InternalException(String.Format("invalid enum value '{}' in {{ e|ffi_converter_name }}.Read()", value));
            }

        {%- endif %}
    }

    public override int AllocationSize({{ type_name }} value) {
        throw new InternalException("Writing Errors is not supported");
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        throw new InternalException("Writing Errors is not supported");
    }
}
