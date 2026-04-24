{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- macro callback_arg_list(callback) %}
    {%- match callback.return_type() %}
    {%- when Some with (_type) %}
    {# Method returns a value - include all args including uniffiOutReturn #}
    {%- for arg in callback.arguments() %}
    {%- if !loop.first %}, {% endif %}{{ arg.type_().borrow()|ffi_type_name }} {{ arg.name()|var_name }}
    {%- endfor %}
    {%- when None %}
    {# Void method - filter out uniffiOutReturn #}
    {%- for arg in callback.arguments() %}
    {%- if arg.name() != "uniffiOutReturn" %}
    {%- if !loop.first %}, {% endif %}{{ arg.type_().borrow()|ffi_type_name }} {{ arg.name()|var_name }}
    {%- endif %}
    {%- endfor %}
    {%- endmatch %}
    {%- if callback.has_rust_call_status_arg() %}{% if callback.arguments().len() > 0 %}, {% endif %}ref UniffiRustCallStatus _uniffi_out_err{% endif %}
{%- endmacro %}

// This is an implementation detail that will be called internally by the public API.
#if NET8_0_OR_GREATER
static partial class _UniFFILib {
#else
static class _UniFFILib {
#endif
    {%- for def in ci.ffi_definitions() %}
    {%- match def %}
    {%- when FfiDefinition::CallbackFunction(callback) %}
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate {% call cs::ffi_return_type(callback) %} {{ callback.name()|ffi_callback_name }}(
        {% call callback_arg_list(callback) %}
    );
    {%- when FfiDefinition::Struct(ffi_struct) %}
    [StructLayout(LayoutKind.Sequential)]
    public struct {{ ffi_struct.name()|ffi_struct_name }}
    {
        {%- for field in ffi_struct.fields() %}
        public {{ field.type_().borrow()|ffi_type_name }} {{ field.name()|var_name }};
        {%- endfor %}
    }
    {%- when FfiDefinition::Function(_) %}
    {# functions are handled below #}
    {%- endmatch %}
    {%- endfor %}

    static _UniFFILib() {
        _UniFFILib.uniffiCheckContractApiVersion();
        {%- if !config.omit_checksums %}
        _UniFFILib.uniffiCheckApiChecksums();
        {%- endif %}
        {% let initialization_fns = self.initialization_fns() %}
        {% for func in initialization_fns -%}
        {{ func }}();
        {% endfor -%}
    }

    {% for func in ci.iter_ffi_function_definitions() -%}
#if NET8_0_OR_GREATER
    [LibraryImport("{{ config.cdylib_name() }}")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static partial
#else
    [DllImport("{{ config.cdylib_name() }}", CallingConvention = CallingConvention.Cdecl)]
    public static extern
#endif
    {% match func.return_type() -%}{%- when Some with (type_) %} {{ type_.borrow()|ffi_type_name }}{% when None %} void{% endmatch %} {{ func.name() }}(
        {%- call cs::arg_list_ffi_decl(func) %}
    );

    {% endfor %}

    static void uniffiCheckContractApiVersion() {
        var scaffolding_contract_version = _UniFFILib.{{ ci.ffi_uniffi_contract_version().name() }}();
        if ({{ ci.uniffi_contract_version() }} != scaffolding_contract_version) {
            throw new UniffiContractVersionException($"{{ config.namespace() }}: uniffi bindings expected version `{{ ci.uniffi_contract_version() }}`, library returned `{scaffolding_contract_version}`");
        }
    }

    {%- if !config.omit_checksums %}
    static void uniffiCheckApiChecksums() {
        {%- for (name, expected_checksum) in ci.iter_checksums() %}
        {
            var checksum = _UniFFILib.{{ name }}();
            if (checksum != {{ expected_checksum }}) {
                throw new UniffiContractChecksumException($"{{ config.namespace() }}: uniffi bindings expected function `{{ name }}` checksum `{{ expected_checksum }}`, library returned `{checksum}`");
            }
        }
        {%- endfor %}
    }
    {%- endif %}
}
