{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

// This is a helper for safely working with byte buffers returned from the Rust code.
// A rust-owned buffer is represented by its capacity, its current length, and a
// pointer to the underlying data.

[StructLayout(LayoutKind.Sequential)]
internal struct RustBuffer {
    public ulong capacity;
    public ulong len;
    public IntPtr data;

    public static RustBuffer Alloc(int size) {
        return _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            var buffer = _UniFFILib.{{ ci.ffi_rustbuffer_alloc().name() }}(Convert.ToUInt64(size), ref status);
            if (buffer.data == IntPtr.Zero) {
                throw new AllocationException($"RustBuffer.Alloc() returned null data pointer (size={size})");
            }
            return buffer;
        });
    }

    public static void Free(RustBuffer buffer) {
        _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            _UniFFILib.{{ ci.ffi_rustbuffer_free().name() }}(buffer, ref status);
        });
    }

    public static BigEndianStream MemoryStream(IntPtr data, long length)
    {
        unsafe
        {
            return new BigEndianStream(new UnmanagedMemoryStream((byte*)data.ToPointer(), length));
        }
    }

    public BigEndianStream AsStream()
    {
        unsafe
        {
            return new BigEndianStream(
                new UnmanagedMemoryStream((byte*)data.ToPointer(), Convert.ToInt64(len))
            );
        }
    }

    public BigEndianStream AsWriteableStream()
    {
        unsafe
        {
            return new BigEndianStream(
                new UnmanagedMemoryStream(
                    (byte*)data.ToPointer(),
                    Convert.ToInt64(capacity),
                    Convert.ToInt64(capacity),
                    FileAccess.Write
                )
            );
        }
    }
}

// This is a helper for safely passing byte references into the rust code.
// The pointer borrows foreign-owned memory, so it is only valid for the duration of
// the call it is passed to.

[StructLayout(LayoutKind.Sequential)]
internal struct ForeignBytes {
    public int length;
    public IntPtr data;
}

// Pins a `byte[]` so Rust can borrow it as `ForeignBytes` without copying. `[ByRef] bytes`
// (`&[u8]`) arguments take this zero-copy path instead of being copied into a `RustBuffer`.
// The array must stay pinned until the FFI call returns, so this is always used as a
// `using` declaration wrapping the call.
internal struct ForeignBytesPin : IDisposable {
    private GCHandle _handle;
    private readonly int _length;

    public ForeignBytesPin(byte[] value) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }
        _handle = GCHandle.Alloc(value, GCHandleType.Pinned);
        _length = value.Length;
    }

    public ForeignBytes Bytes {
        get { return new ForeignBytes { length = _length, data = _handle.AddrOfPinnedObject() }; }
    }

    public void Dispose() {
        if (_handle.IsAllocated) {
            _handle.Free();
        }
    }
}
