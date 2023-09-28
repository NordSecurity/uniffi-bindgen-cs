{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class {{ ffi_converter_name }}: FfiConverter<float, float> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override float Lift(float value) {
        return value;
    }

    public override float Read(BigEndianStream stream) {
        return stream.ReadFloat();
    }

    public override float Lower(float value) {
        return value;
    }

    public override int AllocationSize(float value) {
        return 4;
    }

    public override void Write(float value, BigEndianStream stream) {
        stream.WriteFloat(value);
    }
}
