{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class {{ ffi_converter_name }}: FfiConverter<sbyte, sbyte> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override sbyte Lift(sbyte value) {
        return value;
    }

    public override sbyte Read(BigEndianStream stream) {
        return stream.ReadSByte();
    }

    public override sbyte Lower(sbyte value) {
        return value;
    }

    public override int AllocationSize(sbyte value) {
        return 1;
    }

    public override void Write(sbyte value, BigEndianStream stream) {
        stream.WriteSByte(value);
    }
}
