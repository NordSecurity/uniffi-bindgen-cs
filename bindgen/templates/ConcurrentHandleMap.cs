{{- self.add_import("System.Collections.Concurrent") }}
{{- self.add_import("System.Diagnostics.CodeAnalysis") }}
{{- self.add_import("System.Threading") }}

class ConcurrentHandleMap<T> where T: notnull {
    readonly ConcurrentDictionary<ulong, T> _map = new();

    long _currentHandle = 0;

    public ulong Insert(T obj) {
        var handle = (ulong)Interlocked.Increment(ref _currentHandle);
        if (!_map.TryAdd(handle, obj)) {
            throw new InternalException("ConcurrentHandleMap: Duplicate handle");
        }
        return handle;
    }

    public bool TryGet(ulong handle, [NotNullWhen(true)] out T? result) {
        return _map.TryGetValue(handle, out result);
    }

    public T Get(ulong handle) {
        if (_map.TryGetValue(handle, out var result)) {
            return result;
        } else {
            throw new InternalException("ConcurrentHandleMap: Invalid handle");
        }
    }

    public bool Remove(ulong handle) {
        return _map.TryRemove(handle, out _);
    }

    public bool Remove(ulong handle, [NotNullWhen(true)] out T? result) {
        return _map.TryRemove(handle, out result);
    }
}
