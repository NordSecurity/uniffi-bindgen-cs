{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

class FfiConverterString: FfiConverter<string, RustBuffer> {
    public static FfiConverterString INSTANCE = new FfiConverterString();

    // Note: we don't inherit from FfiConverterRustBuffer, because we use a
    // special encoding when lowering/lifting.  We can use `RustBuffer.len` to
    // store our length and avoid writing it out to the buffer.
    public override string Lift(RustBuffer value) {
        try {
            var length = Convert.ToInt32(value.len);
#if NET8_0_OR_GREATER
            unsafe {
                return System.Text.Encoding.UTF8.GetString(
                    new ReadOnlySpan<byte>((byte*)value.data, length));
            }
#else
            var bytes = value.AsStream().ReadBytes(length);
            return System.Text.Encoding.UTF8.GetString(bytes);
#endif
        } finally {
            RustBuffer.Free(value);
        }
    }

    public override string Read(BigEndianStream stream) {
        var length = stream.ReadInt();
        return stream.ReadUtf8String(length);
    }

    public override RustBuffer Lower(string value) {
        {%- match config.null_string_to_empty %}
        {%- when Some(true) %}
        if (value == null) {
            value = "";
        }
        {%- when _ %}
        {%- endmatch %}
#if NET8_0_OR_GREATER
        var rbuf = RustBuffer.Alloc(System.Text.Encoding.UTF8.GetByteCount(value));
        unsafe {
            var dest = new Span<byte>((byte*)rbuf.data, Convert.ToInt32(rbuf.len));
            System.Text.Encoding.UTF8.GetBytes(value, dest);
        }
        return rbuf;
#else
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var rbuf = RustBuffer.Alloc(bytes.Length);
        rbuf.AsWriteableStream().WriteBytes(bytes);
        return rbuf;
#endif
    }

    // TODO(CS)
    // We aren't sure exactly how many bytes our string will be once it's UTF-8
    // encoded.  Allocate 3 bytes per unicode codepoint which will always be
    // enough.
    public override int AllocationSize(string value) {
        const int sizeForLength = 4;
        var sizeForString = System.Text.Encoding.UTF8.GetByteCount(value);
        return sizeForLength + sizeForString;
    }

    public override void Write(string value, BigEndianStream stream) {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        stream.WriteInt(bytes.Length);
        stream.WriteBytes(bytes);
    }
}
