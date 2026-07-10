{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let inner_type_name = inner_type|type_name(ci) %}

class {{ ffi_converter_name }}: FfiConverterRustBuffer<HashSet<{{ inner_type_name }}>> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override HashSet<{{ inner_type_name }}> Read(BigEndianStream stream) {
        var length = stream.ReadInt();
        var result = new HashSet<{{ inner_type_name }}>();
        var readFn = {{ inner_type|read_fn }};
        for (int i = 0; i < length; i++) {
            result.Add(readFn(stream));
        }
        return result;
    }

    public override int AllocationSize(HashSet<{{ inner_type_name }}> value) {
        var sizeForLength = 4;

        // details/1-empty-list-as-default-method-parameter.md
        if (value == null) {
            return sizeForLength;
        }

        var allocationSizeFn = {{ inner_type|allocation_size_fn }};
        var sizeForItems = value.Sum(item => allocationSizeFn(item));
        return sizeForLength + sizeForItems;
    }

    public override void Write(HashSet<{{ inner_type_name }}> value, BigEndianStream stream) {
        // details/1-empty-list-as-default-method-parameter.md
        if (value == null) {
            stream.WriteInt(0);
            return;
        }

        stream.WriteInt(value.Count);
        var writerFn = {{ inner_type|write_fn }};
        foreach (var item in value) {
            writerFn(item, stream);
        }
    }
}
