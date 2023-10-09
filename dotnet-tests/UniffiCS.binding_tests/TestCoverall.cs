/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Threading;
using System;
using uniffi.coverall;

public class TestCoverall {
    [Fact]
    public void FFIObjectSafeHandleDropsNativeReferenceOutsideOfUsingBlock() {
        Assert.Equal(0UL, CoverallMethods.GetNumAlive());
        var closure = () => {
            var coveralls = new Coveralls("safe_handle_drops_native_reference");
            Assert.Equal(1UL, CoverallMethods.GetNumAlive());
        };
        closure();
        GC.Collect();
        Thread.Sleep(10);
        Assert.Equal(0UL, CoverallMethods.GetNumAlive());
    }

    [Fact]
    public void TestCreateSomeDict() {
        using (var d = CoverallMethods.CreateSomeDict()) {
            Assert.Equal("text", d.text);
            Assert.Equal("maybe_text", d.maybeText);
            Assert.True(d.aBool);
            Assert.False(d.maybeABool);
            Assert.Equal((byte)1, d.unsigned8);
            Assert.Equal((byte)2, d.maybeUnsigned8);
            Assert.Equal((ushort)3, d.unsigned16);
            Assert.Equal((ushort)4, d.maybeUnsigned16);
            Assert.Equal(18446744073709551615UL, d.unsigned64);
            Assert.Equal(0ul, d.maybeUnsigned64);
            Assert.Equal((sbyte)8, d.signed8);
            Assert.Equal((sbyte)0, d.maybeSigned8);
            Assert.Equal(9223372036854775807L, d.signed64);
            Assert.Equal(0L, d.maybeSigned64);

            Assert.Equal(1.2345f, d.float32);
            Assert.Equal(22.0f / 7.0f, d.maybeFloat32);
            Assert.Equal(0.0, d.float64);
            Assert.Equal(1.0, d.maybeFloat64);

            Assert.Equal("some_dict", d.coveralls!.GetName());
        }
    }

    [Fact]
    public void TestArcs() {
        using (var coveralls = new Coveralls("test_arcs")) {
            Assert.Equal(1UL, CoverallMethods.GetNumAlive());
            // One ref held by the foreign-language code, one created for this method call.
            Assert.Equal(2UL, coveralls.StrongCount());
            Assert.Null(coveralls.GetOther());
            coveralls.TakeOther(coveralls);
            // Should now be a new strong ref, held by the object's reference to itself.
            Assert.Equal(3UL, coveralls.StrongCount());
            Assert.Equal(1UL, CoverallMethods.GetNumAlive());
            // Careful, this makes a new C# object which must be separately destroyed.
            using (var other = coveralls.GetOther()) {
                Assert.Equal("test_arcs", other!.GetName());
            }

            Assert.Throws<CoverallException.TooManyHoles>(() => coveralls.TakeOtherFallible());

            Assert.Throws<PanicException>(() => coveralls.TakeOtherPanic("expected panic: with an arc!"));

            Assert.Throws<PanicException>(() => coveralls.FalliblePanic("Expected panic in a fallible function!"));

            coveralls.TakeOther(null);
            Assert.Equal(2UL, coveralls.StrongCount());  
        }

        Assert.Equal(0UL, CoverallMethods.GetNumAlive());
    }

    [Fact]
    public void TestReturnObjects() {
        using (var coveralls = new Coveralls("test_return_objects")) {
            Assert.Equal(1UL, CoverallMethods.GetNumAlive());
            Assert.Equal(2UL, coveralls.StrongCount());
            using (var c2 = coveralls.CloneMe()) {
                Assert.Equal(coveralls.GetName(), c2.GetName());
                Assert.Equal(2UL, CoverallMethods.GetNumAlive());
                Assert.Equal(2UL, c2.StrongCount());

                coveralls.TakeOther(c2);
                // same number alive but `c2` has an additional ref count.
                Assert.Equal(2UL, CoverallMethods.GetNumAlive());
                Assert.Equal(2UL, coveralls.StrongCount());
                Assert.Equal(3UL, c2.StrongCount());
            }

            // Here we've dropped C# reference to `c2`, but the rust struct will not
            // be dropped as coveralls hold an `Arc<>` to it.
            Assert.Equal(2UL, CoverallMethods.GetNumAlive());
        }

        Assert.Equal(0UL, CoverallMethods.GetNumAlive());
    }

    [Fact]
    public void TestSimpleErrors() {
        using (var coveralls = new Coveralls("test_simple_errors")) {
            Assert.Throws<CoverallException.TooManyHoles>(() => coveralls.MaybeThrow(true));
            Assert.Throws<CoverallException.TooManyHoles>(() => coveralls.MaybeThrowInto(true));
            Assert.Throws<PanicException>(() => coveralls.Panic("oops"));
        }
    }

    [Fact]
    public void TestComplexErrors() {
        using (var coveralls = new Coveralls("test_complex_errors")) {
            Assert.True(coveralls.MaybeThrowComplex(0));

            var os_exception = Assert.Throws<ComplexException.OsException>(
                () => coveralls.MaybeThrowComplex(1));
            Assert.Equal(10, os_exception.code);
            Assert.Equal(20, os_exception.extendedCode);

            var permission_denied = Assert.Throws<ComplexException.PermissionDenied>(
                () => coveralls.MaybeThrowComplex(2));
            Assert.Equal("Forbidden", permission_denied.reason);

            Assert.Throws<ComplexException.UnknownException>(() => coveralls.MaybeThrowComplex(3));

            Assert.Throws<PanicException>(() => coveralls.MaybeThrowComplex(4));
        }
    }

    [Fact]
    public void TestInterfacesInDicts() {
        using (var coveralls = new Coveralls("test_interface_in_dicts")) {
            coveralls.AddPatch(new Patch(Color.Red));
            coveralls.AddRepair(new Repair(DateTime.Now, new Patch(Color.Blue)));
            Assert.Equal(2, coveralls.GetRepairs().Count);
        }
    }

    [Fact]
    public void MultiThreadedCallsWork() {
        // Make sure that there is no blocking during concurrent FFI calls.

        using (var counter = new ThreadsafeCounter()) {
            const int WAIT_MILLIS = 10;

            Thread blockingThread = new Thread(new ThreadStart(() => {
                counter.BusyWait(WAIT_MILLIS);
            }));

            var count = 0;
            Thread countingThread = new Thread(new ThreadStart(() => {
                for (int i = 0; i < WAIT_MILLIS; i++) {
                    // `count` is only incremented if another thread is blocking the counter.
                    // This ensures that both calls are running concurrently.
                    count = counter.IncrementIfBusy();
                    Thread.Sleep(1);
                }
            }));

            blockingThread.Start();
            countingThread.Start();
            blockingThread.Join();
            countingThread.Join();
            Assert.True(count > 0);
        }
    }

}
