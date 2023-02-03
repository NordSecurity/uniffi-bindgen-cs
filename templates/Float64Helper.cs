{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class FfiConverterDouble: FfiConverter<double, double> {
    public static FfiConverterDouble INSTANCE = new FfiConverterDouble();

    public override double Lift(double value) {
        return value;
    }

    public override double Read(BigEndianStream stream) {
        return stream.ReadDouble();
    }

    public override double Lower(double value) {
        return value;
    }

    public override int AllocationSize(double value) {
        return 8;
    }

    public override void Write(double value, BigEndianStream stream) {
        stream.WriteDouble(value);
    }
}
