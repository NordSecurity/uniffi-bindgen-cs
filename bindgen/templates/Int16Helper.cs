{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class {{ ffi_converter_name }}: FfiConverter<short, short> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override short Lift(short value) {
        return value;
    }

    public override short Read(BigEndianStream stream) {
        return stream.ReadShort();
    }

    public override short Lower(short value) {
        return value;
    }

    public override int AllocationSize(short value) {
        return 2;
    }

    public override void Write(short value, BigEndianStream stream) {
        stream.WriteShort(value);
    }
}
