{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let namespace = ci.namespace_for_module_path(module_path)? %}
{%- let package_name = self.external_type_package_name(module_path, namespace) %}
{%- let local_ffi_converter_name = "FfiConverterType{}"|format(name) %}
{%- let type_label = name|class_name(ci) %}
{%- let ext_converter = "{}.{}"|format(package_name, local_ffi_converter_name) %}

{{- self.add_import(package_name) }}

class {{ local_ffi_converter_name }}: FfiConverterRustBuffer<{{ type_label }}>
{%- if ci.is_name_used_as_error(name) %}, CallStatusErrorHandler<{{ type_label }}>{% endif %} {
    public static {{ local_ffi_converter_name }} INSTANCE = new {{ local_ffi_converter_name }}();

    public override {{ type_label }} Read(BigEndianStream stream) {
        return {{ ext_converter }}.INSTANCE.Read(
            new {{ package_name }}.BigEndianStream(stream.InnerStream)
        );
    }

    public override int AllocationSize({{ type_label }} value) {
        return {{ ext_converter }}.INSTANCE.AllocationSize(value);
    }

    public override void Write({{ type_label }} value, BigEndianStream stream) {
        {{ ext_converter }}.INSTANCE.Write(
            value,
            new {{ package_name }}.BigEndianStream(stream.InnerStream)
        );
    }
{%- if ci.is_name_used_as_error(name) %}

    public {{ type_label }} Lift(RustBuffer buf) {
        return Read(buf.AsStream());
    }
{%- endif %}
}
