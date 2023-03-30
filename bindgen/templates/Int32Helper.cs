{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class FfiConverterInt: FfiConverter<int, int> {
    public static FfiConverterInt INSTANCE = new FfiConverterInt();

    public override int Lift(int value) {
        return value;
    }

    public override int Read(BigEndianStream stream) {
        return stream.ReadInt();
    }

    public override int Lower(int value) {
        return value;
    }

    public override int AllocationSize(int value) {
        return 4;
    }

    public override void Write(int value, BigEndianStream stream) {
        stream.WriteInt(value);
    }
}
