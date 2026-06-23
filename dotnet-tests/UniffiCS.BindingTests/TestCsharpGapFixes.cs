/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using uniffi.csharp_gap_fixes;

namespace UniffiCS.BindingTests;

public class TestCsharpGapFixes
{
    [Fact]
    public void MonitorClassMethodCallable()
    {
        using var m = new Monitor();
        m.MonitorClassMethod();
    }

    [Fact]
    public void MakeNestedBytesShape()
    {
        var arr = CsharpGapFixesMethods.MakeNestedBytes(3, 4);
        Assert.Equal(3, arr.Length);
        Assert.Equal(4, arr[0].Length);
    }

    [Fact]
    public void SumNestedBytesRoundTrip()
    {
        byte[][] m = [[1, 2], [3, 4]];
        Assert.Equal((uint)10, CsharpGapFixesMethods.SumNestedBytes(m));
    }

    // Regression: ErrorTemplate Dispose() for non-flat error enums with unnamed
    // (tuple-style) object-type fields emitted `variant_value.` (CS1001) instead
    // of `variant_value.@v1`. Verified by catching the error and disposing it.
    [Fact]
    public void TupleObjectErrorDisposeDoesNotThrow()
    {
        var ex = Assert.Throws<TupleObjectException.Detailed>(
            () => CsharpGapFixesMethods.ThrowTupleObjectError("oops"));
        using (ex) { }
    }
}
