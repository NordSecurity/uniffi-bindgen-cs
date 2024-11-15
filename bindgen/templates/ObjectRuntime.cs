{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

internal abstract class FFIObject: IDisposable { 
    protected IntPtr pointer;

    private readonly AtomicBoolean _wasDestroyed = new AtomicBoolean(false);
    private readonly AtomicLong _callCounter = new AtomicLong(1L);

    protected FFIObject(IntPtr pointer) {
        this.pointer = pointer;
    }

    protected abstract void FreeRustArcPtr();

    public void Destroy()
    {
        // Only allow a single call to this method.
        if (_wasDestroyed.CompareAndSet(false, true))
        {
            // This decrement always matches the initial count of 1 given at creation time.
            if (_callCounter.DecrementAndGet() == 0)
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

    ~FFIObject() {
        Destroy();
    }

    private void IncrementCallCounter() {
        // Check and increment the call counter, to keep the object alive.
        // This needs a compare-and-set retry loop in case of concurrent updates.
        long count;
        do
        {
            count = _callCounter.Get();
            if (count == 0L) throw new System.ObjectDisposedException(String.Format("'{0}' object has already been destroyed", this.GetType().Name));
            if (count == long.MaxValue) throw new System.OverflowException(String.Format("'{0}' call counter would overflow", this.GetType().Name));

        } while(!_callCounter.CompareAndSet(count, count + 1L));
    }

    private void DecrementCallCounter() {
        // This decrement always matches the increment we performed above.
        if (_callCounter.DecrementAndGet() == 0L) {
            Dispose();
        }
    }

    internal void CallWithPointer(Action<IntPtr> action)
    {
        IncrementCallCounter();
         try {
            action(this.pointer);
         }
         finally {
            DecrementCallCounter();
         }
    }

    internal T CallWithPointer<T>(Func<IntPtr, T> func)
    {   
        IncrementCallCounter();
         try {
            return func(this.pointer);
         }
         finally {
            DecrementCallCounter();
         }
    }
}

class AtomicBoolean
{
    private int _value;

    public AtomicBoolean(bool initialValue) => _value = initialValue ? 1 : 0;
    public bool CompareAndSet(bool expected, bool newValue)
    {
        int expectedInt = expected ? 1 : 0;
        int newInt = newValue ? 1 : 0;
        return Interlocked.CompareExchange(ref _value, newInt, expectedInt) == expectedInt;
    }

    public bool Get() => _value == 1;
}


class AtomicLong
{
    private long _value;

    public AtomicLong(long initialValue) => _value = initialValue;

    public bool CompareAndSet(long expected, long newValue)
    {
        return Interlocked.CompareExchange(ref _value, newValue, expected) == expected;
    }

    public long Get() => _value;

    public long DecrementAndGet() => Interlocked.Decrement(ref _value);
}