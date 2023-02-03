/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using uniffi.callbacks;

class OnCallAnsweredImpl: OnCallAnswered {
    public int yesCount = 0;
    public int busyCount = 0;
    public string stringReceived = "";

    public String Hello() {
        yesCount += 1;
        return $"Hi hi {yesCount}";
    }

    public void Busy() {
        busyCount += 1;
    }

    public void TextReceived(String text) {
        stringReceived = text;
    }
}

public class TestCallbacks {
    [Fact]
    public void CallbackWorks() {
        var callback = new OnCallAnsweredImpl();
        var telephone = new Telephone();

        telephone.Call(true, callback);
        Assert.Equal(0, callback.busyCount);
        Assert.Equal(1, callback.yesCount);
        Assert.Equal("", callback.stringReceived);

        telephone.Call(true, callback);
        Assert.Equal(0, callback.busyCount);
        Assert.Equal(2, callback.yesCount);
        Assert.Equal("", callback.stringReceived);

        telephone.Call(false, callback);
        Assert.Equal(1, callback.busyCount);
        Assert.Equal(2, callback.yesCount);
        Assert.Equal("Not now, I'm on another call!", callback.stringReceived);

        var callback2 = new OnCallAnsweredImpl();
        telephone.Call(true, callback2);
        Assert.Equal(0, callback2.busyCount);
        Assert.Equal(1, callback2.yesCount);
        Assert.Equal("", callback2.stringReceived);

        telephone.Dispose();
    }

    [Fact]
    public void CallbackRegistrationIsNotAffectedByGC() {
        // See `static ForeignCallback INSTANCE` at `templates/CallbackInterfaceTemplate.cs`

        var callback = new OnCallAnsweredImpl();
        var telephone = new Telephone();

        // At this point, lib is holding references to managed delegates, so bindings have to
        // make sure that the delegate is not garbage collected.
        System.GC.Collect();

        telephone.Call(true, callback);
    }


    [Fact]
    public void CallbackReferenceIsDropped() {
        var telephone = new Telephone();

        var weak_callback = CallInItsOwnScope(() => {
            var callback = new OnCallAnsweredImpl();
            telephone.Call(true, callback);
            return new WeakReference(callback);
        });

        System.GC.Collect();
        Assert.False(weak_callback.IsAlive);
    }

    // https://stackoverflow.com/questions/15205891/garbage-collection-should-have-removed-object-but-weakreference-isalive-still-re
    private T CallInItsOwnScope<T>(Func<T> getter)
    {
        return getter();
    }
}

