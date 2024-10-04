// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using uniffi.issue_60;

namespace UniffiCS.BindingTests;

public class ClassIssue60
{
    [Fact]
    public void TestIssue60()
    {
        new Shape.Rectangle(new Rectangle(1f, 2f));
        new ShapeException.Rectangle(new Rectangle(1f, 2f));
    }
}
