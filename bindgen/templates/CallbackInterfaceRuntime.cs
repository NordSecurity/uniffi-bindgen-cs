{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

static class UniffiCallbackResponseCode {
    public static int SUCCESS = 0;
    public static int ERROR = 1;
    public static int UNEXPECTED_ERROR = 2;
}

class ConcurrentHandleMap<T> where T: notnull {
    class Entry {
        public Entry(T obj, ulong refcount, ulong handle) {
            this.obj = obj;
            this.refcount = refcount;
            this.handle = handle;
        }

        public T obj;
        public ulong refcount;
        public ulong handle;
    }

    Dictionary<ulong, Entry> leftMap = new Dictionary<ulong, Entry>();
    Dictionary<T, Entry> rightMap = new Dictionary<T, Entry>();

    Object lock_ = new Object();
    ulong currentHandle = 0;

    public ulong Insert(T obj) {
        lock (lock_) {
            if (rightMap.TryGetValue(obj, out Entry? entry)) {
                entry.refcount += 1;
                return entry.handle;
            }

            currentHandle += 1;

            Entry newEntry = new Entry(obj, 1, currentHandle);
            leftMap[newEntry.handle] = newEntry;
            rightMap[newEntry.obj] = newEntry;

            return newEntry.handle;
        }
    }

    public T Get(ulong handle) {
        lock (lock_) {
            if (leftMap.TryGetValue(handle, out Entry? entry)) {
                return entry.obj;
            }

            throw new InternalException($"ConcurrentHandleMap::Get No callback in handlemap '{handle}'");
        }
    }

    public void Remove(ulong handle) {
        lock (lock_) {
            if (leftMap.TryGetValue(handle, out Entry? entry)) {
                entry.refcount -= 1;
                if (entry.refcount < 1) {
                    leftMap.Remove(entry.handle);
                    rightMap.Remove(entry.obj);
                }
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
        return handleMap.Get(handle);
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
