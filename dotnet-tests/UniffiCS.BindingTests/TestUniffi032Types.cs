/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using uniffi.uniffi_032_types;

namespace UniffiCS.BindingTests;

// Types introduced by uniffi-rs 0.32.0: `HashSet<T>` and borrowed `&[u8]` arguments.
public class TestUniffi032Types
{
    [Fact]
    public void SetOfStringsRoundTrips()
    {
        var in_ = new HashSet<string> { "a", "b", "c" };
        Assert.Equal(in_, Uniffi032TypesMethods.IdentitySet(in_));
    }

    [Fact]
    public void SetOfIntsRoundTrips()
    {
        var in_ = new HashSet<int> { -1, 0, 7 };
        Assert.Equal(in_, Uniffi032TypesMethods.IdentityIntSet(in_));
    }

    [Fact]
    public void EmptySetRoundTrips()
    {
        Assert.Empty(Uniffi032TypesMethods.IdentitySet(new HashSet<string>()));
    }

    [Fact]
    public void SetDeduplicatesOnTheRustSide()
    {
        // Rust sees a HashSet, so duplicates collapse regardless of what we write.
        var in_ = new HashSet<string> { "x", "x", "y" };
        Assert.Equal(2, Uniffi032TypesMethods.IdentitySet(in_).Count);
    }

    [Fact]
    public void SetContainsIsEvaluatedByRust()
    {
        var in_ = new HashSet<string> { "needle" };
        Assert.True(Uniffi032TypesMethods.SetContains(in_, "needle"));
        Assert.False(Uniffi032TypesMethods.SetContains(in_, "missing"));
    }

    [Fact]
    public void OptionalSetRoundTrips()
    {
        Assert.Equal(6ul, Uniffi032TypesMethods.OptionalSet(new HashSet<ulong> { 1, 2, 3 }));
        Assert.Equal(0ul, Uniffi032TypesMethods.OptionalSet(null));
    }

    [Fact]
    public void ByrefBytesArePassedByPointer()
    {
        Assert.Equal(3u, Uniffi032TypesMethods.ByrefBytesLen([1, 2, 3]));
        Assert.Equal(6ul, Uniffi032TypesMethods.ByrefBytesSum([1, 2, 3]));
    }

    [Fact]
    public void ByrefEmptyBytesArePassedByPointer()
    {
        // A zero-length array still pins to a valid, non-null pointer.
        Assert.Equal(0u, Uniffi032TypesMethods.ByrefBytesLen([]));
        Assert.Equal(0ul, Uniffi032TypesMethods.ByrefBytesSum([]));
    }

    [Fact]
    public void ByrefBytesMixWithOwnedArgs()
    {
        Assert.Equal("a:2:b", Uniffi032TypesMethods.ByrefBytesMixed("a", [7, 8], "b"));
    }

    [Fact]
    public void TwoByrefBytesArgsStayPinnedTogether()
    {
        Assert.Equal(5u, Uniffi032TypesMethods.ByrefBytesConcatLen([1, 2], [3, 4, 5]));
    }

    [Fact]
    public void ByrefBytesOnFallibleCall()
    {
        Assert.Equal(9, Uniffi032TypesMethods.ByrefBytesFirst([9, 10]));
        Assert.Throws<ByteException.Empty>(() => Uniffi032TypesMethods.ByrefBytesFirst([]));
    }

    [Fact]
    public void ByrefBytesOnVoidCall()
    {
        Uniffi032TypesMethods.ByrefBytesIgnore([1, 2, 3]);
    }

    [Fact]
    public void ByrefBytesAsMethodArgument()
    {
        using var counter = new BytesCounter();
        Assert.Equal(2ul, counter.Add([1, 2]));
        Assert.Equal(5ul, counter.Add([3, 4, 5]));
    }

    [Fact]
    public void RecursiveEnumRoundTrips()
    {
        Tree tree = new Tree.Node(
            new Tree.Node(new Tree.Leaf(1), new Tree.Leaf(2)),
            new Tree.Leaf(3)
        );
        Assert.Equal(tree, Uniffi032TypesMethods.IdentityTree(tree));
        Assert.Equal(6L, Uniffi032TypesMethods.SumTree(tree));
    }

    [Fact]
    public void ExcludedFunctionIsNotGenerated()
    {
        // `excluded_function` is listed under `exclude` in the fixture's uniffi.toml.
        Assert.Null(typeof(Uniffi032TypesMethods).GetMethod("ExcludedFunction"));
        Assert.NotNull(typeof(Uniffi032TypesMethods).GetMethod("ByrefBytesLen"));
    }

    [Fact]
    public void ByrefBytesRoundTripLargeBuffer()
    {
        // Exercises a buffer big enough to land in the LOH, where pinning behaves differently.
        var big = new byte[100_000];
        for (int i = 0; i < big.Length; i++)
        {
            big[i] = (byte)(i % 251);
        }
        var expected = big.Aggregate(0ul, (acc, b) => acc + b);
        Assert.Equal((uint)big.Length, Uniffi032TypesMethods.ByrefBytesLen(big));
        Assert.Equal(expected, Uniffi032TypesMethods.ByrefBytesSum(big));
    }
}
