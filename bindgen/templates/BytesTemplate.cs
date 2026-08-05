{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class {{ ffi_converter_name }}: FfiConverterRustBuffer<{{ type_name }}> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

#if NET8_0_OR_GREATER
    // Copy straight out of the RustBuffer, skipping the stream machinery.
    public override {{ type_name }} Lift(RustBuffer value) {
        try {
            unsafe {
                var buffer = new ReadOnlySpan<byte>((byte*)value.data, Convert.ToInt32(value.len));
                var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer);
                if (4 + length != buffer.Length) {
                    throw new InternalException("junk remaining in buffer after lifting, something is very wrong!!");
                }
                return buffer.Slice(4, length).ToArray();
            }
        } finally {
            RustBuffer.Free(value);
        }
    }
#endif

    public override {{ type_name }} Read(BigEndianStream stream) {
        var length = stream.ReadInt();
        return stream.ReadBytes(length);
    }

    public override int AllocationSize({{ type_name }} value) {
        return 4 + value.Length;
    }

    public override void Write({{ type_name }} value, BigEndianStream stream) {
        stream.WriteInt(value.Length);
        stream.WriteBytes(value);
    }
}
