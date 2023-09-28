{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class {{ ffi_converter_name }}: FfiConverter<long, long> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override long Lift(long value) {
        return value;
    }

    public override long Read(BigEndianStream stream) {
        return stream.ReadLong();
    }

    public override long Lower(long value) {
        return value;
    }

    public override int AllocationSize(long value) {
        return 8;
    }

    public override void Write(long value, BigEndianStream stream) {
        stream.WriteLong(value);
    }
}
