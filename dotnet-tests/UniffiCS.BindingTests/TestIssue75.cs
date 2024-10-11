// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using uniffi.issue_75;

namespace UniffiCS.BindingTests;

public class ClassIssue75
{
    [Fact]
    public void TestIssue75()
    {
        new EmptyDictionary();
    }
}
