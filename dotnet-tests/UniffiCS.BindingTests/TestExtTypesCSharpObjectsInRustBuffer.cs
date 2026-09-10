using System.Collections.Generic;
using uniffi.uniffi_cs_ext_types_base;
using uniffi.uniffi_cs_ext_types_consumer;

namespace UniffiCS.BindingTests;

// External objects and trait interfaces inside a RustBuffer: optional, sequence, map and record
// field positions. A plain object argument crosses the FFI as a handle and is covered by
// TestExtTypesCSharp; these positions are serialized through the consumer's own BigEndianStream.
public class TestExtTypesCSharpObjectsInRustBuffer
{
    [Fact]
    public void TestMaybeInterfaceNone()
    {
        Assert.Null(UniffiCsExtTypesConsumerMethods.GetMaybeBaseInterface(null));
    }

    [Fact]
    public void TestMaybeInterfaceSome()
    {
        var result = UniffiCsExtTypesConsumerMethods.GetMaybeBaseInterface(new BaseInterface("maybe"));
        Assert.NotNull(result);
        Assert.Equal("maybe", result!.Label());
    }

    [Fact]
    public void TestInterfaceList()
    {
        var result = UniffiCsExtTypesConsumerMethods.GetBaseInterfaces(
            new[] { new BaseInterface("a"), new BaseInterface("b") });
        Assert.Equal(2, result.Length);
        Assert.Equal("a", result[0].Label());
        Assert.Equal("b", result[1].Label());
    }

    [Fact]
    public void TestInterfaceListEmpty()
    {
        var result = UniffiCsExtTypesConsumerMethods.GetBaseInterfaces(new BaseInterface[0]);
        Assert.Empty(result);
    }

    [Fact]
    public void TestInterfaceMap()
    {
        var input = new Dictionary<string, BaseInterface>
        {
            ["x"] = new BaseInterface("ex"),
            ["y"] = new BaseInterface("why"),
        };
        var result = UniffiCsExtTypesConsumerMethods.GetBaseInterfaceMap(input);
        Assert.Equal(2, result.Count);
        Assert.Equal("ex", result["x"].Label());
        Assert.Equal("why", result["y"].Label());
    }

    [Fact]
    public void TestInterfaceInRecord()
    {
        var holder = UniffiCsExtTypesConsumerMethods.WrapBaseInterface(new BaseInterface("held"), "tag");
        Assert.Equal("held", holder.Iface.Label());
        Assert.Equal("tag", holder.Label);
    }

    [Fact]
    public void TestMaybeTraitNone()
    {
        Assert.Null(UniffiCsExtTypesConsumerMethods.GetMaybeBaseTrait(null));
    }

    [Fact]
    public void TestMaybeTraitSome()
    {
        var result = UniffiCsExtTypesConsumerMethods.GetMaybeBaseTrait(new CSharpTrait("hi"));
        Assert.NotNull(result);
        Assert.Equal("hi", result!.Greet());
    }

    [Fact]
    public void TestTraitList()
    {
        var greetings = UniffiCsExtTypesConsumerMethods.GreetAll(
            new BaseTrait[] { new CSharpTrait("one"), new CSharpTrait("two") });
        Assert.Equal(new[] { "one", "two" }, greetings);
    }

    class CSharpTrait : BaseTrait
    {
        private readonly string greeting;

        public CSharpTrait(string greeting)
        {
            this.greeting = greeting;
        }

        public string Greet() => greeting;
    }
}
