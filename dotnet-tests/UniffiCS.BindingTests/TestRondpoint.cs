// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using uniffi.rondpoint;

namespace UniffiCS.BindingTests;

public class TestRondpoint
{
    [Fact]
    public void CopyWorks()
    {
        var dico = new Dictionnaire(Enumeration.Deux, true, (byte)0, 123456789u);
        var copyDico = RondpointMethods.CopieDictionnaire(dico);
        Assert.Equal(dico, copyDico);

        Assert.Equal(Enumeration.Deux, RondpointMethods.CopieEnumeration(Enumeration.Deux));

        var list = new List<Enumeration>() { Enumeration.Un, Enumeration.Deux };
        Assert.Equal(list, RondpointMethods.CopieEnumerations(list.ToArray()));

        var dict = new Dictionary<String, EnumerationAvecDonnees>()
        {
            { "0", new EnumerationAvecDonnees.Zero() },
            { "1", new EnumerationAvecDonnees.Un(1u) },
            { "2", new EnumerationAvecDonnees.Deux(2u, "deux") },
        };
        Assert.Equal(dict, RondpointMethods.CopieCarte(dict));

        Assert.True(RondpointMethods.Switcheroo(false));
    }

    [Fact]
    public void EnumComparisonOperatorWorks()
    {
        Assert.Equal(new EnumerationAvecDonnees.Zero(), new EnumerationAvecDonnees.Zero());
        Assert.Equal(new EnumerationAvecDonnees.Un(1u), new EnumerationAvecDonnees.Un(1u));
        Assert.Equal(new EnumerationAvecDonnees.Deux(2u, "deux"), new EnumerationAvecDonnees.Deux(2u, "deux"));

        Assert.NotEqual(
            (EnumerationAvecDonnees)new EnumerationAvecDonnees.Zero(),
            (EnumerationAvecDonnees)new EnumerationAvecDonnees.Un(1u)
        );
        Assert.NotEqual(new EnumerationAvecDonnees.Un(1u), new EnumerationAvecDonnees.Un(2u));
        Assert.NotEqual(new EnumerationAvecDonnees.Deux(2u, "un"), new EnumerationAvecDonnees.Deux(2u, "deux"));
    }

    [Fact]
    public void TestRoundTrip()
    {
        var rt = new Retourneur();

        var meanValue = 0x1234_5678_9123_4567;

        // booleans
        AffirmAllerRetour(rt.IdentiqueBoolean, true, false);

        // bytes
        AffirmAllerRetour(rt.IdentiqueI8, SByte.MinValue, SByte.MaxValue, (sbyte)meanValue);
        AffirmAllerRetour(rt.IdentiqueU8, Byte.MinValue, Byte.MaxValue, (byte)meanValue);

        // shorts
        AffirmAllerRetour(rt.IdentiqueI16, Int16.MinValue, Int16.MaxValue, (short)meanValue);
        AffirmAllerRetour(rt.IdentiqueU16, UInt16.MinValue, UInt16.MaxValue, (ushort)meanValue);

        // ints
        AffirmAllerRetour(rt.IdentiqueI32, Int32.MinValue, Int32.MaxValue, (int)meanValue);
        AffirmAllerRetour(rt.IdentiqueU32, UInt32.MinValue, UInt32.MaxValue, (uint)meanValue);

        // longs
        AffirmAllerRetour(rt.IdentiqueI64, Int64.MinValue, Int64.MaxValue, (long)meanValue);
        AffirmAllerRetour(rt.IdentiqueU64, UInt64.MinValue, UInt64.MaxValue, (ulong)meanValue);

        // floats
        AffirmAllerRetour(rt.IdentiqueFloat, Single.MinValue, Single.MaxValue, Single.Epsilon);

        // doubles
        AffirmAllerRetour(rt.IdentiqueDouble, Double.MinValue, Double.MaxValue, Double.Epsilon);

        // strings
        AffirmAllerRetour(
            rt.IdentiqueString,
            "",
            "abc",
            "null\u0000byte",
            "été",
            "ښي لاس ته لوستلو لوستل",
            "😻emoji 👨‍👧‍👦multi-emoji, 🇨🇭a flag, a canal, panama"
        );

        // signed record
        var nombresSignes = (int v) => new DictionnaireNombresSignes((sbyte)v, (short)v, (int)v, (long)v);
        AffirmAllerRetour(rt.IdentiqueNombresSignes, nombresSignes(-1), nombresSignes(0), nombresSignes(1));

        // unsigned record
        var nombres = (int v) => new DictionnaireNombres((byte)v, (ushort)v, (uint)v, (ulong)v);
        AffirmAllerRetour(rt.IdentiqueNombres, nombres(0), nombres(1));

        rt.Dispose();
    }

    [Fact]
    public void TestStringifier()
    {
        var st = new Stringifier();

        var meanValue = 0x1234_5678_9123_4567;

        var wellKnown = st.WellKnownString("c#");
        Assert.Equal("uniffi 💚 c#!", st.WellKnownString("c#"));

        // booleans
        AffirmEnchaine(st.ToStringBoolean, true, false);

        // bytes
        AffirmEnchaine(st.ToStringI8, SByte.MinValue, SByte.MaxValue, (sbyte)meanValue);
        AffirmEnchaine(st.ToStringU8, Byte.MinValue, Byte.MaxValue, (byte)meanValue);

        // shorts
        AffirmEnchaine(st.ToStringI16, Int16.MinValue, Int16.MaxValue, (short)meanValue);
        AffirmEnchaine(st.ToStringU16, UInt16.MinValue, UInt16.MaxValue, (ushort)meanValue);

        // ints
        AffirmEnchaine(st.ToStringI32, Int32.MinValue, Int32.MaxValue, (int)meanValue);
        AffirmEnchaine(st.ToStringU32, UInt32.MinValue, UInt32.MaxValue, (uint)meanValue);

        // longs
        AffirmEnchaine(st.ToStringI64, Int64.MinValue, Int64.MaxValue, (long)meanValue);
        AffirmEnchaine(st.ToStringU64, UInt64.MinValue, UInt64.MaxValue, (ulong)meanValue);

        // floats
        AffirmEnchaine(
            (v) => float.Parse(st.ToStringFloat(v)).ToString(),
            Single.MinValue,
            Single.MaxValue,
            Single.Epsilon
        );

        // doubles
        AffirmEnchaine(
            (v) => double.Parse(st.ToStringDouble(v)).ToString(),
            Double.MinValue,
            Double.MaxValue,
            Double.Epsilon
        );

        st.Dispose();
    }

    [Fact]
    void TestDefaultParameterLiterals()
    {
        var op = new Optionneur();

        Assert.Equal("default", op.SinonString());
        Assert.False(op.SinonBoolean());
        Assert.Equal(new List<String>(), op.SinonSequence());
        // TODO(CS)
        // test default map

        // Optionals
        Assert.Null(op.SinonNull());
        Assert.Equal(0, op.SinonZero());

        // Decimals
        Assert.Equal((sbyte)-42, op.SinonI8Dec());
        Assert.Equal((byte)42, op.SinonU8Dec());
        Assert.Equal((short)42, op.SinonI16Dec());
        Assert.Equal((ushort)42, op.SinonU16Dec());
        Assert.Equal((int)42, op.SinonI32Dec());
        Assert.Equal((uint)42, op.SinonU32Dec());
        Assert.Equal((long)42, op.SinonI64Dec());
        Assert.Equal((ulong)42, op.SinonU64Dec());

        // Hex
        Assert.Equal((sbyte)-0x7f, op.SinonI8Hex());
        Assert.Equal((byte)0xff, op.SinonU8Hex());
        Assert.Equal((short)0x7f, op.SinonI16Hex());
        Assert.Equal((ushort)0xffff, op.SinonU16Hex());
        Assert.Equal((int)0x7fffffff, op.SinonI32Hex());
        Assert.Equal((uint)0xffffffff, op.SinonU32Hex());
        Assert.Equal((long)0x7fffffffffffffff, op.SinonI64Hex());
        Assert.Equal((ulong)0xffffffffffffffff, op.SinonU64Hex());

        // Oct
        Assert.Equal(493u, op.SinonU32Oct());

        // Floats
        Assert.Equal(42.0f, op.SinonF32());
        Assert.Equal(42.1, op.SinonF64());

        // Enums
        Assert.Equal(Enumeration.Trois, op.SinonEnum());
    }

    [Fact]
    void ArgumentsOverwriteDefaultParameterLiterals()
    {
        var op = new Optionneur();

        AffirmAllerRetour(op.SinonString, "foo", "bar");
        AffirmAllerRetour(op.SinonBoolean, true, false);
        AffirmAllerRetour<string[]>(op.SinonSequence, [[ "foo", "bar" ]]);

        // Optionals
#pragma warning disable 8621
        AffirmAllerRetour(op.SinonNull, "foo", "bar");
#pragma warning restore 8621
        AffirmAllerRetour(op.SinonZero, (int?)0, (int?)1);

        // Decimals
        AffirmAllerRetour(op.SinonU8Dec, (byte)0, (byte)1);
        AffirmAllerRetour(op.SinonI8Dec, (sbyte)0, (sbyte)1);
        AffirmAllerRetour(op.SinonU16Dec, (ushort)0, (ushort)1);
        AffirmAllerRetour(op.SinonI16Dec, (short)0, (short)1);
        AffirmAllerRetour(op.SinonU32Dec, (uint)0, (uint)1);
        AffirmAllerRetour(op.SinonI32Dec, (int)0, (int)1);
        AffirmAllerRetour(op.SinonU64Dec, (ulong)0, (ulong)1);
        AffirmAllerRetour(op.SinonI64Dec, (long)0, (long)1);

        // Hex
        AffirmAllerRetour(op.SinonU8Hex, (byte)0, (byte)1);
        AffirmAllerRetour(op.SinonI8Hex, (sbyte)0, (sbyte)1);
        AffirmAllerRetour(op.SinonU16Hex, (ushort)0, (ushort)1);
        AffirmAllerRetour(op.SinonI16Hex, (short)0, (short)1);
        AffirmAllerRetour(op.SinonU32Hex, (uint)0, (uint)1);
        AffirmAllerRetour(op.SinonI32Hex, (int)0, (int)1);
        AffirmAllerRetour(op.SinonU64Hex, (ulong)0, (ulong)1);
        AffirmAllerRetour(op.SinonI64Hex, (long)0, (long)1);

        // Oct
        AffirmAllerRetour(op.SinonU32Oct, (uint)0, (uint)1);

        // Floats
        AffirmAllerRetour(op.SinonF32, 0.0f, 1.0f);
        AffirmAllerRetour(op.SinonF64, 0.0, 1.0);

        // Enums
        AffirmAllerRetour(op.SinonEnum, Enumeration.Un, Enumeration.Deux, Enumeration.Trois);

        op.Dispose();
    }

    [Fact]
    void TestDefaultParameterLiteralsInRecord()
    {
        // Testing defaulting properties in record types.
        var defaultes = new OptionneurDictionnaire();
        var explicite = new OptionneurDictionnaire()
        {
            I8Var = (sbyte)-8,
            U8Var = (byte)8,
            I16Var = (short)-16,
            U16Var = (ushort)0x10,
            I32Var = (int)-32,
            U32Var = (uint)32,
            I64Var = (long)-64,
            U64Var = (ulong)64,
            FloatVar = 4.0f,
            DoubleVar = 8.0,
            BooleanVar = true,
            StringVar = "default",
#pragma warning disable 8625
            ListVar = null,
#pragma warning restore 8625
            EnumerationVar = Enumeration.Deux,
            DictionnaireVar = null,
        };
        Assert.Equal(defaultes, explicite);

        using (var rt = new Retourneur())
        {
            // a default list value (null) is transformed into an empty list after a roundtrip
            defaultes = defaultes with
            {
                ListVar = []
            };

            // TODO(CS): C# record comparison doesn't work if the record contains lists/maps.
            // Rewrite records to use custom struct type.
            // AffirmAllerRetour(rt.IdentiqueOptionneurDictionnaire, defaultes);
        }
    }

    static void AffirmAllerRetour<T>(Func<T, T> callback, params T[] list)
    {
        foreach (var value in list)
        {
            Assert.Equal(value, callback(value));
        }
    }

    static void AffirmEnchaine<T>(Func<T, string> callback, params T[] list)
        where T : notnull
    {
        foreach (var value in list)
        {
#pragma warning disable 8602
            Assert.Equal(value.ToString().ToLower(), callback(value).ToLower());
#pragma warning restore 8602
        }
    }
}
