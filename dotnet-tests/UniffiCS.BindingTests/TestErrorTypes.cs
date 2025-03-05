using System;
using System.Collections.Generic;
using uniffi.error_types;

namespace UniffiCS.BindingTests;


public class TestErrorTypes
{
    [Fact]
    public void SimpleObjectErrorThrow()
    {
        try {
            ErrorTypesMethods.Oops();
            throw new System.Exception("Should have failed");
        } catch (ErrorInterface e) {
            Assert.Equal("because uniffi told me so\n\nCaused by:\n    oops", e.ToString());
            Assert.Equal(2, e.Chain().Count);
            Assert.Equal("because uniffi told me so", e.Link(0));
        }
    }

    [Fact]
    public void NoWrapObjectErrorThrow()
    {
        try {
            ErrorTypesMethods.OopsNowrap();
            throw new System.Exception("Should have failed");
        } catch (ErrorInterface e) {
            Assert.Equal("because uniffi told me so\n\nCaused by:\n    oops", e.ToString());
            Assert.Equal(2, e.Chain().Count);
            Assert.Equal("because uniffi told me so", e.Link(0));
        }
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
        try {
            ErrorTypesMethods.Toops();
            throw new System.Exception("Should have failed");
        } catch (ErrorTrait e) {
            Assert.Equal("trait-oops", e.Msg());
        }
    }

    [Fact]
    public void RichErrorThrow()
    {
        try {
            ErrorTypesMethods.ThrowRich("oh no");
            throw new System.Exception("Should have failed");
        } catch (RichException e) {
            Assert.Equal("RichError: \"oh no\"", e.ToString());
        }
    }

    [Fact]
    public void EnumThrow()
    {
        try {
            ErrorTypesMethods.OopsEnum(0);
            throw new System.Exception("Should have failed");
        } catch (uniffi.error_types.Exception e) {
            Assert.Contains("Oops", e.ToString());
        }

        try {
            ErrorTypesMethods.OopsEnum(1);
            throw new System.Exception("Should have failed");
        } catch (uniffi.error_types.Exception e) {
            Assert.Contains("Value", e.ToString());
            Assert.Contains("value=value", e.ToString());
        }

        try {
            ErrorTypesMethods.OopsEnum(2);
            throw new System.Exception("Should have failed");
        } catch (uniffi.error_types.Exception e) {
            Assert.Contains("IntValue", e.ToString());
            Assert.Contains("value=2", e.ToString());
        }

        try {
            ErrorTypesMethods.OopsEnum(3);
            throw new System.Exception("Should have failed");
        } catch (uniffi.error_types.Exception.FlatInnerException e) {
            Assert.Contains("FlatInnerException", e.ToString());
            Assert.Contains("CaseA: inner", e.ToString());
        }

        try {
            ErrorTypesMethods.OopsEnum(4);
            throw new System.Exception("Should have failed");
        } catch (uniffi.error_types.Exception.FlatInnerException e) {
            Assert.Contains("FlatInnerException", e.ToString());
            Assert.Contains("CaseB: NonUniffiTypeValue: value", e.ToString());
        }

        try {
            ErrorTypesMethods.OopsEnum(5);
            throw new System.Exception("Should have failed");
        } catch (uniffi.error_types.Exception.InnerException e) {
            Assert.Contains("InnerException", e.ToString());
            Assert.Contains("CaseA: v1=inner", e.ToString());
        }
    }

    [Fact]
    public void TupleThrow()
    {
        try {
            ErrorTypesMethods.OopsTuple(0);
            throw new System.Exception("Should have failed");
        } catch (TupleException e) {
            Assert.Contains("Oops: v1=oops", e.ToString());
        }

        try {
            ErrorTypesMethods.OopsTuple(1);
            throw new System.Exception("Should have failed");
        } catch (TupleException e) {
            Assert.Contains("Value: v1=1", e.ToString());
        }
    }

    [Fact]
    public async void AsyncThrow()
    {
        try {
            await ErrorTypesMethods.Aoops();
            throw new System.Exception("Should have failed");
        } catch (ErrorInterface e) {
            Assert.Equal("async-oops", e.ToString());
        }
    }
}