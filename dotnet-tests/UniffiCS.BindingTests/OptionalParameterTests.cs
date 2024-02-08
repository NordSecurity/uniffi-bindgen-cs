/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using uniffi.uniffi_cs_optional_parameters;
using static uniffi.uniffi_cs_optional_parameters.UniffiCsOptionalParametersMethods;

namespace UniffiCS.binding_tests;

public class OptionalParameterTests
{
    [Fact]
    public void OptionalParameter_CanBeOmitted()
    {
        var person = new Person(isSpecial: false);
        string message = Hello(person);
        Assert.Equal("Hello stranger!", message);
    }

    [Fact]
    public void OptionalParameter_CanBeSpecified()
    {
        var person = new Person(name: "John Connor", isSpecial: false);
        string message = Hello(person);
        Assert.Equal("Hello John Connor!", message);
    }
}
