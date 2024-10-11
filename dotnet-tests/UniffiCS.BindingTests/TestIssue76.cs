// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using uniffi.issue_76;

namespace UniffiCS.BindingTests;

public class ClassIssue76
{
    [Fact]
    public void TestIssue76()
    {
        Assert.Throws<Exception.Example>(() => Issue76Methods.AlwaysError());
    }
}
