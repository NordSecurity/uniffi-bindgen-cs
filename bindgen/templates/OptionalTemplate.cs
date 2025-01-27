{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let inner_type_name = inner_type|type_name(ci) %}

class {{ ffi_converter_name }}: FfiConverterRustBuffer<{{ inner_type_name }}?> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override {{ inner_type_name }}? Read(BigEndianStream stream) {
        if (stream.ReadByte() == 0) {
            return null;
        }
        return {{ inner_type|read_fn }}(stream);
    }

    public override int AllocationSize({{ inner_type_name }}? value) {
        if (value == null) {
            return 1;
        } else {
            return 1 + {{ inner_type|allocation_size_fn }}(({{ inner_type_name }})value);
        }
    }

    public override void Write({{ inner_type_name }}? value, BigEndianStream stream) {
        if (value == null) {
            stream.WriteByte(0);
        } else {
            stream.WriteByte(1);
            {{ inner_type|write_fn }}(({{ inner_type_name }})value, stream);
        }
    }
}
