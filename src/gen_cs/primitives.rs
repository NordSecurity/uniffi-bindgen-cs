/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use paste::paste;
use uniffi_bindgen::backend::{CodeOracle, CodeType, Literal};
use uniffi_bindgen::interface::{types::Type, Radix};

fn render_literal(_oracle: &dyn CodeOracle, literal: &Literal) -> String {
    fn typed_number(type_: &Type, num_str: String) -> String {
        match type_ {
            // The following types are implicitly converted from literal
            // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
            Type::Int8
            | Type::UInt8
            | Type::Int16
            | Type::UInt16
            | Type::Int32
            | Type::UInt32
            | Type::UInt64
            | Type::Float64 => num_str,

            Type::Int64 => format!("{}L", num_str),
            Type::Float32 => format!("{}f", num_str),
            _ => panic!("Unexpected literal: {} is not a number", num_str),
        }
    }

    match literal {
        Literal::Boolean(v) => format!("{}", v),
        Literal::String(s) => format!("\"{}\"", s),
        Literal::Int(i, radix, type_) => typed_number(
            type_,
            match radix {
                Radix::Octal => format!("{:#x}", i),
                Radix::Decimal => format!("{}", i),
                Radix::Hexadecimal => format!("{:#x}", i),
            },
        ),
        Literal::UInt(i, radix, type_) => typed_number(
            type_,
            match radix {
                Radix::Octal => format!("{:#x}", i),
                Radix::Decimal => format!("{}", i),
                Radix::Hexadecimal => format!("{:#x}", i),
            },
        ),
        Literal::Float(string, type_) => typed_number(type_, string.clone()),

        _ => unreachable!("Literal"),
    }
}

macro_rules! impl_code_type_for_primitive {
    ($T:ty, $type_label:literal, $canonical_name:literal) => {
        paste! {
            pub struct $T;

            impl CodeType for $T  {
                fn type_label(&self, _oracle: &dyn CodeOracle) -> String {
                    $type_label.into()
                }

                fn canonical_name(&self, _oracle: &dyn CodeOracle) -> String {
                    $canonical_name.into()
                }

                fn literal(&self, oracle: &dyn CodeOracle, literal: &Literal) -> String {
                    render_literal(oracle, &literal)
                }
            }
        }
    };
}

impl_code_type_for_primitive!(BooleanCodeType, "Boolean", "Boolean");
impl_code_type_for_primitive!(StringCodeType, "String", "String");
impl_code_type_for_primitive!(Int8CodeType, "SByte", "SByte");
impl_code_type_for_primitive!(Int16CodeType, "Int16", "Short");
impl_code_type_for_primitive!(Int32CodeType, "Int32", "Int");
impl_code_type_for_primitive!(Int64CodeType, "Int64", "Long");
impl_code_type_for_primitive!(UInt8CodeType, "Byte", "Byte");
impl_code_type_for_primitive!(UInt16CodeType, "UInt16", "UShort");
impl_code_type_for_primitive!(UInt32CodeType, "UInt32", "UInt");
impl_code_type_for_primitive!(UInt64CodeType, "UInt64", "ULong");
impl_code_type_for_primitive!(Float32CodeType, "Single", "Float");
impl_code_type_for_primitive!(Float64CodeType, "Double", "Double");
