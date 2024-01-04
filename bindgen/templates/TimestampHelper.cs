{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class FfiConverterTimestamp: FfiConverterRustBuffer<DateTime> {
    public static FfiConverterTimestamp INSTANCE = new FfiConverterTimestamp();

    // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeSpan.cs
    private const uint NanosecondsPerTick = 100;

    public override DateTime Read(BigEndianStream stream) {
        var seconds = stream.ReadLong();
        var nanoseconds = stream.ReadUInt();
        var sign = 1;
        if (seconds < 0) {
            sign = -1;
        }
        var ticks = seconds * TimeSpan.TicksPerSecond;
        ticks += (nanoseconds / NanosecondsPerTick) * sign;
        return DateTime.UnixEpoch.ToUniversalTime().AddTicks(ticks);
    }

    public override int AllocationSize(DateTime value) {
        // 8 bytes for seconds, 4 bytes for nanoseconds
        return 12;
    }

    public override void Write(DateTime value, BigEndianStream stream) {
        var epochOffset = value.ToUniversalTime().Subtract(DateTime.UnixEpoch);

        int sign = 1;
        if (epochOffset.Ticks < 0) {
            epochOffset = epochOffset.Negate();
            sign = -1;
        }

        stream.WriteLong(epochOffset.Ticks / TimeSpan.TicksPerSecond * sign);
        stream.WriteUInt(Convert.ToUInt32(epochOffset.Ticks % TimeSpan.TicksPerSecond * NanosecondsPerTick));
    }
}
