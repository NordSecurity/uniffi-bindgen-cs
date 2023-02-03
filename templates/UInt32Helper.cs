{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class FfiConverterUInt: FfiConverter<uint, uint> {
    public static FfiConverterUInt INSTANCE = new FfiConverterUInt();

    public override uint Lift(uint value) {
        return value;
    }

    public override uint Read(BigEndianStream stream) {
        return stream.ReadUInt();
    }

    public override uint Lower(uint value) {
        return value;
    }

    public override int AllocationSize(uint value) {
        return 4;
    }

    public override void Write(uint value, BigEndianStream stream) {
        stream.WriteUInt(value);
    }
}
