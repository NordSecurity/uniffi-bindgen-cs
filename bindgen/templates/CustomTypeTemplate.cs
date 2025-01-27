{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- match config.custom_types.get(name.as_str())  %}
{%- when None %}
{#- Define the type using typealiases to the builtin #}
/**
 * Typealias from the type name used in the UDL file to the builtin type.  This
 * is needed because the UDL type name is used in function/method signatures.
 * It's also what we have an external type that references a custom type.
 */
{{- self.add_type_alias(name, builtin|type_name_custom(ci)) }}
{{- self.add_type_alias(ffi_converter_name, builtin|ffi_converter_name) }}

{%- when Some with (config) %}

{%- let ffi_type_name = builtin|ffi_type|ffi_type_name %}

{# When the config specifies a different type name, create a typealias for it #}
{%- match config.type_name %}
{%- when Some(concrete_type_name) %}
/**
 * Typealias from the type name used in the UDL file to the custom type.  This
 * is needed because the UDL type name is used in function/method signatures.
 * It's also what we have an external type that references a custom type.
 */
{{- self.add_type_alias(name, concrete_type_name) }}
{%- else %}
{%- endmatch %}

{%- match config.imports %}
{%- when Some(imports) %}
{%- for import_name in imports %}
{{ self.add_import(import_name) }}
{%- endfor %}
{%- else %}
{%- endmatch %}

class {{ ffi_converter_name }}: FfiConverter<{{ name }}, {{ ffi_type_name }}> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override {{ name }} Lift({{ ffi_type_name }} value) {
        var builtinValue = {{ builtin|lift_fn }}(value);
        return {{ config.into_custom.render("builtinValue") }};
    }

    public override {{ ffi_type_name }} Lower({{ name }} value) {
        var builtinValue = {{ config.from_custom.render("value") }};
        return {{ builtin|lower_fn }}(builtinValue);
    }

    public override {{ name }} Read(BigEndianStream stream) {
        var builtinValue = {{ builtin|read_fn }}(stream);
        return {{ config.into_custom.render("builtinValue") }};
    }

    public override int AllocationSize({{ name }} value) {
        var builtinValue = {{ config.from_custom.render("value") }};
        return {{ builtin|allocation_size_fn }}(builtinValue);
    }

    public override void Write({{ name }} value, BigEndianStream stream) {
        var builtinValue = {{ config.from_custom.render("value") }};
        {{ builtin|write_fn }}(builtinValue, stream);
    }
}
{%- endmatch %}
