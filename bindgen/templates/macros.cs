{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{#
// Template to call into rust. Used in several places.
// Variable names in `arg_list_decl` should match up with arg lists
// passed to rust via `_arg_list_ffi_call`
#}

{%- macro to_ffi_call(func) -%}
    {%- match func.throws_type() %}
    {%- when Some with (e) %}
    _UniffiHelpers.RustCallWithError({{ e|ffi_converter_name}}.INSTANCE,
    {%- else %}
    _UniffiHelpers.RustCall(
    {%- endmatch %} (ref RustCallStatus _status) =>
    _UniFFILib.{{ func.ffi_func().name() }}({% call _arg_list_ffi_call(func) -%}{% if func.arguments().len() > 0 %},{% endif %} ref _status)
)
{%- endmacro -%}

{%- macro to_ffi_call_with_prefix(prefix, func) %}
    {%- match func.throws_type() %}
    {%- when Some with (e) %}
    _UniffiHelpers.RustCallWithError({{ e|ffi_converter_name}}.INSTANCE,
    {%- else %}
    _UniffiHelpers.RustCall(
    {%- endmatch %} (ref RustCallStatus _status) =>
    _UniFFILib.{{ func.ffi_func().name() }}(
        {{- prefix }}, {% call _arg_list_ffi_call(func) -%}{% if func.arguments().len() > 0 %},{% endif %} ref _status)
)
{%- endmacro -%}

{%- macro _arg_list_ffi_call(func) %}
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
        {{ arg|type_name }} {{ arg.name()|var_name -}}
        {%- match arg.default_value() %}
        {%- when Some with(literal) %} = {{ literal|render_literal(arg) }}
        {%- else %}
        {%- endmatch %}
        {%- if !loop.last %}, {% endif -%}
    {%- endfor %}
{%- endmacro %}

{% macro arg_list_protocol(func) %}
    {%- for arg in func.arguments() -%}
        {{ arg|type_name }} {{ arg.name()|var_name -}}
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
        {{- arg.type_().borrow()|ffi_type_name }} {{ arg.name()|var_name -}},
    {%- endfor %}
    {%- endif %}
    ref RustCallStatus _uniffi_out_err
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
    /// <exception cref="{{ throwable|type_name }}"></exception>
    {%- else -%}
    {%- endmatch %}
{%- endmacro %}

{%- macro docstring(defn, indent_spaces) %}
{%- match defn.docstring() %}
{%- when Some(docstring) %}
{{ docstring|docstring(indent_spaces) }}
{%- else %}
{%- endmatch %}
{%- endmacro %}
