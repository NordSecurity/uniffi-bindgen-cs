{{ self.add_import("System.Threading")}}
{{ self.add_import("System.Threading.Tasks")}}

{% if self.include_once_check("ConcurrentHandleMap.cs") %}{% include "ConcurrentHandleMap.cs" %}{% endif %}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void UniFfiFutureCallback(IntPtr continuationHandle, byte pollResult);

internal static class _UniFFIAsync {
    internal const byte UNIFFI_RUST_FUTURE_POLL_READY = 0;
    // internal const byte UNIFFI_RUST_FUTURE_POLL_MAYBE_READY = 1;

    internal static ConcurrentHandleMap<TaskCompletionSource<byte>> _async_handle_map = new ConcurrentHandleMap<TaskCompletionSource<byte>>();
    public static ConcurrentHandleMap<CancellationTokenSource> _foreign_futures_map = new ConcurrentHandleMap<CancellationTokenSource>();

    // FFI type for Rust future continuations
    internal class UniffiRustFutureContinuationCallback
    {
        public static UniFfiFutureCallback callback = Callback;

        public static void Callback(IntPtr continuationHandle, byte pollResult)
        {
            if (_async_handle_map.Remove((ulong)continuationHandle.ToInt64(), out TaskCompletionSource<byte> task))
            {
                task.SetResult(pollResult);
            }
            else 
            {
                throw new InternalException($"Unable to find continuation handle: {continuationHandle}");
            }
        }
    }

    public class UniffiForeignFutureDroppedCallbackImpl
    {
        public static _UniFFILib.UniffiForeignFutureDroppedCallback callback = Callback;

        public static void Callback(ulong handle)
        {
            if (_foreign_futures_map.Remove(handle, out CancellationTokenSource task))
            {
                task.Cancel();
            }
            else
            {
                throw new InternalException($"Unable to find cancellation token: {handle}");
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
    ) where E : UniffiException
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
    ) where E : UniffiException
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