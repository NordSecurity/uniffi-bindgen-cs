/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using uniffi.custom_types_builtin;

public class TestCustomTypesBuiltin {
    [Fact]
    public void CustomTypesWork() {
        var demo = CustomTypesBuiltinMethods.GetCustomTypesBuiltin();
        AssertDemo(demo);

        demo = CustomTypesBuiltinMethods.ReturnCustomTypesBuiltin(demo);
        AssertDemo(demo);
    }

    void AssertDemo(CustomTypesBuiltin demo) {
        Assert.Equal("Hello, world!", demo.@string);
        Assert.True(demo.boolean);
        Assert.Equal(SByte.MaxValue, demo.int8);
        Assert.Equal(Int16.MaxValue, demo.int16);
        Assert.Equal(Int32.MaxValue, demo.int32);
        Assert.Equal(Int64.MaxValue, demo.int64);
        Assert.Equal(Byte.MaxValue, demo.uint8);
        Assert.Equal(UInt16.MaxValue, demo.uint16);
        Assert.Equal(UInt32.MaxValue, demo.uint32);
        Assert.Equal(UInt64.MaxValue, demo.uint64);
        Assert.Equal(Single.MaxValue, demo.@float);
        Assert.Equal(Double.MaxValue, demo.@double);
    }
}
