{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let rec = ci.get_record_definition(name).unwrap() %}
{%- let (ordered_fields, is_reordered) = rec.fields()|order_fields %}
{%- let rec_ffi_converter = rec|ffi_converter_name %}
{%- let self_lower_prefix = format!("{}.INSTANCE.Lower(this)", rec_ffi_converter) %}

{%- call cs::docstring(rec, 0) %}
{%- for field in ordered_fields %}
{%- match field.docstring() %}
{%- when Some with(docstring) %}
/// <param name="{{ field.name()|property_name }}">
{%- let docstring = textwrap::dedent(docstring) %}
{%- for line in docstring.lines() %}
/// {{ line.trim_end() }}
{%- endfor %}
/// </param>
{%- else %}
{%- endmatch %}
{%- endfor %}
{%- let (ordered_fields, is_reordered) = rec.fields()|order_fields %}
{%- if is_reordered %}
/// <remarks>
/// <b>UniFFI Warning:</b> Optional parameters have been reordered because
/// of a C# syntax limitation. Use named parameters for compatibility with
/// future ordering changes.
/// </remarks>
{%- endif %}
{{ config.access_modifier() }} record {{ type_name }} (
    {%- for field in ordered_fields %}
    {%- call cs::docstring(field, 4) %}
    {{ field|type_name(ci) }} {{ field.name()|property_name -}}
    {%- match field.default_value() %}
        {%- when Some with(literal) %} = {{ literal|render_literal(field, ci) }}
        {%- else %}
    {%- endmatch -%}
    {% if !loop.last %}, {% endif %}
    {%- endfor %}
) {% if contains_object_references %}: IDisposable {% endif %}{
    {%- if contains_object_references %}
    public void Dispose() {
        {%- call cs::destroy_fields(rec, "this") %}
    }
    {%- endif %}

    {%- if !rec.methods().is_empty() %}
    {%- call cs::value_type_methods(rec.methods(), self_lower_prefix) %}
    {%- endif %}

    {%- let uniffi_trait_methods = rec.uniffi_trait_methods() %}
    {%- call cs::value_type_uniffi_traits(uniffi_trait_methods, self_lower_prefix) %}
}

class {{ rec|ffi_converter_name }}: FfiConverterRustBuffer<{{ type_name }}> {
    public static {{ rec|ffi_converter_name }} INSTANCE = new {{ rec|ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        return new {{ type_name }}(
        {%- for field in rec.fields() %}
            {{ field.name()|property_name }}: {{ field|read_fn }}(stream){% if !loop.last %},{% endif%}
        {%- endfor %}
        );
    }

    public override int AllocationSize({{ type_name }} value) {
        return 0
        {%- for field in rec.fields() %}
            + {{ field|allocation_size_fn }}(value.{{ field.name()|property_name }})
        {%- endfor -%};
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        {%- for field in rec.fields() %}
            {{ field|write_fn }}(value.{{ field.name()|property_name }}, stream);
        {%- endfor %}
    }
}
