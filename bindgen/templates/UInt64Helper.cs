{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class {{ ffi_converter_name }}: FfiConverter<ulong, ulong> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override ulong Lift(ulong value) {
        return value;
    }

    public override ulong Read(BigEndianStream stream) {
        return stream.ReadULong();
    }

    public override ulong Lower(ulong value) {
        return value;
    }

    public override int AllocationSize(ulong value) {
        return 8;
    }

    public override void Write(ulong value, BigEndianStream stream) {
        stream.WriteULong(value);
    }
}
