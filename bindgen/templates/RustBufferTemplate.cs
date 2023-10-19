{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

// This is a helper for safely working with byte buffers returned from the Rust code.
// A rust-owned buffer is represented by its capacity, its current length, and a
// pointer to the underlying data.

[StructLayout(LayoutKind.Sequential)]
internal struct RustBuffer {
    public int capacity;
    public int len;
    public IntPtr data;

    public static RustBuffer Alloc(int size) {
        return _UniffiHelpers.RustCall((ref RustCallStatus status) => {
            var buffer = _UniFFILib.{{ ci.ffi_rustbuffer_alloc().name() }}(size, ref status);
            if (buffer.data == IntPtr.Zero) {
                throw new AllocationException($"RustBuffer.Alloc() returned null data pointer (size={size})");
            }
            return buffer;
        });
    }

    public static void Free(RustBuffer buffer) {
        _UniffiHelpers.RustCall((ref RustCallStatus status) => {
            _UniFFILib.{{ ci.ffi_rustbuffer_free().name() }}(buffer, ref status);
        });
    }

    public static BigEndianStream MemoryStream(IntPtr data, int length) {
        unsafe {
            return new BigEndianStream(new UnmanagedMemoryStream((byte*)data.ToPointer(), length));
        }
    }

    public BigEndianStream AsStream() {
        unsafe {
            return new BigEndianStream(new UnmanagedMemoryStream((byte*)data.ToPointer(), len));
        }
    }

    public BigEndianStream AsWriteableStream() {
        unsafe {
            return new BigEndianStream(new UnmanagedMemoryStream((byte*)data.ToPointer(), capacity, capacity, FileAccess.Write));
        }
    }
}

// This is a helper for safely passing byte references into the rust code.
// It's not actually used at the moment, because there aren't many things that you
// can take a direct pointer to managed memory, and if we're going to copy something
// then we might as well copy it into a `RustBuffer`. But it's here for API
// completeness.

[StructLayout(LayoutKind.Sequential)]
internal struct ForeignBytes {
    public int length;
    public IntPtr data;
}
