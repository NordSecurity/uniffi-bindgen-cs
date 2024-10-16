{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

static class UniffiCallbackResponseCode {
    public static int SUCCESS = 0;
    public static int ERROR = 1;
    public static int UNEXPECTED_ERROR = 2;
}

class ConcurrentHandleMap<T> where T: notnull {
    Dictionary<ulong, T> map = new Dictionary<ulong, T>();

    Object lock_ = new Object();
    ulong currentHandle = 0;

    public ulong Insert(T obj) {
        lock (lock_) {
            currentHandle += 1;
            map[currentHandle] = obj;
            return currentHandle;
        }
    }

    public bool TryGet(ulong handle, out T result) {
        lock (lock_) {
            #pragma warning disable 8601 // Possible null reference assignment
            return map.TryGetValue(handle, out result);
            #pragma warning restore 8601
        }
    }

    public bool Remove(ulong handle) {
        return Remove(handle, out T result);
    }

    public bool Remove(ulong handle, out T result) {
        lock (lock_) {
            // Possible null reference assignment
            #pragma warning disable 8601
            if (map.TryGetValue(handle, out result)) {
            #pragma warning restore 8601
                map.Remove(handle);
                return true;
            } else {
                return false;
            }
        }
    }
}

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
