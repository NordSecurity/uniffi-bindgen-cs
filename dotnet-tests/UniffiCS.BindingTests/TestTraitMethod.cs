// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using uniffi.trait_methods;

namespace UniffiCS.BindingTests;

public class TestTraitMethods
{
    [Fact]
    public void TestDisplay()
    {
        using (var methods = new TraitMethods("yo"))
        {
            Assert.Equal("TraitMethods(yo)", methods.ToString());
        }
    }

    [Fact]
    public void TestEq()
    {
        using (var methods = new TraitMethods("yo"))
        {
            // Values are equal if input is the same
            Assert.Equal(methods, new TraitMethods("yo"));
            Assert.NotEqual(methods, new TraitMethods("yoyo"));

            // Values are not referentially equal
            Assert.False(methods == new TraitMethods("yo"));
        }
    }

    [Fact]
    public void TestEqNull()
    {
        TraitMethods? t1 = null;
        TraitMethods? t2 = null;
        Assert.True(Object.Equals(t1, t2));

        t1 = new TraitMethods("yo");
        Assert.False(Object.Equals(t1, t2));
        
        Assert.False(new TraitMethods("yo") == null);
        Assert.True(new TraitMethods("yo") != null);
    }

    [Fact]
    public void TestEqContains() 
    {
        var tm = new TraitMethods("yo");
        var list = new List<TraitMethods>
        {
            tm
        };

        Assert.Contains(tm, list);
        Assert.Contains(new TraitMethods("yo"), list);
        Assert.DoesNotContain(null, list);
        Assert.DoesNotContain(new TraitMethods("yoyo"), list);
    }

   [Fact]
   public void TestHash()
   {
       using (var methods = new TraitMethods("yo"))
       {
           Assert.Equal(methods.GetHashCode(), new TraitMethods("yo").GetHashCode());
           Assert.NotEqual(methods.GetHashCode(), new TraitMethods("yoyo").GetHashCode());
       }
   }
}

public class TestProcMacroTraitMethods
{
    [Fact]
    public void TestDisplay()
    {
        using (var methods = new ProcTraitMethods("yo"))
        {
            Assert.Equal("ProcTraitMethods(yo)", methods.ToString());
        }
    }

   [Fact]
    public void TestEq()
    {
        using (var methods = new ProcTraitMethods("yo"))
        {
            // Values are equal if input is the same
            Assert.Equal(methods, new ProcTraitMethods("yo"));
            Assert.NotEqual(methods, new ProcTraitMethods("yoyo"));

            // Values are not referentially equal
            Assert.False(methods == new ProcTraitMethods("yo"));
        }
    }

    [Fact]
    public void TestEqNull()
    {
        ProcTraitMethods? t1 = null;
        ProcTraitMethods? t2 = null;
        Assert.True(Object.Equals(t1, t2));
        
        Assert.False(new ProcTraitMethods("yo") == null);
    }

    [Fact]
    public void TestEqContains() 
    {
        var tm = new ProcTraitMethods("yo");
        var list = new List<ProcTraitMethods>
        {
            tm
        };

        Assert.Contains(tm, list);
        Assert.Contains(new ProcTraitMethods("yo"), list);
        Assert.DoesNotContain(null, list);
        Assert.DoesNotContain(new ProcTraitMethods("yoyo"), list);
    }

    [Fact]
    public void TestHash()
    {
        using (var methods = new ProcTraitMethods("yo"))
        {
            Assert.Equal(methods.GetHashCode(), new ProcTraitMethods("yo").GetHashCode());
            Assert.NotEqual(methods.GetHashCode(), new ProcTraitMethods("yoyo").GetHashCode());
        }
    }
}