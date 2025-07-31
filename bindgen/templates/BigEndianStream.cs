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
#if DOTNET_8_0_OR_GREATER
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

#if DOTNET_8_0_OR_GREATER
        stream.Write(buffer);
#else
        stream.Write(buffer, 0, buffer.Length);
#endif
    }

    public static void WriteInt64(this Stream stream, long value, int bytesToWrite = 8)
    {
#if DOTNET_8_0_OR_GREATER
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

#if DOTNET_8_0_OR_GREATER
        stream.Write(buffer);
#else
        stream.Write(buffer, 0, buffer.Length);
#endif
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

    public void WriteBytes(byte[] value) {
#if DOTNET_8_0_OR_GREATER
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
        CheckRemaining(length);
        byte[] result = new byte[length];
        stream.Read(result, 0, length);
        return result;
    }

    public byte ReadByte() {
        CheckRemaining(1);
        return Convert.ToByte(stream.ReadByte());
    }

    public ushort ReadUShort() {
        CheckRemaining(2);
        return (ushort)(stream.ReadByte() << 8 | stream.ReadByte());
    }

    public uint ReadUInt() {
        CheckRemaining(4);
        return (uint)(stream.ReadByte() << 24
            | stream.ReadByte() << 16
            | stream.ReadByte() << 8
            | stream.ReadByte());
    }

    public ulong ReadULong() {
        return (ulong)ReadUInt() << 32 | (ulong)ReadUInt();
    }

    public sbyte ReadSByte() {
        return (sbyte)ReadByte();
    }

    public short ReadShort() {
        return (short)ReadUShort();
    }

    public int ReadInt() {
        return (int)ReadUInt();
    }

    public float ReadFloat() {
        unsafe {
            int value = ReadInt();
            return *((float*)&value);
        }
    }

    public long ReadLong() {
        return (long)ReadULong();
    }

    public double ReadDouble() {
        return BitConverter.Int64BitsToDouble(ReadLong());
    }

    private void CheckRemaining(int length) {
        if (stream.Length - Position < length) {
            throw new StreamUnderflowException();
        }
    }
}
