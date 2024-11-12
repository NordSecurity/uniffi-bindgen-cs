{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- call cs::docstring(func, 4) %}
{%- call cs::method_throws_annotation(func.throws_type()) %}
{%- if func.is_async() %}
   public static async {% call cs::return_type(func) %} {{ func.name()|fn_name }}({%- call cs::arg_list_decl(func) -%}) 
   {
        {%- match func.return_type() %}
        {%- when Some(return_type) %}
        return 
        {% else %}
        {% endmatch -%}
         await _UniFFIAsync.UniffiRustCallAsync(
            // Get rust future
            _UniffiHelpers.RustCall((ref RustCallStatus status) => {
                return _UniFFILib.{{ func.ffi_func().name() }}({% call cs::lower_arg_list(func) %});
            }),
            // Poll
            (IntPtr future, IntPtr continuation) => _UniffiHelpers.RustCall((ref RustCallStatus _status) => {
                _UniFFILib.{{ func.ffi_rust_future_poll(ci) }}(future, continuation);
            }),
            // Complete
            (IntPtr future, RustCallStatus status) =>  _UniffiHelpers.RustCall((ref RustCallStatus _status) => {
                {%- match func.return_type() %}
                {%- when Some(return_type) %}
                return 
                {% else %}
                {% endmatch -%}
                 _UniFFILib.{{ func.ffi_rust_future_complete(ci) }}(future, ref status);
            }),
            // Free
            (IntPtr future) =>  _UniffiHelpers.RustCall((ref RustCallStatus _status) => {
                _UniFFILib.{{ func.ffi_rust_future_free(ci) }}(future);
            }),
            {%- match func.return_type() %}
            {%- when Some(return_type) %}
            // Lift
            (result) => {{ return_type|lift_fn }}(
                {%- if return_type|is_pointer %}
                ({{return_type|type_name}}SafeHandle)
                {%- endif -%} result
            ),
            {% else %}
            {% endmatch %}
            // Error
            {%- match func.throws_type() %}
            {%- when Some(e)  %}
            {{ e|as_error|error_ffi_converter_name }}.INSTANCE
            {%- when None %}
            NullCallStatusErrorHandler.INSTANCE
            {% endmatch %}
       );
   }
{%- else %}
{%- match func.return_type() -%}
{%- when Some with (return_type) %}
    public static {{ return_type|type_name }} {{ func.name()|fn_name }}({%- call cs::arg_list_decl(func) -%}) {
        return {{ return_type|lift_fn }}({% call cs::to_ffi_call(func) %});
    }
{% when None %}
    public static void {{ func.name()|fn_name }}({% call cs::arg_list_decl(func) %}) {
        {% call cs::to_ffi_call(func) %};
    }
{% endmatch %}
{% endif  %}
