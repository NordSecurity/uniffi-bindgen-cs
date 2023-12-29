// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using uniffi.trait_methods;

namespace UniffiCS.BindingTests;

public class TestTraitMethods
{
    [Fact]
    public void TraitMethodsWork()
    {
        using (var methods = new TraitMethods("yo"))
        {
            Assert.Equal("TraitMethods(yo)", methods.ToString());
        }
    }

    [Fact]
    public void TraitMethodsProcMacro()
    {
        using (var methods = new ProcTraitMethods("yo"))
        {
            Assert.Equal("ProcTraitMethods(yo)", methods.ToString());
        }
    }
}
