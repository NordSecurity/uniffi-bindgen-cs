using System;
using uniffi.uniffi_cs_ext_types_base;
using uniffi.uniffi_cs_ext_types_consumer;

namespace UniffiCS.BindingTests;

// Custom types defined in the base crate and used from the consumer crate. C# `using` aliases are
// file-scoped, so the consumer's generated file has to declare `BaseBlob` and `BaseUrl` itself.
// `BaseBlob` has no config and is a `byte[]`; `BaseUrl` is mapped to `System.Uri` by the base
// crate's `uniffi.toml`, and the consumer inherits that mapping.
public class TestExtTypesCSharpCustomTypes
{
    [Fact]
    public void TestBlobFromBaseIntoConsumer()
    {
        var blob = UniffiCsExtTypesBaseMethods.MakeBaseBlob(4);
        Assert.IsType<byte[]>(blob);
        Assert.Equal(4u, UniffiCsExtTypesConsumerMethods.BaseBlobLen(blob));
    }

    [Fact]
    public void TestBlobLiteral()
    {
        Assert.Equal(3u, UniffiCsExtTypesConsumerMethods.BaseBlobLen(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void TestMaybeBlobNone()
    {
        Assert.Null(UniffiCsExtTypesConsumerMethods.GetMaybeBaseBlob(null));
    }

    [Fact]
    public void TestMaybeBlobSome()
    {
        var result = UniffiCsExtTypesConsumerMethods.GetMaybeBaseBlob(new byte[] { 7, 8 });
        Assert.Equal(new byte[] { 7, 8 }, result);
    }

    [Fact]
    public void TestBlobList()
    {
        var result = UniffiCsExtTypesConsumerMethods.GetBaseBlobs(
            new[] { new byte[] { 1 }, new byte[] { 2, 2 } });
        Assert.Equal(2, result.Length);
        Assert.Equal(new byte[] { 1 }, result[0]);
        Assert.Equal(new byte[] { 2, 2 }, result[1]);
    }

    [Fact]
    public void TestUrlFromBaseIntoConsumer()
    {
        var url = UniffiCsExtTypesBaseMethods.MakeBaseUrl("https://example.com/path");
        Assert.IsType<Uri>(url);
        Assert.Equal("https://example.com/path", UniffiCsExtTypesConsumerMethods.BaseUrlString(url));
    }

    [Fact]
    public void TestUrlLiteral()
    {
        var result = UniffiCsExtTypesConsumerMethods.BaseUrlString(new Uri("https://example.com/"));
        Assert.Equal("https://example.com/", result);
    }

    [Fact]
    public void TestMaybeUrlNone()
    {
        Assert.Null(UniffiCsExtTypesConsumerMethods.GetMaybeBaseUrl(null));
    }

    [Fact]
    public void TestMaybeUrlSome()
    {
        var result = UniffiCsExtTypesConsumerMethods.GetMaybeBaseUrl(new Uri("https://example.com/a"));
        Assert.NotNull(result);
        Assert.Equal("example.com", result!.Host);
    }

    [Fact]
    public void TestCustomTypesInRecord()
    {
        var holder = UniffiCsExtTypesConsumerMethods.HoldCustomTypes(
            new byte[] { 9 }, new Uri("https://example.com/r"));
        Assert.Equal(new byte[] { 9 }, holder.Blob);
        Assert.Equal("https://example.com/r", holder.Url.AbsoluteUri);
    }
}
