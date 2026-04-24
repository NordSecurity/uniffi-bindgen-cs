// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

// Regression test for loro-ffi compatibility:
// Enum variants where the field name (after PascalCase) collides with the variant class name.
// e.g. Insert { insert: Resource } → property InsertValue (renamed by enum_field_name)
//      Delete { delete: u32 }      → property DeleteValue
//      Retain { retain: u32 }      → property RetainValue
//
// Previously the generated Dispose() method accessed variant_value.Insert (the nested class),
// causing CS0572 and CS0119 errors.

using uniffi.loro_ffi_compat;

namespace UniffiCS.BindingTests;

public class TestLoroFfiCompat
{
    [Fact]
    public void InsertVariantFieldAccessUsesRenamedProperty()
    {
        var resource = new Resource("hello");
        var change = LoroFfiCompatMethods.MakeChangeInsert(resource);

        var insert = Assert.IsType<Change.Insert>(change);
        // Field 'insert' is renamed to 'InsertValue' to avoid collision with the nested class name
        Assert.Equal("hello", insert.InsertValue.GetName());
    }

    [Fact]
    public void DeleteVariantFieldAccessUsesRenamedProperty()
    {
        var change = LoroFfiCompatMethods.MakeChangeDelete(42u);

        var delete = Assert.IsType<Change.Delete>(change);
        // Field 'delete' is renamed to 'DeleteValue' to avoid collision with the nested class name
        Assert.Equal(42u, delete.DeleteValue);
    }

    [Fact]
    public void RetainVariantFieldAccessUsesRenamedProperty()
    {
        var change = LoroFfiCompatMethods.MakeChangeRetain(7u);

        var retain = Assert.IsType<Change.Retain>(change);
        // Field 'retain' is renamed to 'RetainValue' to avoid collision with the nested class name
        Assert.Equal(7u, retain.RetainValue);
    }

    [Fact]
    public void DisposeDoesNotThrow()
    {
        // Dispose() is generated because Change contains object references (Arc<Resource>).
        // Before the fix, Dispose() accessed variant_value.Insert (a nested type, not a property),
        // which caused CS0572 / CS0119 compile errors.
        var resource = new Resource("test");
        using var change = LoroFfiCompatMethods.MakeChangeInsert(resource);
        // If this compiles and runs without exception, the fix is working.
    }
}
