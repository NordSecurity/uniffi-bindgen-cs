{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

static class UniffiCallbackResponseCode {
    public static int SUCCESS = 0;
    public static int ERROR = 1;
    public static int UNEXPECTED_ERROR = 2;
}

{% if self.include_once_check("ConcurrentHandleMap.cs") %}{% include "ConcurrentHandleMap.cs" %}{% endif %}


[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int ForeignCallback(ulong handle, uint method, IntPtr argsData, int argsLength, ref RustBuffer outBuf);

internal abstract class FfiConverterCallbackInterface<CallbackInterface>
        : FfiConverter<CallbackInterface, ulong>
        where CallbackInterface: notnull
{
    ConcurrentHandleMap<CallbackInterface> handleMap = new ConcurrentHandleMap<CallbackInterface>();

    // Registers the foreign callback with the Rust side.
    // This method is generated for each callback interface.
    public abstract void Register();

    public RustBuffer Drop(ulong handle) {
        handleMap.Remove(handle);
        return new RustBuffer();
    }

    public override CallbackInterface Lift(ulong handle) {
        if (!handleMap.TryGet(handle, out CallbackInterface result)) {
            throw new InternalException($"No callback in handlemap '{handle}'");
        }
        return result;
    }

    public override CallbackInterface Read(BigEndianStream stream) {
        return Lift(stream.ReadULong());
    }

    public override ulong Lower(CallbackInterface value) {
        return handleMap.Insert(value);
    }

    public override int AllocationSize(CallbackInterface value) {
        return 8;
    }

    public override void Write(CallbackInterface value, BigEndianStream stream) {
        stream.WriteULong(Lower(value));
    }
}
