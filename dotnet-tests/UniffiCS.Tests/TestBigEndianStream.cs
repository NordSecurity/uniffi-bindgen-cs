// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using UniffiCS.Tests.gen;

namespace UniffiCS.Tests;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public class TestBigEndianStream
{
    [Fact]
    public void TestBytes()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Hello, world!");
        var stream = new BigEndianStream(new MemoryStream(new byte[bytes.Length]));
        stream.WriteBytes(bytes);
        Assert.False(stream.HasRemaining());
        stream.Position = 0;
        Assert.Equal(bytes, stream.ReadBytes(bytes.Length));
        Assert.False(stream.HasRemaining());
    }

    [Fact]
    public void TestEndianness()
    {
        var endianessTest = (Action<BigEndianStream> write, byte[] expected_bytes) =>
        {
            var stream = new BigEndianStream(new MemoryStream(new byte[expected_bytes.Length]));
            write(stream);
            stream.Position = 0;
            Assert.Equal(expected_bytes, stream.ReadBytes(expected_bytes.Length));
        };

        endianessTest((stream) => stream.WriteUShort(0x1122), new byte[] { 0x11, 0x22 });

        endianessTest((stream) => stream.WriteUInt(0x11223344), new byte[] { 0x11, 0x22, 0x33, 0x44 });

        endianessTest(
            (stream) => stream.WriteULong(0x1122334455667788),
            new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }
        );

        endianessTest((stream) => stream.WriteShort(0x1122), new byte[] { 0x11, 0x22 });

        endianessTest((stream) => stream.WriteInt(0x11223344), new byte[] { 0x11, 0x22, 0x33, 0x44 });

        endianessTest(
            (stream) => stream.WriteLong(0x1122334455667788),
            new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }
        );

        // https://docs.rs/bytes/latest/bytes/trait.BufMut.html#method.put_f32
        // https://www.h-schmidt.net/FloatConverter/IEEE754.html
        endianessTest((stream) => stream.WriteFloat(1.2f), new byte[] { 0x3F, 0x99, 0x99, 0x9A });

        // https://docs.rs/bytes/latest/bytes/trait.BufMut.html#method.put_f64
        // https://www.h-schmidt.net/FloatConverter/IEEE754.html
        endianessTest(
            (stream) => stream.WriteDouble(1.2d),
            new byte[] { 0x3F, 0xF3, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33 }
        );
    }

    [Fact]
    void TestReadWrite()
    {
        var meanValue = 0x1234_5678_9123_4567;

        ReadWriteTest<byte>(
            (stream) => stream.WriteByte,
            (stream) => stream.ReadByte,
            Byte.MinValue,
            Byte.MaxValue,
            (byte)meanValue
        );

        ReadWriteTest<ushort>(
            (stream) => stream.WriteUShort,
            (stream) => stream.ReadUShort,
            UInt16.MinValue,
            UInt16.MaxValue,
            (ushort)meanValue
        );

        ReadWriteTest<uint>(
            (stream) => stream.WriteUInt,
            (stream) => stream.ReadUInt,
            UInt32.MinValue,
            UInt32.MaxValue,
            (uint)meanValue
        );

        ReadWriteTest<ulong>(
            (stream) => stream.WriteULong,
            (stream) => stream.ReadULong,
            UInt64.MinValue,
            UInt64.MaxValue,
            (ulong)meanValue
        );

        ReadWriteTest<sbyte>(
            (stream) => stream.WriteSByte,
            (stream) => stream.ReadSByte,
            SByte.MinValue,
            SByte.MaxValue,
            (sbyte)meanValue
        );

        ReadWriteTest<short>(
            (stream) => stream.WriteShort,
            (stream) => stream.ReadShort,
            Int16.MinValue,
            Int16.MaxValue,
            (short)meanValue
        );

        ReadWriteTest<int>(
            (stream) => stream.WriteInt,
            (stream) => stream.ReadInt,
            Int32.MinValue,
            Int32.MaxValue,
            (int)meanValue
        );

        ReadWriteTest<long>(
            (stream) => stream.WriteLong,
            (stream) => stream.ReadLong,
            Int64.MinValue,
            Int64.MaxValue,
            (long)meanValue
        );

        ReadWriteTest<float>(
            (stream) => stream.WriteFloat,
            (stream) => stream.ReadFloat,
            Single.MinValue,
            Single.MaxValue,
            Single.Epsilon
        );

        ReadWriteTest<double>(
            (stream) => stream.WriteDouble,
            (stream) => stream.ReadDouble,
            Double.MinValue,
            Double.MaxValue,
            Double.Epsilon
        );
    }

    [Fact]
    public void TestUnderflowReadThrows()
    {
        var newStream = (int length) => new BigEndianStream(new MemoryStream(new byte[length]));

        Assert.Throws<StreamUnderflowException>(() => newStream(10).ReadBytes(11));

        Assert.Throws<StreamUnderflowException>(() => newStream(0).ReadByte());
        Assert.Throws<StreamUnderflowException>(() => newStream(1).ReadUShort());
        Assert.Throws<StreamUnderflowException>(() => newStream(3).ReadUInt());
        Assert.Throws<StreamUnderflowException>(() => newStream(7).ReadULong());

        Assert.Throws<StreamUnderflowException>(() => newStream(0).ReadSByte());
        Assert.Throws<StreamUnderflowException>(() => newStream(1).ReadShort());
        Assert.Throws<StreamUnderflowException>(() => newStream(3).ReadInt());
        Assert.Throws<StreamUnderflowException>(() => newStream(7).ReadLong());

        Assert.Throws<StreamUnderflowException>(() => newStream(3).ReadFloat());
        Assert.Throws<StreamUnderflowException>(() => newStream(7).ReadDouble());
    }

    static void ReadWriteTest<T>(
        Func<BigEndianStream, Action<T>> write,
        Func<BigEndianStream, Func<T>> read,
        T minValue,
        T maxValue,
        T meanValue
    )
        where T : IComparable
    {
        var memoryStream = new MemoryStream(new byte[Marshal.SizeOf(minValue)]);
        var stream = new BigEndianStream(memoryStream);

        stream.Position = 0;
        write(stream)(minValue);
        stream.Position = 0;
        Assert.Equal(0, read(stream)().CompareTo(minValue));

        stream.Position = 0;
        write(stream)(maxValue);
        stream.Position = 0;
        Assert.Equal(0, read(stream)().CompareTo(maxValue));

        stream.Position = 0;
        write(stream)(meanValue);
        stream.Position = 0;
        Assert.Equal(read(stream)(), meanValue);
    }
}
