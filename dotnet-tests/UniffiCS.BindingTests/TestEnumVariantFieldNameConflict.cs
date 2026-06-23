/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using uniffi.enum_variant_field_name_conflict;

namespace UniffiCS.BindingTests;

public class TestEnumVariantFieldNameConflict
{
    [Fact]
    public void MakeInsertExposesRenamedProperty()
    {
        var c = EnumVariantFieldNameConflictMethods.MakeInsert("hello");
        var ins = Assert.IsType<Change.Insert>(c);
        Assert.Equal("hello", ins.InsertValue);
    }

    [Fact]
    public void GetInsertValueOnInsertReturnsValue()
    {
        Assert.Equal("hello", EnumVariantFieldNameConflictMethods.GetInsertValue(
            EnumVariantFieldNameConflictMethods.MakeInsert("hello")));
    }

    [Fact]
    public void GetInsertValueOnDeleteReturnsNull()
    {
        Assert.Null(EnumVariantFieldNameConflictMethods.GetInsertValue(
            EnumVariantFieldNameConflictMethods.MakeDelete("bye")));
    }
}
