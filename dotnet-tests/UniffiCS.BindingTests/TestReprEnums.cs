using System;
using uniffi.uniffi_cs_repr_enums;

namespace UniffiCS.BindingTests;

public class TestReprEnums
{
    [Fact]
    public void DiscriminantValuesArePreserved()
    {
        Assert.Equal(-0x6001, (int)IndexExport.Update);
        Assert.Equal(0, (int)IndexExport.Snapshot);
        Assert.Equal(1, (int)IndexExport.Delete);
        Assert.Equal(0x4000, (int)IndexExport.Other);
        Assert.Equal(0xFFFFFFF0u, (uint)LargeUnsigned.Big);
        Assert.Equal(0u, (uint)LargeUnsigned.Zero);
    }

    [Fact]
    public void UnderlyingTypesAreCorrect()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(IndexExport)));
        Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(LargeUnsigned)));
    }

    [Fact]
    public void IndexExportRoundTrips()
    {
        Assert.Equal(IndexExport.Update, UniffiCsReprEnumsMethods.RoundtripIndex(IndexExport.Update));
        Assert.Equal(IndexExport.Snapshot, UniffiCsReprEnumsMethods.RoundtripIndex(IndexExport.Snapshot));
        Assert.Equal(IndexExport.Delete, UniffiCsReprEnumsMethods.RoundtripIndex(IndexExport.Delete));
        Assert.Equal(IndexExport.Other, UniffiCsReprEnumsMethods.RoundtripIndex(IndexExport.Other));
    }

    [Fact]
    public void LargeUnsignedRoundTrips()
    {
        Assert.Equal(LargeUnsigned.Zero, UniffiCsReprEnumsMethods.RoundtripLarge(LargeUnsigned.Zero));
        Assert.Equal(LargeUnsigned.Big, UniffiCsReprEnumsMethods.RoundtripLarge(LargeUnsigned.Big));
    }
}
