{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{#
// C# doesn't support enums with associated data. Use regular `enum` for flat
// enums, and `record` for enums with associated data.
#}

{%- if e.is_flat() %}

{%- call cs::docstring(e, 0) %}
{{ config.access_modifier() }} enum {{ type_name }}: int {
    {% for variant in e.variants() -%}
    {%- call cs::docstring(variant, 4) %}
    {{ variant.name()|enum_variant }}{% if !loop.last %},{% endif %}
    {%- endfor %}
}

{%- let enum_ffi_converter = e|ffi_converter_name %}
{%- let flat_self_lower = format!("{}.INSTANCE.Lower(self_)", enum_ffi_converter) %}
{%- let uniffi_trait_methods = e.uniffi_trait_methods() %}
{%- if !e.methods().is_empty() || uniffi_trait_methods.display_fmt.is_some() || uniffi_trait_methods.debug_fmt.is_some() %}
{{ config.access_modifier() }} static class {{ type_name }}Extensions {
    {%- call cs::flat_enum_extension_methods(e.methods(), type_name, flat_self_lower) %}
    {%- call cs::flat_enum_uniffi_traits(uniffi_trait_methods, type_name, flat_self_lower) %}
}
{%- endif %}

class {{ e|ffi_converter_name }}: FfiConverterRustBuffer<{{ type_name }}> {
    public static {{ e|ffi_converter_name }} INSTANCE = new {{ e|ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        var value = stream.ReadInt() - 1;
        if (Enum.IsDefined(typeof({{ type_name }}), value)) {
            return ({{ type_name }})value;
        } else {
            throw new InternalException(String.Format("invalid enum value '{0}' in {{ e|ffi_converter_name }}.Read()", value));
        }
    }

    public override int AllocationSize({{ type_name }} value) {
        return 4;
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        stream.WriteInt((int)value + 1);
    }
}

{% else %}

{%- call cs::docstring(e, 0) %}
{%- let enum_ffi_converter = e|ffi_converter_name %}
{%- let self_lower_prefix = format!("{}.INSTANCE.Lower(this)", enum_ffi_converter) %}
{{ config.access_modifier() }} record {{ type_name }}{% if contains_object_references %}: IDisposable {% endif %} {
    {% for variant in e.variants() -%}
    {%- call cs::docstring(variant, 4) %}
    {% if !variant.has_fields() -%}
    public record {{ variant.name()|class_name(ci) }}: {{ type_name }} {}
    {% else -%}
    public record {{ variant.name()|class_name(ci) }} (
        {%- for field in variant.fields() %}
        {%- let field_name = field.name()|or_pos_var(loop.index)|property_name %}
        {% call cs::enum_parameter_type_name(field|type_name(ci), variant.name()|class_name(ci)) %} {% call cs::enum_field_name(field_name, variant.name()|class_name(ci)) %}{% if !loop.last %},{% endif %}
        {%- endfor %}
    ) : {{ type_name }} {}
    {%- endif %}
    {% endfor %}

    {% if contains_object_references %}
    public void Dispose() {
        switch (this) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant.name()|class_name(ci) }} variant_value:
                {%- if variant.has_fields() %}
                {% call cs::destroy_fields(variant, "variant_value") %}
                {%- endif %}
                break;
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{0}' in {{ type_name }}.Dispose()", this));
        }
    }
    {% endif %}

    {%- if !e.methods().is_empty() %}
    {%- call cs::value_type_methods(e.methods(), self_lower_prefix) %}
    {%- endif %}

    {%- let uniffi_trait_methods = e.uniffi_trait_methods() %}
    {%- call cs::value_type_uniffi_traits(uniffi_trait_methods, self_lower_prefix) %}
}

class {{ e|ffi_converter_name }} : FfiConverterRustBuffer<{{ type_name }}>{
    public static FfiConverterRustBuffer<{{ type_name }}> INSTANCE = new {{ e|ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        var value = stream.ReadInt();
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ loop.index }}:
                return new {{ type_name }}.{{ variant.name()|class_name(ci) }}(
                    {%- for field in variant.fields() %}
                    {{ field|read_fn }}(stream){% if !loop.last %},{% endif %}
                    {%- endfor %}
                );
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{0}' in {{ e|ffi_converter_name }}.Read()", value));
        }
    }

    public override int AllocationSize({{ type_name }} value) {
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant.name()|class_name(ci) }} variant_value:
                return 4
                    {%- for field in variant.fields() %}
                    {%- let field_name = field.name()|or_pos_var(loop.index)|property_name %}
                    + {{ field|allocation_size_fn }}(variant_value.{% call cs::enum_field_name(field_name, variant.name()|class_name(ci)) %})
                    {%- endfor %};
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{0}' in {{ e|ffi_converter_name }}.AllocationSize()", value));
        }
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        switch (value) {
            {%- for variant in e.variants() %}
            case {{ type_name }}.{{ variant.name()|class_name(ci) }} variant_value:
                stream.WriteInt({{ loop.index }});
                {%- for field in variant.fields() %}
                {%- let field_name = field.name()|or_pos_var(loop.index)|property_name %}
                {{ field|write_fn }}(variant_value.{% call cs::enum_field_name(field_name, variant.name()|class_name(ci)) %}, stream);
                {%- endfor %}
                break;
            {%- endfor %}
            default:
                throw new InternalException(String.Format("invalid enum value '{0}' in {{ e|ffi_converter_name }}.Write()", value));
        }
    }
}

{% endif %}
