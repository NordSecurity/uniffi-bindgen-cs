{{- self.add_import("System.Collections.Concurrent") }}
{{- self.add_import("System.Diagnostics.CodeAnalysis") }}
{{- self.add_import("System.Threading") }}

class ConcurrentHandleMap<T> where T: notnull {
    readonly ConcurrentDictionary<ulong, T> _map = new();

    // Handles are odd numbers (1, 3, 5, ...) — the lowest bit must always be set.
    // Rust uses (handle & 1) to distinguish foreign-language handles from Rust Arc
    // pointers, which are always even due to memory alignment. See uniffi_core/src/ffi/handle.rs.
    const long HANDLE_INITIAL = 1;
    const long HANDLE_DELTA = 2;
    long _currentHandle = HANDLE_INITIAL - HANDLE_DELTA;

    public ulong Insert(T obj) {
        var handle = (ulong)Interlocked.Add(ref _currentHandle, HANDLE_DELTA);
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
