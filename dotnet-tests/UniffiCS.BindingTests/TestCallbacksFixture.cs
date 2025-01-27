// // This Source Code Form is subject to the terms of the Mozilla Public
// // License, v. 2.0. If a copy of the MPL was not distributed with this
// // file, You can obtain one at http://mozilla.org/MPL/2.0/.

// using System;
// using System.Collections.Generic;
// using System.Linq;
// using uniffi.fixture_callbacks;

// namespace UniffiCS.BindingTests;

// class CsharpGetters : ForeignGetters
// {
//     public Boolean GetBool(Boolean v, Boolean argumentTwo)
//     {
//         return v ^ argumentTwo;
//     }

//     public String GetString(String v, Boolean arg2)
//     {
//         if (v == "bad-argument")
//         {
//             throw new SimpleException.BadArgument("bad argument");
//         }
//         if (v == "unexpected-error")
//         {
//             throw new Exception("something failed");
//         }
//         return arg2 ? "1234567890123" : v;
//     }

//     public String? GetOption(String? v, Boolean arg2)
//     {
//         if (v == "bad-argument")
//         {
//             throw new ComplexException.ReallyBadArgument(20);
//         }
//         if (v == "unexpected-error")
//         {
//             throw new Exception("something failed");
//         }
//         return arg2 && v != null ? v.ToUpper() : v;
//     }

//     public List<Int32> GetList(List<Int32> v, Boolean arg2)
//     {
//         return arg2 ? v : new List<Int32>();
//     }

//     public void GetNothing(String v)
//     {
//         if (v == "bad-argument")
//         {
//             throw new SimpleException.BadArgument("bad argument");
//         }
//         if (v == "unexpected-error")
//         {
//             throw new Exception("something failed");
//         }
//     }
// }

// class CsharpStringifier : StoredForeignStringifier
// {
//     public String FromSimpleType(Int32 value)
//     {
//         return "C#: " + value.ToString();
//     }

//     public String FromComplexType(List<Double?>? values)
//     {
//         if (values == null)
//         {
//             return "C#: null";
//         }
//         else
//         {
//             var stringValues = values.Select(number =>
//             {
//                 return number == null ? "null" : number.ToString();
//             });
//             return "C#: " + string.Join(" ", stringValues);
//         }
//     }
// }

// public class TestCallbacksFixture
// {
//     [Fact]
//     public void CallbackRoundTripValues()
//     {
//         var callback = new CsharpGetters();
//         using (var rustGetters = new RustGetters())
//         {
//             foreach (var v in new List<Boolean> { true, false })
//             {
//                 var flag = true;
//                 Assert.Equal(callback.GetBool(v, flag), rustGetters.GetBool(callback, v, flag));
//             }

//             foreach (
//                 var v in new List<List<Int32>>
//                 {
//                     new List<Int32> { 1, 2 },
//                     new List<Int32> { 0, 1 }
//                 }
//             )
//             {
//                 var flag = true;
//                 Assert.Equal(callback.GetList(v, flag), rustGetters.GetList(callback, v, flag));
//             }

//             foreach (var v in new List<String> { "Hello", "world" })
//             {
//                 var flag = true;
//                 Assert.Equal(callback.GetString(v, flag), rustGetters.GetString(callback, v, flag));
//             }

//             foreach (var v in new List<String?> { "Some", null })
//             {
//                 var flag = true;
//                 Assert.Equal(callback.GetOption(v, flag), rustGetters.GetOption(callback, v, flag));
//             }

//             Assert.Equal("TestString", rustGetters.GetStringOptionalCallback(callback, "TestString", false));
//             Assert.Null(rustGetters.GetStringOptionalCallback(null, "TestString", false));
//         }
//     }

//     [Fact]
//     public void CallbackRoundTripErrors()
//     {
//         var callback = new CsharpGetters();
//         using (var rustGetters = new RustGetters())
//         {
//             Assert.Throws<SimpleException.BadArgument>(() => rustGetters.GetString(callback, "bad-argument", true));
//             Assert.Throws<SimpleException.UnexpectedException>(
//                 () => rustGetters.GetString(callback, "unexpected-error", true)
//             );

//             var reallyBadArgument = Assert.Throws<ComplexException.ReallyBadArgument>(
//                 () => rustGetters.GetOption(callback, "bad-argument", true)
//             );
//             Assert.Equal(20, reallyBadArgument.code);

//             var unexpectedException = Assert.Throws<ComplexException.UnexpectedErrorWithReason>(
//                 () => rustGetters.GetOption(callback, "unexpected-error", true)
//             );
//             Assert.Equal(new Exception("something failed").Message, unexpectedException.reason);
//         }
//     }

//     [Fact]
//     public void CallbackMayBeStoredInObject()
//     {
//         var stringifier = new CsharpStringifier();
//         using (var rustStringifier = new RustStringifier(stringifier))
//         {
//             foreach (var v in new List<Int32> { 1, 2 })
//             {
//                 Assert.Equal(stringifier.FromSimpleType(v), rustStringifier.FromSimpleType(v));
//             }

//             foreach (
//                 var v in new List<List<Double?>?>
//                 {
//                     null,
//                     new List<Double?> { null, 3.14 }
//                 }
//             )
//             {
//                 Assert.Equal(stringifier.FromComplexType(v), rustStringifier.FromComplexType(v));
//             }
//         }
//     }

//     [Fact]
//     public void VoidCallbackExceptions()
//     {
//         var callback = new CsharpGetters();
//         using (var rustGetters = new RustGetters())
//         {
//             // no exception
//             rustGetters.GetNothing(callback, "foo");
//             Assert.Throws<SimpleException.BadArgument>(() => rustGetters.GetNothing(callback, "bad-argument"));
//             Assert.Throws<SimpleException.UnexpectedException>(
//                 () => rustGetters.GetNothing(callback, "unexpected-error")
//             );
//         }
//     }
//     
//      [Fact]
        // public void ShortLivedCallbackDoesNotInvalidateLongerLivedCallback()
        // {
        //     var stringifier = new CsharpStringifier();
        //     using (var rustStringifier1 = new RustStringifier(stringifier))
        //     {
        //         using (var rustStringifier2 = new RustStringifier(stringifier))
        //         {
        //             Assert.Equal("C#: 123", rustStringifier2.FromSimpleType(123));
        //         }
        //         // `stringifier` must remain valid after `rustStringifier2` drops the reference

        //         Assert.Equal("C#: 123", rustStringifier1.FromSimpleType(123));
        //     }
        // }
//}
// }
