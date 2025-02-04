using System;
using System.Collections.Generic;
using uniffi.traits;

namespace UniffiCS.BindingTests;

public class TestTraits
{
    [Fact]
    public void TraitsWoring()
    {
        foreach (var button in TraitsMethods.GetButtons())
        {
            var name = button.Name();
            Assert.Contains(name, new string[] {"go", "stop"});
            Assert.Equal(TraitsMethods.Press(button).Name(), name);
        }
    }

}