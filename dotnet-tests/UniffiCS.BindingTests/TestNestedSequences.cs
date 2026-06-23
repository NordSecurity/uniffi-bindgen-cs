/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using uniffi.nested_sequences;

namespace UniffiCS.BindingTests;

public class TestNestedSequences
{
    [Fact]
    public void IdentityNestedBytesRoundTrip()
    {
        byte[][] in_ = [[1, 2], [3, 4]];
        Assert.Equal(in_, NestedSequencesMethods.IdentityNestedBytes(in_));
    }

    [Fact]
    public void IdentityNestedStringsRoundTrip()
    {
        string[][] in_ = [["a", "b"], ["c"]];
        Assert.Equal(in_, NestedSequencesMethods.IdentityNestedStrings(in_));
    }

    [Fact]
    public void MakeGrid2x3ReturnsZeroToFive()
    {
        var g = NestedSequencesMethods.MakeGrid(2, 3);
        Assert.Equal(2, g.Length);
        Assert.Equal(new uint[] { 0, 1, 2 }, g[0]);
        Assert.Equal(new uint[] { 3, 4, 5 }, g[1]);
    }

    [Fact]
    public void IdentityNestedIntsRoundTrip()
    {
        int[][] in_ = [[1, 2], [3]];
        Assert.Equal(in_, NestedSequencesMethods.IdentityNestedInts(in_));
    }
}
