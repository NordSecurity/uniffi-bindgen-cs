{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let namespace = ci.namespace_for_module_path(module_path)? %}
{%- let package_name = self.external_type_package_name(module_path, namespace) %}
{%- let local_ffi_converter_name = "FfiConverterType{}"|format(name) %}
{%- let type_label = name|class_name(ci) %}
{%- let ext_converter = "{}.{}.INSTANCE"|format(package_name, local_ffi_converter_name) %}

{{- self.add_import(package_name) }}

// Forwards to the converter generated in `{{ package_name }}`. Every generated file has its own
// `BigEndianStream` class, so `Read` and `Write` re-wrap the stream before handing it over. This is
// what lets the external object sit inside an optional, a sequence, a map or a record of this file.
class {{ local_ffi_converter_name }}: FfiConverter<{{ type_label }}, ulong> {
    public static {{ local_ffi_converter_name }} INSTANCE = new {{ local_ffi_converter_name }}();

    public override {{ type_label }} Lift(ulong value) {
        return {{ ext_converter }}.Lift(value);
    }

    public override ulong Lower({{ type_label }} value) {
        return {{ ext_converter }}.Lower(value);
    }

    public override {{ type_label }} Read(BigEndianStream stream) {
        return {{ ext_converter }}.Read(
            new {{ package_name }}.BigEndianStream(stream.InnerStream)
        );
    }

    public override int AllocationSize({{ type_label }} value) {
        return {{ ext_converter }}.AllocationSize(value);
    }

    public override void Write({{ type_label }} value, BigEndianStream stream) {
        {{ ext_converter }}.Write(
            value,
            new {{ package_name }}.BigEndianStream(stream.InnerStream)
        );
    }
}
