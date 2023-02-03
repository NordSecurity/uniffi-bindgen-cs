{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class FfiConverterByte: FfiConverter<byte, byte> {
    public static FfiConverterByte INSTANCE = new FfiConverterByte();

    public override byte Lift(byte value) {
        return value;
    }

    public override byte Read(BigEndianStream stream) {
        return stream.ReadByte();
    }

    public override byte Lower(byte value) {
        return value;
    }

    public override int AllocationSize(byte value) {
        return 1;
    }

    public override void Write(byte value, BigEndianStream stream) {
        stream.WriteByte(value);
    }
}
