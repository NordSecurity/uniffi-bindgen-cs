// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using uniffi.coverall;

namespace UniffiCS.BindingTests;

public class TestCoverall
{
    [Fact]
    public void ObjectDecrementsReference()
    {
        Assert.Equal(0UL, CoverallMethods.GetNumAlive());
        var closure = () =>
        {
            var coveralls = new Coveralls("FFIObject_drops_native_reference");
            Assert.Equal(1UL, CoverallMethods.GetNumAlive());
        };
        closure();
        GC.Collect();
        Thread.Sleep(10);
        Assert.Equal(0UL, CoverallMethods.GetNumAlive());
    }

    [Fact]
    public void TestCreateSomeDict()
    {
        using (var d = CoverallMethods.CreateSomeDict())
        {
            Assert.Equal("text", d.Text);
            Assert.Equal("maybe_text", d.MaybeText);
            Assert.True(d.ABool);
            Assert.False(d.MaybeABool);
            Assert.Equal((byte)1, d.Unsigned8);
            Assert.Equal((byte)2, d.MaybeUnsigned8);
            Assert.Equal((ushort)3, d.Unsigned16);
            Assert.Equal((ushort)4, d.MaybeUnsigned16);
            Assert.Equal(18446744073709551615UL, d.Unsigned64);
            Assert.Equal(0ul, d.MaybeUnsigned64);
            Assert.Equal((sbyte)8, d.Signed8);
            Assert.Equal((sbyte)0, d.MaybeSigned8);
            Assert.Equal(9223372036854775807L, d.Signed64);
            Assert.Equal(0L, d.MaybeSigned64);

            Assert.Equal(1.2345f, d.Float32);
            Assert.Equal(22.0f / 7.0f, d.MaybeFloat32);
            Assert.Equal(0.0, d.Float64);
            Assert.Equal(1.0, d.MaybeFloat64);

            Assert.Equal("some_dict", d.Coveralls!.GetName());
        }
    }

    [Fact]
    public void TestArcs()
    {
        using (var coveralls = new Coveralls("test_arcs"))
        {
            Assert.Equal(1UL, CoverallMethods.GetNumAlive());
            // One ref held by the foreign-language code, one created for this method call.
            Assert.Equal(2UL, coveralls.StrongCount());
            Assert.Null(coveralls.GetOther());
            coveralls.TakeOther(coveralls);
            // Should now be a new strong ref, held by the object's reference to itself.
            Assert.Equal(3UL, coveralls.StrongCount());
            Assert.Equal(1UL, CoverallMethods.GetNumAlive());
            // Careful, this makes a new C# object which must be separately destroyed.
            using (var other = coveralls.GetOther())
            {
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
    public void TestReturnObjects()
    {
        using (var coveralls = new Coveralls("test_return_objects"))
        {
            Assert.Equal(1UL, CoverallMethods.GetNumAlive());
            Assert.Equal(2UL, coveralls.StrongCount());
            using (var c2 = coveralls.CloneMe())
            {
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
    public void TestSimpleErrors()
    {
        using (var coveralls = new Coveralls("test_simple_errors"))
        {
            Assert.Throws<CoverallException.TooManyHoles>(() => coveralls.MaybeThrow(true));
            Assert.Throws<CoverallException.TooManyHoles>(() => coveralls.MaybeThrowInto(true));
            Assert.Throws<PanicException>(() => coveralls.Panic("oops"));
        }
    }

    [Fact]
    public void TestComplexErrors()
    {
        using (var coveralls = new Coveralls("test_complex_errors"))
        {
            Assert.True(coveralls.MaybeThrowComplex(0));

            var os_exception = Assert.Throws<ComplexException.OsException>(() => coveralls.MaybeThrowComplex(1));
            Assert.Equal(10, os_exception.code);
            Assert.Equal(20, os_exception.extendedCode);

            var permission_denied = Assert.Throws<ComplexException.PermissionDenied>(
                () => coveralls.MaybeThrowComplex(2)
            );
            Assert.Equal("Forbidden", permission_denied.reason);

            Assert.Throws<ComplexException.UnknownException>(() => coveralls.MaybeThrowComplex(3));

            Assert.Throws<PanicException>(() => coveralls.MaybeThrowComplex(4));
        }
    }

    [Fact]
    public void TestErrorValues()
    {
        using (var _coveralls = new Coveralls("test_error_values"))
        {
            var rootError = Assert.Throws<RootException.Complex>(() => CoverallMethods.ThrowRootError());
            Assert.True(rootError.error is ComplexException.OsException);

            var otherError = CoverallMethods.GetRootError();
            Assert.True(otherError is RootException.Other);
            Assert.Equal(OtherError.Unexpected, ((RootException.Other)otherError).error);

            var complexError = CoverallMethods.GetComplexError(null);
            Assert.True(complexError is ComplexException.PermissionDenied);
            Assert.Null(CoverallMethods.GetErrorDict(null).ComplexError);
        }
    }

    [Fact]
    public void TestInterfacesInDicts()
    {
        using (var coveralls = new Coveralls("test_interface_in_dicts"))
        {
            coveralls.AddPatch(new Patch(Color.Red));
            coveralls.AddRepair(new Repair(DateTime.Now, new Patch(Color.Blue)));
            Assert.Equal(2, coveralls.GetRepairs().Length);
        }
    }

    [Fact]
    public void TestRegressions()
    {
        using (var coveralls = new Coveralls("test_tregressions"))
        {
            Assert.Equal("status: success", coveralls.GetStatus("success"));
        }
    }

    [Fact]
    public void TestEmptyRecords()
    {
        using (var coveralls = new Coveralls("test_empty_records"))
        {
            var es = new EmptyStruct();
            Assert.Equal(coveralls.SetAndGetEmptyStruct(es), es);
        }
    }

    class CSharpGetters : Getters
    {
        public Boolean GetBool(Boolean v, Boolean argumentTwo)
        {
            return v ^ argumentTwo;
        }

        public String GetString(String v, Boolean arg2)
        {
            if (v == "too-many-holes")
            {
                throw new CoverallException.TooManyHoles("too many holes");
            }
            if (v == "unexpected-error")
            {
                throw new PanicException("something failed");
            }
            return arg2 ? v.ToUpper() : v;
        }

        public String? GetOption(String v, Boolean arg2)
        {
            if (v == "os-error")
            {
                throw new ComplexException.OsException(100, 200);
            }
            if (v == "unknown-error")
            {
                throw new ComplexException.UnknownException();
            }
            if (arg2)
            {
                return v != "" ? v.ToUpper() : null;
            }
            else
            {
                return v;
            }
        }

        public int[] GetList(int[] v, Boolean arg2)
        {
            return arg2 ? v : [];
        }


        public void GetNothing(String v)
        {}

        public Coveralls RoundTripObject(Coveralls coveralls)
        {
            return coveralls;
        }
    }

    private void TestGettersFromCSharp(Getters getters)
    {
        Assert.False(getters.GetBool(true, true));
        Assert.True(getters.GetBool(true, false));
        Assert.True(getters.GetBool(false, true));
        Assert.False(getters.GetBool(false, false));

        Assert.Equal("hello", getters.GetString("hello", false));
        Assert.Equal("HELLO", getters.GetString("hello", true));

        Assert.Equal("HELLO", getters.GetOption("hello", true));
        Assert.Equal("hello", getters.GetOption("hello", false));
        Assert.Null(getters.GetOption("", true));

        int[] l = [1, 2, 3];
        Assert.Equal(l, getters.GetList(l, true));
        Assert.Equal([], getters.GetList(l, false));

        getters.GetNothing("hello");

        Assert.Throws<CoverallException.TooManyHoles>(() => getters.GetString("too-many-holes", true));
        Assert.Throws<ComplexException.OsException>(() => getters.GetOption("os-error", true));
        Assert.Throws<ComplexException.UnknownException>(() => getters.GetOption("unknown-error", true));
        Assert.Throws<PanicException>(() => getters.GetString("unexpected-error", true));
    }

    [Fact]
    public void TestTraitsImplementedInRust()
    {
        var rustGetters = CoverallMethods.MakeRustGetters();
        CoverallMethods.TestGetters(rustGetters);
        TestGettersFromCSharp(rustGetters);
    }

    [Fact]
    public void TestTraitsImplementedInCSharp()
    {
        var cSharpGetters = new CSharpGetters();
        CoverallMethods.TestGetters(cSharpGetters);
        TestGettersFromCSharp(cSharpGetters);
    }

    class CSharpNode : NodeTrait
    {
        NodeTrait? currentParent = null;

        public String Name()
        {
            return "node-c#";
        }

        public void SetParent(NodeTrait? parent)
        {
            currentParent = parent;
        }

        public NodeTrait? GetParent()
        {
            return currentParent;
        }

        public ulong StrongCount()
        {
            return 0;
        }
    }

    [Fact]
    public void TestNodeTrait()
    {
        var traits = CoverallMethods.GetTraits();
        Assert.Equal("node-1", traits[0].Name());
        Assert.Equal(2UL, traits[0].StrongCount());

        Assert.Equal("node-2", traits[1].Name());
        Assert.Equal(2UL, traits[1].StrongCount());

        var l1 = new List<String> { "node-2" };
        traits[0].SetParent(traits[1]);
        Assert.Equal(l1, CoverallMethods.AncestorNames(traits[0]));
        Assert.Empty(CoverallMethods.AncestorNames(traits[1]));
        Assert.Equal(2UL, traits[1].StrongCount());
        Assert.Equal("node-2", traits[0].GetParent()!.Name());

        var csNode = new CSharpNode();
        traits[1].SetParent(csNode);

        var pl0 = new List<String> { "node-2", "node-c#" };
        var pl1 = new List<String> { "node-c#" };
        Assert.Equal(pl0, CoverallMethods.AncestorNames(traits[0]));
        Assert.Equal(pl1, CoverallMethods.AncestorNames(traits[1]));
        Assert.Empty(CoverallMethods.AncestorNames(csNode));

        traits[1].SetParent(null);
        csNode.SetParent(traits[0]);
        var cs = new List<String> { "node-1", "node-2" };
        var l0 = new List<String> { "node-2" };
        Assert.Equal(cs, CoverallMethods.AncestorNames(csNode));
        Assert.Equal(l0, CoverallMethods.AncestorNames(traits[0]));
        Assert.Empty(CoverallMethods.AncestorNames(traits[1]));

        csNode.SetParent(null);
        traits[0].SetParent(null);
    }

    [Fact]
    public void TestRoundTrips()
    {
        var rustGetters = CoverallMethods.MakeRustGetters();
        CoverallMethods.TestRoundTripThroughRust(rustGetters);
        var cSharpGetters = new CSharpGetters();
        CoverallMethods.TestRoundTripThroughForeign(cSharpGetters);
        GC.Collect();
    }

    [Fact]
    public void TestStringUtil()
    {
        var traits = CoverallMethods.GetStringUtilTraits();
        Assert.Equal("cowboy", traits[0].Concat("cow", "boy"));
        Assert.Equal("cowboy", traits[1].Concat("cow", "boy"));
    }

    [Fact]
    public void TestDictWithDefaults()
    {
        var d = new DictWithDefaults();
        Assert.Equal("default-value", d.Name);
        Assert.Equal(31UL, d.Integer);
        Assert.Null(d.Category);

        var d1 = new DictWithDefaults("this", "that", 42UL);
        Assert.Equal("this", d1.Name);
        Assert.Equal("that", d1.Category);
        Assert.Equal(42UL, d1.Integer);
    }

    [Fact]
    public void MultiThreadedCallsWork()
    {
        // Make sure that there is no blocking during concurrent FFI calls.

        using (var counter = new ThreadsafeCounter())
        {
            const int WAIT_MILLIS = 20;

            Thread blockingThread = new Thread(
                new ThreadStart(() =>
                {
                    counter.BusyWait(WAIT_MILLIS);
                })
            );

            var count = 0;
            Thread countingThread = new Thread(
                new ThreadStart(() =>
                {
                    for (int i = 0; i < WAIT_MILLIS; i++)
                    {
                        // `count` is only incremented if another thread is blocking the counter.
                        // This ensures that both calls are running concurrently.
                        count = counter.IncrementIfBusy();
                        Thread.Sleep(1);
                    }
                })
            );

            blockingThread.Start();
            countingThread.Start();
            blockingThread.Join();
            countingThread.Join();
            Assert.True(count > 0);
        }
    }

    [Fact]
    public void TestBytes()
    {
        using (var coveralls = new Coveralls("test_bytes"))
        {
            Assert.Equal(new byte[] { 3, 2, 1 }, coveralls.Reverse(new byte[] { 1, 2, 3 }));
        }
    }
}
