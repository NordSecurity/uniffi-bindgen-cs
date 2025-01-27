{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

// The FfiConverter interface handles converter types to and from the FFI
//
// All implementing objects should be public to support external types.  When a
// type is external we need to import it's FfiConverter.
internal abstract class FfiConverter<CsType, FfiType> {
    // Convert an FFI type to a C# type
    public abstract CsType Lift(FfiType value);

    // Convert C# type to an FFI type
    public abstract FfiType Lower(CsType value);

    // Read a C# type from a `ByteBuffer`
    public abstract CsType Read(BigEndianStream stream);

    // Calculate bytes to allocate when creating a `RustBuffer`
    //
    // This must return at least as many bytes as the write() function will
    // write. It can return more bytes than needed, for example when writing
    // Strings we can't know the exact bytes needed until we the UTF-8
    // encoding, so we pessimistically allocate the largest size possible (3
    // bytes per codepoint).  Allocating extra bytes is not really a big deal
    // because the `RustBuffer` is short-lived.
    public abstract int AllocationSize(CsType value);

    // Write a C# type to a `ByteBuffer`
    public abstract void Write(CsType value, BigEndianStream stream);

    // Lower a value into a `RustBuffer`
    //
    // This method lowers a value into a `RustBuffer` rather than the normal
    // FfiType.  It's used by the callback interface code.  Callback interface
    // returns are always serialized into a `RustBuffer` regardless of their
    // normal FFI type.
    public RustBuffer LowerIntoRustBuffer(CsType value) {
        var rbuf = RustBuffer.Alloc(AllocationSize(value));
        try {
            var stream = rbuf.AsWriteableStream();
            Write(value, stream);
            rbuf.len = Convert.ToUInt64(stream.Position);
            return rbuf;
        } catch {
            RustBuffer.Free(rbuf);
            throw;
        }
    }

    // Lift a value from a `RustBuffer`.
    //
    // This here mostly because of the symmetry with `lowerIntoRustBuffer()`.
    // It's currently only used by the `FfiConverterRustBuffer` class below.
    protected CsType LiftFromRustBuffer(RustBuffer rbuf) {
        var stream = rbuf.AsStream();
        try {
           var item = Read(stream);
           if (stream.HasRemaining()) {
               throw new InternalException("junk remaining in buffer after lifting, something is very wrong!!");
           }
           return item;
        } finally {
            RustBuffer.Free(rbuf);
        }
    }
}

// FfiConverter that uses `RustBuffer` as the FfiType
internal abstract class FfiConverterRustBuffer<CsType>: FfiConverter<CsType, RustBuffer> {
    public override CsType Lift(RustBuffer value) {
        return LiftFromRustBuffer(value);
    }
    public override RustBuffer Lower(CsType value) {
        return LowerIntoRustBuffer(value);
    }
}
