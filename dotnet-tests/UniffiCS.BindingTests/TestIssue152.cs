// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using uniffi.issue_152;

namespace UniffiCS.BindingTests;

public class ClassIssue152
{
    [Fact]
    public void RecordPropertyNamesArePascalCase()
    {
        // Create a person using the constructor with PascalCase property names
        var person = new Person(
            FirstName: "John",
            LastName: "Doe",
            Age: 30
        );

        // Access properties using PascalCase (no @ prefix needed)
        Assert.Equal("John", person.FirstName);
        Assert.Equal("Doe", person.LastName);
        Assert.Equal(30u, person.Age);
    }

    [Fact]
    public void RecordRoundtripWorks()
    {
        // Create via helper function and verify round-trip
        var person = Issue152Methods.CreatePerson("Jane", "Smith", 25);

        Assert.Equal("Jane", person.FirstName);
        Assert.Equal("Jane", Issue152Methods.GetFirstName(person));
    }

    [Fact]
    public void KeywordFieldsUseAtPrefix()
    {
        // Fields that become C# keywords when PascalCased should have @ prefix
        var fields = new KeywordFields(
            @Event: "click",
            @Class: "button",
            Name: "submit"
        );

        // Access using @ prefix for keywords
        Assert.Equal("click", fields.@Event);
        Assert.Equal("button", fields.@Class);
        Assert.Equal("submit", fields.Name);
    }

    [Fact]
    public void KeywordFieldsRoundtripWorks()
    {
        var fields = Issue152Methods.CreateKeywordFields("hover", "link", "nav");

        Assert.Equal("hover", fields.@Event);
        Assert.Equal("hover", Issue152Methods.GetEvent(fields));
    }
}
