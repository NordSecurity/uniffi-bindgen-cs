class ConcurrentHandleMap<T> where T: notnull {
    Dictionary<ulong, T> map = new Dictionary<ulong, T>();

    Object lock_ = new Object();
    ulong currentHandle = 1;

    public ulong Insert(T obj) {
        lock (lock_) {
            currentHandle += 2;
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

    public T Get(ulong handle) {
        if (TryGet(handle, out var result)) {
            return result;
        } else {
            throw new InternalException("ConcurrentHandleMap: Invalid handle");
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