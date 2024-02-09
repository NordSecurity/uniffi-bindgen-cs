using System;
using System.IO;
using System.Runtime.InteropServices;
using uniffi.stringify;

namespace UniffiCS.BindingTests;

public class TestNumericLimits
{
    [Fact]
    public void NumericLimitsAreTheSame()
    {
        // At first, I tried to write this test by stringifying values in C# and Rust, then
        // comparing the stringified result. Turns out C# and Rust format floating point values
        // differently, so comparing the result is useless. I tried to change the formatting
        // settings in few ways, but I couldn't find formatting settings that would produce
        // exact results.

        var meanValue = 0x1234_5678_9123_4567;

        ParseTest<sbyte>(
            StringifyMethods.ParseI8,
            SByte.MinValue,
            SByte.MaxValue,
            (sbyte)meanValue);

        ParseTest<short>(
            StringifyMethods.ParseI16,
            Int16.MinValue,
            Int16.MaxValue,
            (short)meanValue);

        ParseTest<int>(
            StringifyMethods.ParseI32,
            Int32.MinValue,
            Int32.MaxValue,
            (int)meanValue);

        ParseTest<long>(
            StringifyMethods.ParseI64,
            Int64.MinValue,
            Int64.MaxValue,
            (long)meanValue);

        ParseTest<byte>(
            StringifyMethods.ParseU8,
            Byte.MinValue,
            Byte.MaxValue,
            (byte)meanValue);

        ParseTest<ushort>(
            StringifyMethods.ParseU16,
            UInt16.MinValue,
            UInt16.MaxValue,
            (ushort)meanValue);

        ParseTest<uint>(
            StringifyMethods.ParseU32,
            UInt32.MinValue,
            UInt32.MaxValue,
            (uint)meanValue);

        ParseTest<ulong>(
            StringifyMethods.ParseU64,
            UInt64.MinValue,
            UInt64.MaxValue,
            (ulong)meanValue);

        ParseTest<float>(
            StringifyMethods.ParseF32,
            Single.MinValue,
            Single.MaxValue,
            Single.Epsilon);

        ParseTest<double>(
            StringifyMethods.ParseF64,
            Double.MinValue,
            Double.MaxValue,
            Double.Epsilon);
    }

    static void ParseTest<T>(
        Func<String, T> parseMethod,
        T minValue,
        T maxValue,
        T meanValue
    )
    {
        // Possible null reference assignment
#pragma warning disable 8602
#pragma warning disable 8604
        Assert.Equal(minValue, parseMethod(minValue.ToString()));
        Assert.Equal(maxValue, parseMethod(maxValue.ToString()));
        Assert.Equal(meanValue, parseMethod(meanValue.ToString()));
#pragma warning restore 8602
#pragma warning restore 8604
    }
}
