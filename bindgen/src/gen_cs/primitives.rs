/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use super::CodeType;
use paste::paste;
use uniffi_bindgen::interface::{Literal, Radix, Type};
use uniffi_bindgen::ComponentInterface;

fn render_literal(literal: &Literal) -> String {
    fn typed_number(type_: &Type, num_str: String) -> String {
        let unwrapped_type = match type_ {
            Type::Optional { inner_type } => inner_type,
            t => t,
        };
        match unwrapped_type {
            // Bytes, Shorts and Ints can all be inferred from the type.
            Type::Int8 | Type::UInt8 | Type::Int16 | Type::UInt16 | Type::Int32 => num_str,
            Type::Int64 => format!("{num_str}L"),

            Type::UInt32 => format!("{num_str}u"),
            Type::UInt64 => format!("{num_str}uL"),

            Type::Float32 => format!("{num_str}f"),
            Type::Float64 => num_str,
            _ => panic!("Unexpected literal: {num_str} for type: {type_:?}"),
        }
    }

    match literal {
        Literal::Boolean(v) => format!("{v}"),
        Literal::String(s) => format!("\"{s}\""),
        Literal::Int(i, radix, type_) => typed_number(
            type_,
            match radix {
                Radix::Octal => format!("{i:#x}"),
                Radix::Decimal => format!("{i}"),
                Radix::Hexadecimal => format!("{i:#x}"),
            },
        ),
        Literal::UInt(i, radix, type_) => typed_number(
            type_,
            match radix {
                Radix::Octal => format!("{i:#x}"),
                Radix::Decimal => format!("{i}"),
                Radix::Hexadecimal => format!("{i:#x}"),
            },
        ),
        Literal::Float(string, type_) => typed_number(type_, string.clone()),

        _ => unreachable!("Literal: {:?}", literal),
    }
}

macro_rules! impl_code_type_for_primitive {
    ($T:ty, $type_label:literal, $canonical_name:literal, $default:literal) => {
        paste! {
            #[derive(Debug)]
            pub struct $T;

            impl CodeType for $T  {
                fn type_label(&self, _ci: &ComponentInterface) -> String {
                    $type_label.into()
                }

                fn canonical_name(&self) -> String {
                    $canonical_name.into()
                }

                fn literal(&self, literal: &Literal, _ci: &ComponentInterface) -> String {
                    render_literal(&literal)
                }

                fn default_value(&self, _ci: &ComponentInterface) -> String {
                    $default.into()
                }
            }
        }
    };
}

impl_code_type_for_primitive!(BooleanCodeType, "bool", "Boolean", "false");
impl_code_type_for_primitive!(StringCodeType, "string", "String", "string.Empty");
impl_code_type_for_primitive!(Int8CodeType, "sbyte", "Int8", "0");
impl_code_type_for_primitive!(Int16CodeType, "short", "Int16", "0");
impl_code_type_for_primitive!(Int32CodeType, "int", "Int32", "0");
impl_code_type_for_primitive!(Int64CodeType, "long", "Int64", "0L");
impl_code_type_for_primitive!(UInt8CodeType, "byte", "UInt8", "0");
impl_code_type_for_primitive!(UInt16CodeType, "ushort", "UInt16", "0");
impl_code_type_for_primitive!(UInt32CodeType, "uint", "UInt32", "0");
impl_code_type_for_primitive!(UInt64CodeType, "ulong", "UInt64", "0");
impl_code_type_for_primitive!(Float32CodeType, "float", "Float", "0.0f");
impl_code_type_for_primitive!(Float64CodeType, "double", "Double", "0.0");
impl_code_type_for_primitive!(BytesCodeType, "byte[]", "ByteArray", "Array.Empty<byte>()");
