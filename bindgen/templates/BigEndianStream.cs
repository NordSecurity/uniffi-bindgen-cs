{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

// Big endian streams are not yet available in dotnet :'(
// https://github.com/dotnet/runtime/issues/26904

class StreamUnderflowException: System.Exception {
    public StreamUnderflowException() {
    }
}

static class BigEndianStreamExtensions
{
    public static void WriteInt32(this Stream stream, int value, int bytesToWrite = 4)
    {
#if NET8_0_OR_GREATER
        Span<byte> buffer = stackalloc byte[bytesToWrite];
#else
        byte[] buffer = new byte[bytesToWrite];
#endif
        var posByte = bytesToWrite;
        while (posByte != 0)
        {
            posByte--;
            buffer[posByte] = (byte)(value);
            value >>= 8;
        }

#if NET8_0_OR_GREATER
        stream.Write(buffer);
#else
        stream.Write(buffer, 0, buffer.Length);
#endif
    }

    public static void WriteInt64(this Stream stream, long value)
    {
        int bytesToWrite = 8;
#if NET8_0_OR_GREATER
         Span<byte> buffer = stackalloc byte[bytesToWrite];
 #else
         byte[] buffer = new byte[bytesToWrite];
 #endif
        var posByte = bytesToWrite;
        while (posByte != 0)
        {
            posByte--;
            buffer[posByte] = (byte)(value);
            value >>= 8;
        }

#if NET8_0_OR_GREATER
        stream.Write(buffer);
#else
        stream.Write(buffer, 0, buffer.Length);
#endif
    }

    public static uint ReadUint32(this Stream stream, int bytesToRead = 4) {
        CheckRemaining(stream, bytesToRead);
#if NET8_0_OR_GREATER
         Span<byte> buffer = stackalloc byte[bytesToRead];
         stream.Read(buffer);
#else
        byte[] buffer = new byte[bytesToRead];
        stream.Read(buffer, 0, bytesToRead);
#endif
        uint result = 0;
        uint digitMultiplier = 1;
        int posByte = bytesToRead;
        while (posByte != 0)
        {
            posByte--;
            result |= buffer[posByte]*digitMultiplier;
            digitMultiplier <<= 8;
        }

        return result;
    }

    public static ulong ReadUInt64(this Stream stream) {
        int bytesToRead = 8;
        CheckRemaining(stream, bytesToRead);
#if NET8_0_OR_GREATER
         Span<byte> buffer = stackalloc byte[bytesToRead];
         stream.Read(buffer);
#else
        byte[] buffer = new byte[bytesToRead];
        stream.Read(buffer, 0, bytesToRead);
#endif
        ulong result = 0;
        ulong digitMultiplier = 1;
        int posByte = bytesToRead;
        while (posByte != 0)
        {
            posByte--;
            result |= buffer[posByte]*digitMultiplier;
            digitMultiplier <<= 8;
        }

        return result;
    }

    public static void CheckRemaining(this Stream stream, int length) {
        if (stream.Length - stream.Position < length) {
            throw new StreamUnderflowException();
        }
    }

    public static void ForEach<T>(this T[] items, Action<T> action){
        foreach (var item in items) {
            action(item);
        }
    }
}

class BigEndianStream {
    Stream stream;
    public BigEndianStream(Stream stream) {
        this.stream = stream;
    }

    public bool HasRemaining() {
        return (stream.Length - Position) > 0;
    }

    public long Position {
        get => stream.Position;
        set => stream.Position = value;
    }

    public void WriteBytes(byte[] buffer) {
#if NET8_0_OR_GREATER
        stream.Write(buffer);
#else
        stream.Write(buffer, 0, buffer.Length);
#endif
    }

    public void WriteByte(byte value) => stream.WriteInt32(value, bytesToWrite: 1);
    public void WriteSByte(sbyte value) => stream.WriteInt32(value, bytesToWrite: 1);

    public void WriteUShort(ushort value) => stream.WriteInt32(value, bytesToWrite: 2);
    public void WriteShort(short value) => stream.WriteInt32(value, bytesToWrite: 2);

    public void WriteUInt(uint value) => stream.WriteInt32((int)value);
    public void WriteInt(int value) => stream.WriteInt32(value);

    public void WriteULong(ulong value) => stream.WriteInt64((long)value);
    public void WriteLong(long value) => stream.WriteInt64(value);

    public void WriteFloat(float value) {
        unsafe {
            WriteInt(*((int*)&value));
        }
    }
    public void WriteDouble(double value) => stream.WriteInt64(BitConverter.DoubleToInt64Bits(value));

    public byte[] ReadBytes(int length) {
        stream.CheckRemaining(length);
        byte[] result = new byte[length];
        stream.Read(result, 0, length);
        return result;
    }

    public byte ReadByte() => (byte)stream.ReadUint32(bytesToRead: 1);
    public ushort ReadUShort() => (ushort)stream.ReadUint32(bytesToRead: 2);
    public uint ReadUInt() => (uint)stream.ReadUint32(bytesToRead: 4);
    public ulong ReadULong() => stream.ReadUInt64();

    public sbyte ReadSByte() => (sbyte)ReadByte();
    public short ReadShort() => (short)ReadUShort();
    public int ReadInt() => (int)ReadUInt();

    public float ReadFloat() {
        unsafe {
            int value = ReadInt();
            return *((float*)&value);
        }
    }

    public long ReadLong() => (long)ReadULong();
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());
}
