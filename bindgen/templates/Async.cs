{{ self.add_import("System.Threading")}}
{{ self.add_import("System.Threading.Tasks")}}

{% if self.include_once_check("ConcurrentHandleMap.cs") %}{% include "ConcurrentHandleMap.cs" %}{% endif %}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void UniFfiFutureCallback(ulong continuationHandle, byte pollResult);

internal sealed class UniffiForeignFutureHandle : System.IDisposable {
    internal CancellationTokenSource Cts { get; } = new CancellationTokenSource();
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new Lock();
#else
    private readonly object _lock = new object();
#endif

    internal void MarkDropped() {
        lock (_lock) {
            if (!_callbackInvoked) { Cts.Cancel(); }
        }
    }

    private bool _callbackInvoked = false;

    internal void InvokeCallbackOnce(Action invoke) {
        bool shouldInvoke;
        lock (_lock) {
            shouldInvoke = !_callbackInvoked;
            if (shouldInvoke) _callbackInvoked = true;
        }
        if (shouldInvoke) invoke();
    }

    // Dispose() is called exactly once — from the worker's finally (Task.Run path) or from the
    // synchronous early-return path — always after InvokeCallbackOnce has set _callbackInvoked = true.
    public void Dispose() { Cts.Dispose(); }
}

internal static class _UniFFIAsync {
    internal const byte UNIFFI_RUST_FUTURE_POLL_READY = 0;
    // internal const byte UNIFFI_RUST_FUTURE_POLL_MAYBE_READY = 1;

    internal static ConcurrentHandleMap<TaskCompletionSource<byte>> _async_handle_map = new ConcurrentHandleMap<TaskCompletionSource<byte>>();
    public static ConcurrentHandleMap<UniffiForeignFutureHandle> _foreign_futures_map = new ConcurrentHandleMap<UniffiForeignFutureHandle>();

    // FFI type for Rust future continuations
    internal class UniffiRustFutureContinuationCallback
    {
        public static UniFfiFutureCallback callback = Callback;

        public static void Callback(ulong continuationHandle, byte pollResult)
        {
            if (_async_handle_map.Remove(continuationHandle, out TaskCompletionSource<byte>? task))
            {
                task.SetResult(pollResult);
            }
            // else: continuation already completed (e.g. waker called more than once), ignore
        }
    }

    public class UniffiForeignFutureDroppedCallbackImpl
    {
        public static _UniFFILib.UniffiForeignFutureDroppedCallback callback = Callback;

        public static void Callback(ulong handle)
        {
            if (_foreign_futures_map.Remove(handle, out UniffiForeignFutureHandle? futureHandle) && futureHandle is not null) {
                futureHandle.MarkDropped();
            }
        }
    }

    public delegate F CompleteFuncDelegate<F>(ulong handle, ref UniffiRustCallStatus status);

    public delegate void CompleteActionDelegate(ulong handle, ref UniffiRustCallStatus status);

    private static async Task PollFuture(ulong rustFuture, Action<ulong, IntPtr, ulong> pollFunc)
    {
        byte pollResult;
        do
        {
            var tcs = new TaskCompletionSource<byte>(TaskCreationOptions.RunContinuationsAsynchronously);
            IntPtr callback = Marshal.GetFunctionPointerForDelegate(UniffiRustFutureContinuationCallback.callback);
            ulong mapEntry = _async_handle_map.Insert(tcs);
            pollFunc(rustFuture, callback, mapEntry);
            pollResult = await tcs.Task;
        }
        while(pollResult != UNIFFI_RUST_FUTURE_POLL_READY);
    }

    public static async Task<T> UniffiRustCallAsync<T, F, E>(
        ulong rustFuture,
        Action<ulong, IntPtr, ulong> pollFunc,
        CompleteFuncDelegate<F> completeFunc,
        Action<ulong> freeFunc,
        Func<F, T> liftFunc,
        CallStatusErrorHandler<E> errorHandler
    ) where E : System.Exception
    {
        try {
            await PollFuture(rustFuture, pollFunc);
            var result = _UniffiHelpers.RustCallWithError(errorHandler, (ref UniffiRustCallStatus status) => completeFunc(rustFuture, ref status));
            return liftFunc(result);
        }
        finally
        {
            freeFunc(rustFuture);
        }
    }

    public static async Task UniffiRustCallAsync<E>(
        ulong rustFuture,
        Action<ulong, IntPtr, ulong> pollFunc,
        CompleteActionDelegate completeFunc,
        Action<ulong> freeFunc,
        CallStatusErrorHandler<E> errorHandler
    ) where E : System.Exception
    {
         try {
            await PollFuture(rustFuture, pollFunc);
            _UniffiHelpers.RustCallWithError(errorHandler, (ref UniffiRustCallStatus status) => completeFunc(rustFuture, ref status));

        }
        finally
        {
            freeFunc(rustFuture);
        }
    }
}
