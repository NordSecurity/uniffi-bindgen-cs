/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using uniffi.uniffi_cs_optional_parameters;
using static uniffi.uniffi_cs_optional_parameters.UniffiCsOptionalParametersMethods;

namespace UniffiCS.BindingTests;

public class OptionalParameterTests
{
    [Fact]
    public void OptionalParameter_CanBeOmitted()
    {
        var person = new Person(IsSpecial: false);
        string message = Hello(person);
        Assert.Equal("Hello stranger!", message);
    }

    [Fact]
    public void OptionalParameter_CanBeSpecified()
    {
        var person = new Person(Name: "John Connor", IsSpecial: false);
        string message = Hello(person);
        Assert.Equal("Hello John Connor!", message);
    }

    [Fact]
    public void EnumVariantDefault_CanBeOmitted()
    {
        Assert.Equal("Hello stranger!", Greet(new Greeting.Hello(IsSpecial: false)));
        Assert.Equal("Waving 1 time(s)", Greet(new Greeting.Wave()));
        Assert.Equal("Nodding at John 2 time(s)", Greet(new Greeting.Nod("John")));
    }

    [Fact]
    public void EnumVariantDefault_CanBeSpecified()
    {
        Assert.Equal("Hello John Connor! You are special!", Greet(new Greeting.Hello(Name: "John Connor", IsSpecial: true)));
        Assert.Equal("Waving 3 time(s)", Greet(new Greeting.Wave(3)));
        Assert.Equal("Nodding at John 4 time(s)", Greet(new Greeting.Nod("John", 4)));
    }

    [Fact]
    public void EnumVariantWithReorderedFields_RoundTrips()
    {
        var greeting = new Greeting.Hello(Name: "John Connor", IsSpecial: true);
        Assert.Equal(greeting, EchoGreeting(greeting));
    }
}
