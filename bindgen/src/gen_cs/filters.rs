use super::*;

fn oracle() -> &'static CsCodeOracle {
    &CsCodeOracle
}

pub(super) fn type_name(
    as_ct: &impl AsCodeType,
    ci: &ComponentInterface,
) -> Result<String, askama::Error> {
    Ok(as_ct.as_codetype().type_label(ci))
}

pub(super) fn type_name_custom(
    typ: &Type,
    ci: &ComponentInterface,
) -> Result<String, askama::Error> {
    // Lowercasing numeric types introduces a problem. In C# custom types are
    // implemented with `using` directive, and the `using` directive expects
    // and identifier on the right side of `=`. Lowercase numeric types are
    // not identifiers, but rather reserved keywords. So its not possible to
    // define a type alias using a lowercase numeric type as the underlying
    // type. To use numeric types as the underlying type, the uppercase
    // numeric system type counterparts must be used.
    match typ {
        Type::Boolean => Ok("Boolean".to_string()),
        Type::Int8 => Ok("SByte".to_string()),
        Type::Int16 => Ok("Int16".to_string()),
        Type::Int32 => Ok("Int32".to_string()),
        Type::Int64 => Ok("Int64".to_string()),
        Type::UInt8 => Ok("Byte".to_string()),
        Type::UInt16 => Ok("UInt16".to_string()),
        Type::UInt32 => Ok("UInt32".to_string()),
        Type::UInt64 => Ok("UInt64".to_string()),
        Type::Float32 => Ok("Single".to_string()),
        Type::Float64 => Ok("Double".to_string()),
        Type::String => Ok("String".to_string()),
        _ => type_name(typ, ci),
    }
}

pub(super) fn canonical_name(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
    Ok(as_ct.as_codetype().canonical_name())
}

pub(super) fn ffi_converter_name(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
    Ok(as_ct.as_codetype().ffi_converter_name())
}

pub(super) fn error_converter_name(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
    Ok(as_ct.as_codetype().error_converter_name())
}

pub(super) fn lower_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
    Ok(format!(
        "{}.INSTANCE.Lower",
        as_ct.as_codetype().ffi_converter_name()
    ))
}

pub(super) fn allocation_size_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
    Ok(format!(
        "{}.INSTANCE.AllocationSize",
        as_ct.as_codetype().ffi_converter_name()
    ))
}

pub(super) fn write_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
    Ok(format!(
        "{}.INSTANCE.Write",
        as_ct.as_codetype().ffi_converter_name()
    ))
}

pub(super) fn lift_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
    Ok(format!(
        "{}.INSTANCE.Lift",
        as_ct.as_codetype().ffi_converter_name()
    ))
}

pub(super) fn read_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
    Ok(format!(
        "{}.INSTANCE.Read",
        as_ct.as_codetype().ffi_converter_name()
    ))
}

pub(super) fn render_literal(
    default: &DefaultValue,
    as_ct: &impl AsCodeType,
    ci: &ComponentInterface,
) -> Result<String, askama::Error> {
    match default {
        DefaultValue::Default => Ok(as_ct.as_codetype().default_value(ci)),
        DefaultValue::Literal(literal) => Ok(as_ct.as_codetype().literal(literal, ci)),
    }
}

pub(super) fn ffi_type(type_: &impl AsType) -> Result<FfiType, askama::Error> {
    Ok(type_.as_type().into())
}

/// Get the C# syntax for representing a given low-level `FFIType`.
pub(super) fn ffi_type_name(type_: &FfiType) -> Result<String, askama::Error> {
    Ok(oracle().ffi_type_label(type_, false))
}

pub(super) fn arg_type_name(type_: &FfiType) -> Result<String, askama::Error> {
    Ok(oracle().ffi_type_label(type_, true))
}

/// Get the idiomatic C# rendering of a class name (for enums, records, errors, etc).
pub(super) fn class_name(nm: &str, ci: &ComponentInterface) -> Result<String, askama::Error> {
    Ok(oracle().class_name(nm, ci))
}

/// Get the idiomatic C# rendering of a function name.
pub(super) fn fn_name(nm: &str) -> Result<String, askama::Error> {
    Ok(oracle().fn_name(nm))
}

/// Get the idiomatic C# rendering of a variable name.
pub(super) fn var_name(nm: impl AsRef<str>) -> Result<String, askama::Error> {
    Ok(oracle().var_name(nm.as_ref()))
}

/// Get the idiomatic C# rendering of an individual enum variant.
pub(super) fn enum_variant(nm: &str) -> Result<String, askama::Error> {
    Ok(oracle().enum_variant_name(nm))
}

/// Get the idiomatic C# rendering of an exception name, replacing
/// `Error` with `Exception`.
pub fn error_variant_name(v: &Variant) -> Result<String, askama::Error> {
    let name = v.name().to_string().to_upper_camel_case();
    Ok(oracle().convert_error_suffix(&name))
}

/// Get the idiomatic C# rendering of an FFI callback function name
pub(super) fn ffi_callback_name(nm: &str) -> Result<String, askama::Error> {
    Ok(oracle().ffi_callback_name(nm))
}

/// Get the idiomatic C# rendering of an FFI callback impl name
pub(super) fn ffi_callback_impl(nm: &str) -> Result<String, askama::Error> {
    Ok(oracle().ffi_callback_impl(nm))
}

/// Get the idiomatic C# rendering of an FFI callback registration function
pub(super) fn ffi_callback_registration(nm: &str) -> Result<String, askama::Error> {
    Ok(format!("{}.Register", oracle().ffi_callback_impl(nm)))
}

pub(super) fn ffi_foreign_future_complete(return_type: &Type) -> Result<String, askama::Error> {
    Ok(format!(
        "UniffiForeignFutureComplete{}",
        FfiType::return_type_name(Some(&FfiType::from(return_type))).to_upper_camel_case()
    ))
}

/// Get the idiomatic C# rendering of an FFI struct name
pub(super) fn ffi_struct_name(nm: &str) -> Result<String, askama::Error> {
    Ok(oracle().ffi_struct_name(nm))
}

/// Get object name tuple (interface, impl)
pub(super) fn object_names(
    obj: &Object,
    ci: &ComponentInterface,
) -> Result<(String, String), askama::Error> {
    Ok(oracle().object_names(obj, ci))
}

/// Get the idiomatic C# rendering of docstring
pub(super) fn docstring(docstring: &str, spaces: &i32) -> Result<String, askama::Error> {
    let middle = textwrap::indent(&textwrap::dedent(docstring), "/// ");
    let wrapped = format!("/// <summary>\n{middle}\n/// </summary>");

    let spaces = usize::try_from(*spaces).unwrap_or_default();
    Ok(textwrap::indent(&wrapped, &" ".repeat(spaces)))
}

/// Orders fields in a way that avoids CS1737 errors
pub(super) fn order_fields(fields: &[Field]) -> Result<(Vec<Field>, bool), askama::Error> {
    let original_fields = fields.to_vec();
    let mut fields = original_fields.clone();
    // fields with default values must come after fields without default values
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1737
    fields.sort_by_key(|field| field.default_value().is_some());
    let is_reordered = fields != original_fields;
    Ok((fields, is_reordered))
}

/// If the name is empty create one based on position of the variable
pub(super) fn or_pos_var(nm: &str, pos: &usize) -> Result<String, askama::Error> {
    if nm.is_empty() {
        Ok(format!("v{pos}"))
    } else {
        Ok(nm.to_string())
    }
}
