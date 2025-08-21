{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let inner_type_name = inner_type|type_name(ci) %}

class {{ ffi_converter_name }}: FfiConverterRustBuffer<{{ inner_type_name }}[]> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override {{ inner_type_name }}[]  Read(BigEndianStream stream) {
        var length = stream.ReadInt();
        if (length == 0) {
            return [];
        }

        var result = new {{ inner_type_name }}[(length)];
        var readFn = {{ inner_type|read_fn }};
        for (int i = 0; i < length; i++) {
            result[i] = readFn(stream);
        }
        return result;
    }

    public override int AllocationSize({{ inner_type_name }}[]  value) {
        var sizeForLength = 4;

        // details/1-empty-list-as-default-method-parameter.md
        if (value == null) {
            return sizeForLength;
        }

        var allocationSizeFn = {{ inner_type|allocation_size_fn }};
        var sizeForItems = value.Sum(item => allocationSizeFn(item));
        return sizeForLength + sizeForItems;
    }

    public override void Write({{ inner_type_name }}[] value, BigEndianStream stream) {
        // details/1-empty-list-as-default-method-parameter.md
        if (value == null) {
            stream.WriteInt(0);
            return;
        }

        stream.WriteInt(value.Length);
        var writerFn = {{ inner_type|write_fn }};
        value.ForEach(item => writerFn(item, stream));
    }
}
