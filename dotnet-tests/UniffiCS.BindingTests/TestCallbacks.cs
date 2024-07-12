// // This Source Code Form is subject to the terms of the Mozilla Public
// // License, v. 2.0. If a copy of the MPL was not distributed with this
// // file, You can obtain one at http://mozilla.org/MPL/2.0/.

// using System;
// using uniffi.callbacks;

// namespace UniffiCS.BindingTests;

// class SomeOtherError : Exception { }

// class CallAnswererImpl : CallAnswerer
// {
//     public String mode;

//     public CallAnswererImpl(String mode)
//     {
//         this.mode = mode;
//     }

//     public String Answer()
//     {
//         if (mode == "normal")
//         {
//             return "Bonjour";
//         }
//         else if (mode == "busy")
//         {
//             throw new TelephoneException.Busy("I'm busy");
//         }
//         else
//         {
//             throw new SomeOtherError();
//         }
//     }
// }

// public class TestCallbacks
// {
//     [Fact]
//     public void CallbackWorks()
//     {
//         using (var telephone = new Telephone())
//         {
//             Assert.Equal("Bonjour", telephone.Call(new CallAnswererImpl("normal")));

//             Assert.Throws<TelephoneException.Busy>(() => telephone.Call(new CallAnswererImpl("busy")));

//             Assert.Throws<TelephoneException.InternalTelephoneException>(
//                 () => telephone.Call(new CallAnswererImpl("something-else"))
//             );
//         }
//     }

//     [Fact]
//     public void CallbackRegistrationIsNotAffectedByGC()
//     {
//         // See `static ForeignCallback INSTANCE` at `templates/CallbackInterfaceTemplate.cs`

//         var callback = new CallAnswererImpl("normal");
//         var telephone = new Telephone();

//         // At this point, lib is holding references to managed delegates, so bindings have to
//         // make sure that the delegate is not garbage collected.
//         System.GC.Collect();

//         telephone.Call(callback);
//     }

//     [Fact]
//     public void CallbackReferenceIsDropped()
//     {
//         var telephone = new Telephone();

//         var weak_callback = CallInItsOwnScope(() =>
//         {
//             var callback = new CallAnswererImpl("normal");
//             telephone.Call(callback);
//             return new WeakReference(callback);
//         });

//         System.GC.Collect();
//         Assert.False(weak_callback.IsAlive);
//     }

//     // https://stackoverflow.com/questions/15205891/garbage-collection-should-have-removed-object-but-weakreference-isalive-still-re
//     private T CallInItsOwnScope<T>(Func<T> getter)
//     {
//         return getter();
//     }
// }
