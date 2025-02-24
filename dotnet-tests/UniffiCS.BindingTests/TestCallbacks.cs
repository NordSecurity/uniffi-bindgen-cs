// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using uniffi.callbacks;

namespace UniffiCS.BindingTests;

class SomeOtherError : Exception { }

class CallAnswererImpl : CallAnswerer
{
    public String mode;

    public CallAnswererImpl(String mode)
    {
        this.mode = mode;
    }

    public String Answer()
    {
        if (mode == "normal")
        {
            return "Bonjour";
        }
        else if (mode == "busy")
        {
            throw new TelephoneException.Busy("I'm busy");
        }
        else
        {
            throw new SomeOtherError();
        }
    }
}

class OurSim : SimCard {
    public String name;

    public OurSim(String name) {
        this.name = name;
    }

    public String Name() {
        return name;
    }
}

public class TestCallbacks
{
    [Fact]
    public void CallbackWorks()
    {
        var rust_sim = CallbacksMethods.GetSimCards()[0];
        var our_sim = new OurSim("C#");
        var telephone = new Telephone();

        Assert.Equal("Bonjour", telephone.Call(rust_sim, new CallAnswererImpl("normal")));
        Assert.Equal("C# est bon marché", telephone.Call(our_sim, new CallAnswererImpl("normal")));

        Assert.Throws<TelephoneException.Busy>(() => telephone.Call(rust_sim, new CallAnswererImpl("busy")));

        Assert.Throws<TelephoneException.InternalTelephoneException>(
                () => telephone.Call(rust_sim, new CallAnswererImpl("something-else"))
        );
    }

    [Fact]
    public void CallbackRegistrationIsNotAffectedByGC()
    {
        // See `static ForeignCallback INSTANCE` at `templates/CallbackInterfaceTemplate.cs`
        var sims = CallbacksMethods.GetSimCards();

        var callback = new CallAnswererImpl("normal");
        var telephone = new Telephone();

        // At this point, lib is holding references to managed delegates, so bindings have to
        // make sure that the delegate is not garbage collected.
        System.GC.Collect();

        telephone.Call(sims[0], callback);
    }

    [Fact]
    public void CallbackReferenceIsDropped()
    {
        var telephone = new Telephone();
        var sims = CallbacksMethods.GetSimCards();

        var weak_callback = CallInItsOwnScope(() =>
        {
            var callback = new CallAnswererImpl("normal");
            telephone.Call(sims[0], callback);
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
