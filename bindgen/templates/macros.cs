{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{#
// Template to call into rust. Used in several places.
// Variable names in `arg_list_decl` should match up with arg lists
// passed to rust via `lower_arg_list`
#}

{%- macro to_ffi_call(func) -%}
    {%- match func.throws_type() %}
    {%- when Some with (e) %}
    _UniffiHelpers.RustCallWithError({{ e|ffi_converter_name}}.INSTANCE,
    {%- else %}
    _UniffiHelpers.RustCall(
    {%- endmatch %} (ref UniffiRustCallStatus _status) =>
    _UniFFILib.{{ func.ffi_func().name() }}({% call lower_arg_list(func) -%}{% if func.arguments().len() > 0 %},{% endif %} ref _status)
)
{%- endmacro -%}

{%- macro to_ffi_call_with_prefix(prefix, func) %}
    {%- match func.throws_type() %}
    {%- when Some with (e) %}
    _UniffiHelpers.RustCallWithError({{ e|ffi_converter_name}}.INSTANCE,
    {%- else %}
    _UniffiHelpers.RustCall(
    {%- endmatch %} (ref UniffiRustCallStatus _status) =>
    _UniFFILib.{{ func.ffi_func().name() }}(
        {{- prefix }}, {% call lower_arg_list(func) -%}{% if func.arguments().len() > 0 %},{% endif %} ref _status)
)
{%- endmacro -%}

{%- macro lower_arg_list(func) %}
    {%- for arg in func.arguments() %}
        {{- arg|lower_fn }}({{ arg.name()|var_name }})
        {%- if !loop.last %}, {% endif %}
    {%- endfor %}
{%- endmacro -%}

{#-
// Arglist as used in C# declarations of methods, functions and constructors.
// Note the var_name and type_name filters.
-#}

{% macro arg_list_decl(func) %}
    {%- for arg in func.arguments() -%}
        {{ arg|type_name(ci) }} {{ arg.name()|var_name -}}
        {%- match arg.default_value() %}
        {%- when Some with(literal) %} = {{ literal|render_literal(arg, ci) }}
        {%- else %}
        {%- endmatch %}
        {%- if !loop.last %}, {% endif -%}
    {%- endfor %}
{%- endmacro %}

{% macro arg_list_protocol(func) %}
    {%- for arg in func.arguments() -%}
        {{ arg|type_name(ci) }} {{ arg.name()|var_name -}}
        {%- if !loop.last %}, {% endif -%}
    {%- endfor %}
{%- endmacro %}
{#-
// Arglist as used in the _UniFFILib function declations.
// Note unfiltered name but ffi_type_name filters.
-#}
{%- macro arg_list_ffi_decl(func) %}
    {%- if func.is_object_free_function() %}
    IntPtr ptr,
    {%- else %}
    {%- for arg in func.arguments() %}
        {{- arg.type_().borrow()|ffi_type_name }} {{ arg.name()|var_name -}}{%- if !loop.last || func.has_rust_call_status_arg() -%},{%- endif -%}
    {%- endfor %}
    {%- endif %}
    {%- if func.has_rust_call_status_arg() %}ref UniffiRustCallStatus _uniffi_out_err{% endif %}
{%- endmacro -%}

// Macro for destroying fields
{%- macro destroy_fields(member, prefix) %}
    FFIObjectUtil.DisposeAll(
        {%- for field in member.fields() %}
            {{ prefix }}.{{ field.name()|var_name }}{% if !loop.last %},{% endif %}
        {%- endfor %});
{%- endmacro -%}

{%- macro ffi_function_definition(func) %}
fun {{ func.name()|fn_name }}(
    {%- call arg_list_ffi_decl(func) %}
){%- match func.return_type() -%}{%- when Some with (type_) %}: {{ type_|ffi_type_name }}{% when None %}: Unit{% endmatch %}
{% endmacro %}

{%- macro method_throws_annotation(throwable_type) %}
    {%- match throwable_type -%}
    {%- when Some with (throwable) %}
    /// <exception cref="{{ throwable|type_name(ci) }}"></exception>
    {%- else -%}
    {%- endmatch %}
{%- endmacro %}

{%- macro docstring_value(maybe_docstring, indent_spaces) %}
{%- match maybe_docstring %}
{%- when Some(docstring) %}
{{ docstring|docstring(indent_spaces) }}
{%- else %}
{%- endmatch %}
{%- endmacro %}

{%- macro return_type(func) -%}
{%- if func.is_async() -%}
{%- match func.return_type() -%}
{%- when Some(return_type) -%}
Task<{{ return_type|type_name }}>
{%- when None -%}
Task
{%- endmatch -%}
{%- else -%}
{%- match func.return_type() -%}
{%- when Some(return_type) -%}
{{ return_type|type_name }}
{%- when None -%}
void
{%- endmatch -%}
{%- endif -%}
{%- endmacro -%}

{#
// Break the following cycle, where `Rectangle` case name shadow top level `Rectangle` type name.
// If param type name matches the enum name, prefix the param type name with top level namespace.
// https://github.com/NordSecurity/uniffi-bindgen-cs/issues/60
//     public record Rectangle(double @width, double @height) { }
//     public record Shape
//     {                    ____________
//                          ∨          ∧
//         public record Rectangle(Rectangle @s) : Shape { }
//         public record Ellipse(Ellipse @s) : Shape { }
//     }
#}
{%- macro enum_parameter_type_name(param_type_name, enum_name) %}
{%- if param_type_name == enum_name %}{{ config.namespace() }}.{{ param_type_name }}
{%- else %}{{ param_type_name }}
{%- endif %}
{%- endmacro %}

{%- macro docstring(defn, indent_spaces) %}
{%- call docstring_value(defn.docstring(), indent_spaces) %}
{%- endmacro %}
