{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

// This is an implementation detail that will be called internally by the public API.
static class _UniFFILib {
    {%- for def in ci.ffi_definitions() %}
    {%- match def %}
    {%- when FfiDefinition::CallbackFunction(callback) %}
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate {% if callback.return_type().is_some() %}{{ callback.return_type().unwrap()|ffi_type_name }}{% else %}void{% endif %} {{ callback.name()|ffi_callback_name }}(
        {%- for arg in callback.arguments() %}
        {{ arg.type_().borrow()|ffi_type_name }} {{ arg.name().borrow()|var_name }}{%- if !loop.last || callback.has_rust_call_status_arg() -%},{%- endif -%}
        {%- endfor %}
        {%- if callback.has_rust_call_status_arg() %}
        UniffiRustCallStatus uniffiCallStatus
        {%- endif %}
    );
    {%- when FfiDefinition::Struct(ffi_struct) %}
    [StructLayout(LayoutKind.Sequential)]
    public struct {{ ffi_struct.name()|ffi_struct_name }}
    {
        {%- for field in ffi_struct.fields() %}
        {{ field.type_().borrow()|ffi_type_name }} {{ field.name()|var_name }};
        {%- endfor %}
    }
    {%- when FfiDefinition::Function(_) %}
    {# functions are handled below #}
    {%- endmatch %}
    {%- endfor %}

    static _UniFFILib() {
        _UniFFILib.uniffiCheckContractApiVersion();
        _UniFFILib.uniffiCheckApiChecksums();
        {% let initialization_fns = self.initialization_fns() %}
        {% for fn in initialization_fns -%}
        {{ fn }}();
        {% endfor -%}
    }

    {% for func in ci.iter_ffi_function_definitions() -%}
    [DllImport("{{ config.cdylib_name() }}")]
    public static extern {%- match func.return_type() -%}{%- when Some with (type_) %} {{ type_.borrow()|ffi_type_name }}{% when None %} void{% endmatch %} {{ func.name() }}(
        {%- call cs::arg_list_ffi_decl(func) %}
    );

    {% endfor %}

    static void uniffiCheckContractApiVersion() {
        var scaffolding_contract_version = _UniFFILib.{{ ci.ffi_uniffi_contract_version().name() }}();
        if ({{ ci.uniffi_contract_version() }} != scaffolding_contract_version) {
            throw new UniffiContractVersionException($"{{ config.namespace() }}: uniffi bindings expected version `{{ ci.uniffi_contract_version() }}`, library returned `{scaffolding_contract_version}`");
        }
    }

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
}
