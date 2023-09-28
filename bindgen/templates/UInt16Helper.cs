{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class {{ ffi_converter_name }}: FfiConverter<ushort, ushort> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override ushort Lift(ushort value) {
        return value;
    }

    public override ushort Read(BigEndianStream stream) {
        return stream.ReadUShort();
    }

    public override ushort Lower(ushort value) {
        return value;
    }

    public override int AllocationSize(ushort value) {
        return 2;
    }

    public override void Write(ushort value, BigEndianStream stream) {
        stream.WriteUShort(value);
    }
}
