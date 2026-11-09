// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using uniffi.uniffi_docstring;

namespace UniffiCS.BindingTests;

public class TestDocstring
{
    class CallbackImpls : CallbackTest
    {
        public void Test() { }
    }

    [Fact]
    public void DocstringWorks()
    {
        // Checking to make sure the symbols are reachable,
        // not accidentally commented out by docstrings

        UniffiDocstringMethods.Test();

        _ = EnumTest.One;
        _ = EnumTest.Two;

        _ = new AssociatedEnumTest.Test(0);
        _ = new AssociatedEnumTest.Test2(0);

        _ = new ErrorTest.One("hello");
        _ = new ErrorTest.Two("hello");

        _ = new AssociatedErrorTest.Test(0);
        _ = new AssociatedErrorTest.Test2(0);

        var obj1 = new ObjectTest();
        var obj2 = ObjectTest.NewAlternate();
        obj2.Test();

        var record = new RecordTest(123);
        _ = record.Test;

        CallbackTest callback = new CallbackImpls();
        callback.Test();
    }

    [Fact]
    public void DocstringsAppearInBindings()
    {
        // Hacky way to find project directory based on working directory..
        string rootDirectory = Directory.GetCurrentDirectory() + "../../../../../../";
        string bindingsSource = File.ReadAllText(rootDirectory + "dotnet-tests/UniffiCS/gen/uniffi_docstring.cs");

        List<string> expected = new List<string> {
            "// &lt;docstring-alternate-constructor&gt;",
            "// &lt;docstring-associated-enum-variant-2&gt;",
            "// &lt;docstring-associated-enum-variant&gt;",
            "// &lt;docstring-associated-enum&gt;",
            "// &lt;docstring-associated-error-variant-2&gt;",
            "// &lt;docstring-associated-error-variant&gt;",
            "// &lt;docstring-associated-error&gt;",
            "// &lt;docstring-callback-method&gt;",
            "// &lt;docstring-callback&gt;",
            "// &lt;docstring-enum-variant-2&gt;",
            "// &lt;docstring-enum-variant&gt;",
            "// &lt;docstring-enum&gt;",
            "// &lt;docstring-error-variant-2&gt;",
            "// &lt;docstring-error-variant&gt;",
            "// &lt;docstring-error&gt;",
            "// &lt;docstring-function&gt;",
            "// &lt;docstring-method&gt;",
            // Namespace docs are a plain comment, not XML.
            "// <docstring-namespace>",
            "// &lt;docstring-object&gt;",
            "// &lt;docstring-primary-constructor&gt;",
            "// &lt;docstring-record-field&gt;",
            "// &lt;docstring-record&gt;",
            "// &lt;docstring-multiline-function&gt;",
            "// &lt;second-line&gt;"
        };

        List<string> missingDocstrings = expected.Where(e => !bindingsSource.Contains(e)).ToList();
        Assert.Empty(missingDocstrings);

        // Docstrings are XML-escaped, so markers are found by their escaped brackets.
        string pattern = @"// (&lt;.*?&gt;)";
        List<string> unexpectedDocstrings = Regex.Matches(bindingsSource, pattern)
            .Select(match => match.Value)
            .Where(match => !expected.Contains(match))
            .ToList();
        Assert.Empty(unexpectedDocstrings);
    }
}
