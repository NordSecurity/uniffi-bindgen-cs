using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using uniffi.error_types;

namespace UniffiCS.BindingTests;


public class TestErrorTypes
{
    [Fact]
    public void SimpleObjectErrorThrow()
    {
        var e = Assert.Throws<ErrorInterface>(() => ErrorTypesMethods.Oops());
        Assert.Equal("because uniffi told me so\n\nCaused by:\n    oops", e.ToString());
        Assert.Equal(2, e.Chain().Count);
        Assert.Equal("because uniffi told me so", e.Link(0));
    }

    [Fact]
    public void NoWrapObjectErrorThrow()
    {
        var e = Assert.Throws<ErrorInterface>(() => ErrorTypesMethods.OopsNowrap());
        Assert.Equal("because uniffi told me so\n\nCaused by:\n    oops", e.ToString());
        Assert.Equal(2, e.Chain().Count);
        Assert.Equal("because uniffi told me so", e.Link(0));
    }

    [Fact]
    public void SimpleObjectErrorReturn()
    {
        ErrorInterface e = ErrorTypesMethods.GetError("the error");
        Assert.Equal("the error", e.ToString());
        Assert.Equal("the error", e.Link(0));
    }

    [Fact]
    public void TraitObjectErrorThrow()
    {
        var e = Assert.Throws<ErrorTrait>(() => ErrorTypesMethods.Toops());
        Assert.Equal("trait-oops", e.Msg());
    }

    [Fact]
    public void RichErrorThrow()
    {
        var e = Assert.Throws<RichException>(() => ErrorTypesMethods.ThrowRich("oh no"));
        Assert.Equal("RichError: \"oh no\"", e.ToString());
    }

    [Fact]
    public void EnumThrow()
    {
        var e1 = Assert.Throws<uniffi.error_types.Exception.Oops>(() => ErrorTypesMethods.OopsEnum(0));
        Assert.Contains("Oops", e1.ToString());

        var e2 = Assert.Throws<uniffi.error_types.Exception.Value>(() => ErrorTypesMethods.OopsEnum(1));
        Assert.Contains("Value", e2.ToString());
        Assert.Contains("value=value", e2.ToString());

        var e3 = Assert.Throws<uniffi.error_types.Exception.IntValue>(() => ErrorTypesMethods.OopsEnum(2));
        Assert.Contains("IntValue", e3.ToString());
        Assert.Contains("value=2", e3.ToString());

        var e4 = Assert.Throws<uniffi.error_types.Exception.FlatInnerException>(() => ErrorTypesMethods.OopsEnum(3));
        Assert.Contains("FlatInnerException", e4.ToString());
        Assert.Contains("CaseA: inner", e4.ToString());

        var e5 = Assert.Throws<uniffi.error_types.Exception.FlatInnerException>(() => ErrorTypesMethods.OopsEnum(4));
        Assert.Contains("FlatInnerException", e5.ToString());
        Assert.Contains("CaseB: NonUniffiTypeValue: value", e5.ToString());

        var e6 = Assert.Throws<uniffi.error_types.Exception.InnerException>(() => ErrorTypesMethods.OopsEnum(5));
        Assert.Contains("InnerException", e6.ToString());
        Assert.Contains("CaseA: v1=inner", e6.ToString());
    }

    [Fact]
    public void TupleThrow()
    {
        var t1 = Assert.Throws<TupleException.Oops>(() => ErrorTypesMethods.OopsTuple(0));
        Assert.Contains("Oops: v1=oops", t1.ToString());

        var t2 = Assert.Throws<TupleException.Value>(() => ErrorTypesMethods.OopsTuple(1));
        Assert.Contains("Value: v1=1", t2.ToString());
    }

    [Fact]
    public async Task AsyncThrow()
    {
        var e = await Assert.ThrowsAsync<ErrorInterface>(async () => await ErrorTypesMethods.Aoops());
        Assert.Equal("async-oops", e.ToString());
    }
}