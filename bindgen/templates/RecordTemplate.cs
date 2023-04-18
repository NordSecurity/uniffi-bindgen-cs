{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let rec = ci.get_record_definition(name).unwrap() %}

{%- call cs::docstring(rec, 0) %}
public record {{ type_name }} (
    {%- for field in rec.fields() %}
    {%- call cs::docstring(field, 4) %}
    {{ field|type_name }} {{ field.name()|var_name -}}
    {%- match field.default_value() %}
        {%- when Some with(literal) %} = {{ literal|render_literal(field) }}
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
}

class {{ rec|ffi_converter_name }}: FfiConverterRustBuffer<{{ type_name }}> {
    public static {{ rec|ffi_converter_name }} INSTANCE = new {{ rec|ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        return new {{ type_name }}(
        {%- for field in rec.fields() %}
            {{ field|read_fn }}(stream){% if !loop.last %},{% endif%}
        {%- endfor %}
        );
    }

    public override int AllocationSize({{ type_name }} value) {
        return
        {%- for field in rec.fields() %}
            {{ field|allocation_size_fn }}(value.{{ field.name()|var_name }}){% if !loop.last %} +{% endif%}
        {%- endfor -%};
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        {%- for field in rec.fields() %}
            {{ field|write_fn }}(value.{{ field.name()|var_name }}, stream);
        {%- endfor %}
    }
}
