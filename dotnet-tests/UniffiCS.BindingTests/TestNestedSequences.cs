// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using uniffi.nested_sequences;

namespace UniffiCS.BindingTests;

public class TestNestedSequences
{
    [Fact]
    public void TestNestedBytes()
    {
        byte[][] input = [
            [1, 2, 3],
            [4, 5],
            [],
        ];
        var result = NestedSequencesMethods.NestedBytes(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void TestNestedStrings()
    {
        string[][] input = [
            ["hello", "world"],
            ["foo"],
            [],
        ];
        var result = NestedSequencesMethods.NestedStrings(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void TestEmptyNestedSequence()
    {
        byte[][] input = [];
        var result = NestedSequencesMethods.NestedBytes(input);
        Assert.Equal(input, result);
    }
}
