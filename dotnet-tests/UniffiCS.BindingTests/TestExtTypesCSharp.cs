using System.Collections.Generic;
using System.Linq;
using uniffi.uniffi_cs_ext_types_base;
using uniffi.uniffi_cs_ext_types_consumer;

namespace UniffiCS.BindingTests;

public class TestExtTypesCSharp
{
    [Fact]
    public void TestCompositeRecord()
    {
        var composite = UniffiCsExtTypesConsumerMethods.CreateComposite("test", 42, "extra");
        Assert.Equal("test", composite.Base.Name);
        Assert.Equal(42, composite.Base.Value);
        Assert.Equal("extra", composite.Extra);
        Assert.IsType<BaseEnum.Alpha>(composite.Variant);
    }

    [Fact]
    public void TestCompositeRoundTrip()
    {
        var composite = UniffiCsExtTypesConsumerMethods.CreateComposite("round", 7, "trip");
        var baseRecord = UniffiCsExtTypesConsumerMethods.GetBaseFromComposite(composite);
        Assert.Equal("round", baseRecord.Name);
        Assert.Equal(7, baseRecord.Value);
    }

    [Fact]
    public void TestExternalInterface()
    {
        var iface = UniffiCsExtTypesConsumerMethods.GetBaseInterface("hello");
        Assert.Equal("hello", iface.Label());
    }

    [Fact]
    public void TestExternalTraitFromCSharp()
    {
        var greeting = UniffiCsExtTypesConsumerMethods.InvokeExternalTrait(new CSharpBaseTrait());
        Assert.Equal("Hello from C#!", greeting);
    }

    [Fact]
    public void TestExternalTraitFromBase()
    {
        var greeting = UniffiCsExtTypesBaseMethods.InvokeBaseTrait(new CSharpBaseTrait());
        Assert.Equal("Hello from C#!", greeting);
    }

    [Fact]
    public void TestExternalError()
    {
        var e = Assert.Throws<BaseException.General>(
            () => UniffiCsExtTypesConsumerMethods.ThrowExternalError());
        Assert.Contains("external error", e.ToString());
    }

    [Fact]
    public void TestMaybeRecordNone()
    {
        var r = UniffiCsExtTypesConsumerMethods.GetMaybeBaseRecord(null);
        Assert.Equal("default", r.Name);
        Assert.Equal(0, r.Value);
    }

    [Fact]
    public void TestMaybeRecordSome()
    {
        var input = UniffiCsExtTypesBaseMethods.CreateBaseRecord("custom", 99);
        var r = UniffiCsExtTypesConsumerMethods.GetMaybeBaseRecord(input);
        Assert.Equal("custom", r.Name);
        Assert.Equal(99, r.Value);
    }

    [Fact]
    public void TestRecordList()
    {
        var r1 = UniffiCsExtTypesBaseMethods.CreateBaseRecord("a", 1);
        var r2 = UniffiCsExtTypesBaseMethods.CreateBaseRecord("b", 2);
        var result = UniffiCsExtTypesConsumerMethods.GetBaseRecords(new[] { r1, r2 });
        Assert.Equal(2, result.Length);
        Assert.Equal("a", result[0].Name);
        Assert.Equal("b", result[1].Name);
    }

    [Fact]
    public void TestMaybeEnumNone()
    {
        var e = UniffiCsExtTypesConsumerMethods.GetMaybeBaseEnum(null);
        Assert.Null(e);
    }

    [Fact]
    public void TestMaybeEnumSome()
    {
        var e = UniffiCsExtTypesConsumerMethods.GetMaybeBaseEnum(new BaseEnum.Alpha());
        Assert.NotNull(e);
        Assert.IsType<BaseEnum.Alpha>(e);
    }

    [Fact]
    public void TestBaseEnumFromBase()
    {
        var e = UniffiCsExtTypesBaseMethods.GetBaseEnumAlpha();
        Assert.IsType<BaseEnum.Alpha>(e);
    }

    [Fact]
    public void TestBaseRecordFromBase()
    {
        var r = UniffiCsExtTypesBaseMethods.CreateBaseRecord("direct", 55);
        Assert.Equal("direct", r.Name);
        Assert.Equal(55, r.Value);
    }

    [Fact]
    public void TestBaseErrorFromBase()
    {
        var e = Assert.Throws<BaseException.General>(
            () => UniffiCsExtTypesBaseMethods.ThrowBaseError("base error"));
        Assert.Contains("base error", e.ToString());
    }

    class CSharpBaseTrait : BaseTrait
    {
        public string Greet() => "Hello from C#!";
    }
}
