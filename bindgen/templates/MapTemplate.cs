{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class {{ ffi_converter_name }}: FfiConverterRustBuffer<{{ type_name }}> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override {{ type_name }} Read(BigEndianStream stream) {
        var result = new {{ type_name }}();
        var len = stream.ReadInt();
        for (int i = 0; i < len; i++) {
            var key = {{ key_type|read_fn }}(stream);
            var value = {{ value_type|read_fn }}(stream);
            result[key] = value;
        }
        return result;
    }

    public override int AllocationSize({{ type_name }} value) {
        var sizeForLength = 4;

        // details/1-empty-list-as-default-method-parameter.md
        if (value == null) {
            return sizeForLength;
        }

        var sizeForItems = value.Select(item => {
            return {{ key_type|allocation_size_fn }}(item.Key) +
                {{ value_type|allocation_size_fn }}(item.Value);
        }).Sum();
        return sizeForLength + sizeForItems;
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        // details/1-empty-list-as-default-method-parameter.md
        if (value == null) {
            stream.WriteInt(0);
            return;
        }

        stream.WriteInt(value.Count);
        foreach (var item in value) {
            {{ key_type|write_fn }}(item.Key, stream);
            {{ value_type|write_fn }}(item.Value, stream);
        }
    }
}
