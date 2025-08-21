using System;
using System.Collections.Generic;
using uniffi.traits;

namespace UniffiCS.BindingTests;

class OurButton : Button {
    public String Name() {
        return "c#";
    }
}

public class TestTraits
{
    [Fact]
    public void TraitsWorking()
    {
        foreach (var button in TraitsMethods.GetButtons())
        {
            var name = button.Name();
            Assert.Contains(name, ["go", "stop"]);
            Assert.Equal(TraitsMethods.Press(button).Name(), name);
        }
    }

    [Fact]
    public void TraitsWorkingWithForeign()
    {
        var button = new OurButton();
        Assert.Equal("c#", button.Name());
        Assert.Equal("c#", TraitsMethods.Press(button).Name());
    }

}
