// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using uniffi.custom_types_builtin;

namespace UniffiCS.BindingTests;

public class TestCustomTypesBuiltin
{
    [Fact]
    public void CustomTypesWork()
    {
        var demo = CustomTypesBuiltinMethods.GetCustomTypesBuiltin();
        AssertDemo(demo);

        demo = CustomTypesBuiltinMethods.ReturnCustomTypesBuiltin(demo);
        AssertDemo(demo);
    }

    void AssertDemo(CustomTypesBuiltin demo)
    {
        Assert.Equal("Hello, world!", demo.@String);
        Assert.Equal(new List<String> { "Hello, world!" }, demo.Array);
        Assert.Equal(new Dictionary<String, String> { { "hello", "world" } }, demo.Table);
        Assert.True(demo.Boolean);
        Assert.Equal(SByte.MaxValue, demo.Int8);
        Assert.Equal(Int16.MaxValue, demo.Int16);
        Assert.Equal(Int32.MaxValue, demo.Int32);
        Assert.Equal(Int64.MaxValue, demo.Int64);
        Assert.Equal(Byte.MaxValue, demo.Uint8);
        Assert.Equal(UInt16.MaxValue, demo.Uint16);
        Assert.Equal(UInt32.MaxValue, demo.Uint32);
        Assert.Equal(UInt64.MaxValue, demo.Uint64);
        Assert.Equal(Single.MaxValue, demo.@Float);
        Assert.Equal(Double.MaxValue, demo.@Double);
    }
}
