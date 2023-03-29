/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Text.RegularExpressions;
using uniffi.uniffi_docstring;

public class TestDocstring {
    class CallbackImpls : CallbackTest {
        public void Test() {}
    }

    [Fact]
    public void DocstringWorks() {
        // Checking to make sure the symbols are reachable,
        // not accidentally commented out by docstrings

        UniffiDocstringMethods.Test();

        _ = EnumTest.ONE;

        _ = new AssociatedEnumTest.Test();

        _ = new ErrorTest.One("hello");

        _ = new AssociatedErrorTest.Test();

        var obj1 = new ObjectTest();
        var obj2 = ObjectTest.NewAlternate();
        obj2.Test();

        var record = new RecordTest(123);
        _ = record.test;

        CallbackTest callback = new CallbackImpls();
        callback.Test();
    }

    [Fact]
    public void DocstringsAppearInBindings() {
        // Hacky way to find project directory based on working directory..
        string rootDirectory = Directory.GetCurrentDirectory() + "../../../../../../";

        string uniffiTestSource = File.ReadAllText(rootDirectory + "3rd-party/uniffi-rs/fixtures/docstring/tests/test_generated_bindings.rs");
        MatchCollection matches = Regex.Matches(uniffiTestSource, @"<docstring-.*>");
        Assert.NotEmpty(matches);

        string bindingsSource = File.ReadAllText(rootDirectory + "dotnet-tests/UniffiCS/gen/uniffi_docstring.cs");

        List<string> matchList = matches
            .Where(match => !bindingsSource.Contains(match.Value))
            .Select(match => match.Value)
            .ToList();

        Assert.Empty(matchList);
    }
}
