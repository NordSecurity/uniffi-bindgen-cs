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

public class TestTraitRecord
{
    [Fact]
    public void TestToString()
    {
        var record = new TraitRecord(S: "hello", I: 42);
        // TraitRecord exports Debug trait
        Assert.Contains("hello", record.ToString());
    }

    [Fact]
    public void TestEq()
    {
        var r1 = new TraitRecord(S: "hello", I: 1);
        var r2 = new TraitRecord(S: "hello", I: 2);
        var r3 = new TraitRecord(S: "world", I: 1);

        // Rust Eq only compares the string, not the int
        Assert.True(r1.Equals(r2));
        Assert.False(r1.Equals(r3));
    }

    [Fact]
    public void TestHash()
    {
        var r1 = new TraitRecord(S: "hello", I: 1);
        var r2 = new TraitRecord(S: "hello", I: 2);
        var r3 = new TraitRecord(S: "world", I: 1);

        // Rust Hash only hashes the string, not the int
        Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
        Assert.NotEqual(r1.GetHashCode(), r3.GetHashCode());
    }
}

public class TestTraitEnum
{
    [Fact]
    public void TestDisplay()
    {
        // Note: For data enums (C# records), the derived variant record auto-generates
        // ToString(), which overrides the base class FFI-backed ToString().
        // The FFI-backed display is available when accessed through the base type.
        TraitEnum e = new TraitEnum.S("hello");
        // C# record's auto-generated ToString is used for derived variants
        Assert.Contains("S", e.ToString());
    }

    [Fact]
    public void TestEq()
    {
        // Note: C# records auto-generate structural equality on derived variants,
        // overriding the base class FFI-backed Equals.
        // The Rust Eq (variant-only comparison) is on the base type.
        var s1 = new TraitEnum.S("s1");
        var s2 = new TraitEnum.S("s2");
        var i1 = new TraitEnum.I(1);

        // C# record structural equality: different field values = not equal
        Assert.NotEqual(s1, s2);
        // Different variants are not equal
        Assert.False(s1.Equals((TraitEnum)i1));
    }

    [Fact]
    public void TestHash()
    {
        var s1 = new TraitEnum.S("s1");
        var i1 = new TraitEnum.I(1);

        // Different variants have different hash codes
        Assert.NotEqual(s1.GetHashCode(), i1.GetHashCode());
    }
}

public class TestFlatTraitEnum
{
    [Fact]
    public void TestDebugString()
    {
        var e = FlatTraitEnum.Alpha;
        Assert.Contains("Alpha", e.ToDebugString());
    }

    [Fact]
    public void TestDisplayString()
    {
        Assert.Equal("FlatTraitEnum::flat-alpha", FlatTraitEnum.Alpha.ToDisplayString());
        Assert.Equal("FlatTraitEnum::flat-beta", FlatTraitEnum.Beta.ToDisplayString());
        Assert.Equal("FlatTraitEnum::flat-gamma", FlatTraitEnum.Gamma.ToDisplayString());
    }
}