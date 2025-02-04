{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{{ config.access_modifier() }} abstract class FFIObject: IDisposable { 
    protected IntPtr pointer;

    private int _wasDestroyed = 0;
    private long _callCounter = 1;

    protected FFIObject(IntPtr pointer) {
        this.pointer = pointer;
    }

    protected abstract void FreeRustArcPtr();
    protected abstract IntPtr CloneRustArcPtr();

    public void Destroy()
    {
        // Only allow a single call to this method.
        if (Interlocked.CompareExchange(ref _wasDestroyed, 1, 0) == 0)
        {
            // This decrement always matches the initial count of 1 given at creation time.
            if (Interlocked.Decrement(ref _callCounter) == 0)
            {
                FreeRustArcPtr();
            }
        }
    }

    public void Dispose()
    {
        Destroy();
        GC.SuppressFinalize(this); // Suppress finalization to avoid unnecessary GC overhead.
    }

    ~FFIObject() 
    {
        Destroy();
    }

    private void IncrementCallCounter() 
    {
        // Check and increment the call counter, to keep the object alive.
        // This needs a compare-and-set retry loop in case of concurrent updates.
        long count;
        do
        {
            count = Interlocked.Read(ref _callCounter);
            if (count == 0L) throw new System.ObjectDisposedException(String.Format("'{0}' object has already been destroyed", this.GetType().Name));
            if (count == long.MaxValue) throw new System.OverflowException(String.Format("'{0}' call counter would overflow", this.GetType().Name));

        } while (Interlocked.CompareExchange(ref _callCounter, count + 1, count) != count);
    }

    private void DecrementCallCounter() 
    {
        // This decrement always matches the increment we performed above.
        if (Interlocked.Decrement(ref _callCounter) == 0) {
            FreeRustArcPtr();
        }
    }

    internal void CallWithPointer(Action<IntPtr> action)
    {
        IncrementCallCounter();
        try {
            action(CloneRustArcPtr());
        }
        finally {
            DecrementCallCounter();
        }
    }

    internal T CallWithPointer<T>(Func<IntPtr, T> func)
    {   
        IncrementCallCounter();
        try {
            return func(CloneRustArcPtr());
        }
        finally {
            DecrementCallCounter();
        }
    }
}