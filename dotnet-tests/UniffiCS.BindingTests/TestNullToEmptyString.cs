// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using uniffi.null_to_empty_string;

namespace UniffiCS.BindingTests;

public class TestNullToEmptyString
{
    [Fact]
    public void NullToEmptyStringWorks()
    {
        Assert.Equal("hello", LibGreeter.HelloWorld("hello"));
        #pragma warning disable 8625 // Cannot convert null literal to non-nullable reference type
        Assert.Equal("", LibGreeter.HelloWorld(null));
        #pragma warning restore 8625
    }
}
