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
            unsafe {
                return System.Text.Encoding.UTF8.GetString(
                    (byte*)value.data.ToPointer(), Convert.ToInt32(value.len));
            }
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
        var rustBuffer = RustBuffer.Alloc(System.Text.Encoding.UTF8.GetByteCount(value));
        try {
            unsafe {
                fixed (char* chars = value) {
                    System.Text.Encoding.UTF8.GetBytes(
                        chars, value.Length, (byte*)rustBuffer.data.ToPointer(), Convert.ToInt32(rustBuffer.len));
                }
            }
            return rustBuffer;
        } catch {
            RustBuffer.Free(rustBuffer);
            throw;
        }
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
        stream.WriteUtf8String(value);
    }
}
