{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class FfiConverterDuration: FfiConverterRustBuffer<TimeSpan> {
    public static FfiConverterDuration INSTANCE = new FfiConverterDuration();

    // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeSpan.cs
    private const uint NanosecondsPerTick = 100;

    public override TimeSpan Read(BigEndianStream stream) {
        var seconds = stream.ReadULong();
        var nanoseconds = stream.ReadUInt();
        var ticks = seconds * TimeSpan.TicksPerSecond;
        ticks += nanoseconds / NanosecondsPerTick;
        return new TimeSpan(Convert.ToInt64(ticks));
    }

    public override int AllocationSize(TimeSpan value) {
        // 8 bytes for seconds, 4 bytes for nanoseconds
        return 12;
    }

    public override void Write(TimeSpan value, BigEndianStream stream) {
        stream.WriteULong(Convert.ToUInt64(value.Ticks / TimeSpan.TicksPerSecond));
        stream.WriteUInt(Convert.ToUInt32(value.Ticks % TimeSpan.TicksPerSecond * NanosecondsPerTick));
    }
}
