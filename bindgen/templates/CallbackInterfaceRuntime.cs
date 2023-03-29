{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

static class UniffiCallbackConstants {
    public static int SUCCESS = 0;
    public static int ERROR = 1;
    public static int UNEXPECTED_ERROR = 2;
}

class ConcurrentHandleMap<T> where T: notnull {
    Dictionary<ulong, T> leftMap = new Dictionary<ulong, T>();
    Dictionary<T, ulong> rightMap = new Dictionary<T, ulong>();

    Object lock_ = new Object();
    ulong currentHandle = 0;

    public ulong Insert(T obj) {
        lock (lock_) {
            ulong existingHandle = 0;
            if (rightMap.TryGetValue(obj, out existingHandle)) {
                return existingHandle;
            }
            currentHandle += 1;
            leftMap[currentHandle] = obj;
            rightMap[obj] = currentHandle;
            return currentHandle;
        }
    }

    public bool TryGet(ulong handle, out T result) {
        // Possible null reference assignment
        #pragma warning disable 8601
        return leftMap.TryGetValue(handle, out result);
        #pragma warning restore 8601
    }

    public bool Remove(ulong handle) {
        return Remove(handle, out T result);
    }

    public bool Remove(ulong handle, out T result) {
        lock (lock_) {
            // Possible null reference assignment
            #pragma warning disable 8601
            if (leftMap.Remove(handle, out result)) {
            #pragma warning restore 8601
                rightMap.Remove(result);
                return true;
            } else {
                return false;
            }
        }
    }
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int ForeignCallback(ulong handle, int method, IntPtr argsData, int argsLength, ref RustBuffer outBuf);

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
