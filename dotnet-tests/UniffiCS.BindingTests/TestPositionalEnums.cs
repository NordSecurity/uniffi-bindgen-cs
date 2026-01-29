using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using uniffi.uniffi_cs_positional_enums;

namespace UniffiCS.BindingTests;


public class TestPositionalEnums
{
    [Fact]
    public void PositionalEnumsWork()
    {
        var good = new PositionalEnum.GoodVariant("Hi good");
        Assert.Equal("Hi good", good.V1);

        var nice = new PositionalEnum.NiceVariant(10, "Hi nice");
        Assert.Equal(10, nice.V1);
        Assert.Equal("Hi nice", nice.V2);
    }
}