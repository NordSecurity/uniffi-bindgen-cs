/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using uniffi.external_types_lib;

public class TestExternalTypes {
    [Fact]
    public void ExternalTypesWork() {
        var ct = ExternalTypesLibMethods.GetCombinedType(
            new CombinedType(
                new CrateOneType("test"),
                new CrateTwoType(42)));
        Assert.Equal("test", ct.cot.sval);
        Assert.Equal(42, ct.ctt.ival);

        var ct2 = ExternalTypesLibMethods.GetCombinedType(null);
        Assert.Equal("hello", ct2.cot.sval);
        Assert.Equal(1, ct2.ctt.ival);
    }
}
