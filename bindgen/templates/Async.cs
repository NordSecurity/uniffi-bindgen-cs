[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void UniFfiFutureCallback(IntPtr continuationHandle, byte pollResult);

internal static class _UniFFIAsync {
    internal const byte UNIFFI_RUST_FUTURE_POLL_READY = 0;
    // internal const byte UNIFFI_RUST_FUTURE_POLL_MAYBE_READY = 1;

    // FFI type for Rust future continuations
    internal class UniffiRustFutureContinuationCallback
    {
        public static UniFfiFutureCallback callback = Callback;

        public static void Callback(IntPtr continuationHandle, byte pollResult)
        {
            GCHandle handle = GCHandle.FromIntPtr(continuationHandle);
            TaskCompletionSource<byte> tcs = (TaskCompletionSource<byte>)handle.Target;
            tcs.SetResult(pollResult);
        }

        public static void Register()
        {
            IntPtr fn = Marshal.GetFunctionPointerForDelegate(callback);
            _UniFFILib.{{ ci.ffi_rust_future_continuation_callback_set().name() }}(fn);
        }
    }

    public delegate F CompleteFuncDelegate<F>(IntPtr ptr, ref RustCallStatus status);

    public delegate void CompleteActionDelegate(IntPtr ptr, ref RustCallStatus status);

    public static async Task<T> UniffiRustCallAsync<T, F, E>(
        IntPtr rustFuture,
        Action<IntPtr, IntPtr> pollFunc,
        CompleteFuncDelegate<F> completeFunc,
        Action<IntPtr> freeFunc,
        Func<F, T> liftFunc,
        CallStatusErrorHandler<E> errorHandler
    ) where E : UniffiException
    {
        try {
            byte pollResult;
            do 
            {
                var tcs = new TaskCompletionSource<byte>();
                var handle = GCHandle.Alloc(tcs);
                pollFunc(rustFuture, GCHandle.ToIntPtr(handle));
                pollResult = await tcs.Task;
            }
            while(pollResult != UNIFFI_RUST_FUTURE_POLL_READY);

            var result = _UniffiHelpers.RustCallWithError(errorHandler, (ref RustCallStatus status) => completeFunc(rustFuture, ref status));
            return liftFunc(result);
        }
        finally
        {
            freeFunc(rustFuture);
        }
    }

    public static async Task UniffiRustCallAsync<E>(
        IntPtr rustFuture,
        Action<IntPtr, IntPtr> pollFunc,
        CompleteActionDelegate completeFunc,
        Action<IntPtr> freeFunc,
        CallStatusErrorHandler<E> errorHandler
    ) where E : UniffiException
    {
         try {
            byte pollResult;
            do 
            {
                var tcs = new TaskCompletionSource<byte>();
                var handle = GCHandle.Alloc(tcs);
                pollFunc(rustFuture, GCHandle.ToIntPtr(handle));
                pollResult = await tcs.Task;
            }
            while(pollResult != UNIFFI_RUST_FUTURE_POLL_READY);

            _UniffiHelpers.RustCallWithError(errorHandler, (ref RustCallStatus status) => completeFunc(rustFuture, ref status));

        }
        finally
        {
            freeFunc(rustFuture);
        }
    }
}