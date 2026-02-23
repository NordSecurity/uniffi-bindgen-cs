{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let namespace = ci.namespace_for_module_path(module_path)? %}
{%- let package_name = self.external_type_package_name(module_path, namespace) %}
{%- let local_ffi_converter_name = "FfiConverterType{}"|format(name) %}
{%- let fully_qualified_ffi_converter = "{}.{}"|format(package_name, local_ffi_converter_name) %}

{{- self.add_import(package_name) }}
{{- self.add_type_alias(local_ffi_converter_name, fully_qualified_ffi_converter) }}
