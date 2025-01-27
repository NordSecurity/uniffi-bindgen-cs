/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use std::borrow::Borrow;
use std::cell::RefCell;
use std::collections::{BTreeSet, HashMap, HashSet};
use std::fmt::Debug;

use anyhow::{Context, Result};
use askama::Template;
use heck::{ToLowerCamelCase, ToUpperCamelCase};
use serde::{Deserialize, Serialize};

use uniffi_bindgen::backend::{TemplateExpression, Type};
use uniffi_bindgen::interface::*;
use uniffi_bindgen::ComponentInterface;

mod callback_interface;
mod compounds;
mod custom;
mod enum_;
mod external;
pub mod formatting;
mod miscellany;
mod object;
mod primitives;
mod record;

trait CodeType: Debug {
    /// The language specific label used to reference this type. This will be used in
    /// method signatures and property declarations.
    fn type_label(&self, ci: &ComponentInterface) -> String;

    /// A representation of this type label that can be used as part of another
    /// identifier. e.g. `read_foo()`, or `FooInternals`.
    ///
    /// This is especially useful when creating specialized objects or methods to deal
    /// with this type only.
    fn canonical_name(&self) -> String;

    fn literal(&self, _literal: &Literal, ci: &ComponentInterface) -> String {
        unimplemented!("Unimplemented for {}", self.type_label(ci))
    }

    /// Name of the FfiConverter
    ///
    /// This is the object that contains the lower, write, lift, and read methods for this type.
    /// Depending on the binding this will either be a singleton or a class with static methods.
    ///
    /// This is the newer way of handling these methods and replaces the lower, write, lift, and
    /// read CodeType methods.  Currently only used by Kotlin, but the plan is to move other
    /// backends to using this.
    fn ffi_converter_name(&self) -> String {
        format!("FfiConverter{}", self.canonical_name())
    }

    /// A list of imports that are needed if this type is in use.
    /// Classes are imported exactly once.
    fn imports(&self) -> Option<Vec<String>> {
        None
    }

    /// Function to run at startup
    fn initialization_fn(&self) -> Option<String> {
        None
    }
}

// config options to customize the generated C# bindings.
#[derive(Debug, Default, Clone, Serialize, Deserialize)]
pub struct Config {
    pub(super) namespace: Option<String>,
    pub(super) cdylib_name: Option<String>,
    #[serde(default)]
    custom_types: HashMap<String, CustomTypeConfig>,
    #[serde(default)]
    external_packages: HashMap<String, String>,
    global_methods_class_name: Option<String>,
    access_modifier: Option<String>,
    null_string_to_empty: Option<bool>,
}

#[derive(Debug, Default, Clone, Serialize, Deserialize)]
pub struct CustomTypeConfig {
    imports: Option<Vec<String>>,
    type_name: Option<String>,
    into_custom: TemplateExpression,
    from_custom: TemplateExpression,
}

impl Config {
    pub fn namespace(&self) -> String {
        self.namespace
            .as_ref()
            .expect("`namespace` must be set in `update_from_ci`")
            .clone()
    }

    pub fn cdylib_name(&self) -> String {
        self.cdylib_name
            .as_ref()
            .expect("`cdylib_name` not specified")
            .clone()
    }

    pub fn access_modifier(&self) -> String {
        match self.access_modifier.as_ref() {
            Some(value) => value.clone(),
            None => "internal".to_string(),
        }
    }
}

// Generate C# bindings for the given ComponentInterface, as a string.
pub fn generate_bindings(config: &Config, ci: &ComponentInterface) -> Result<String> {
    CsWrapper::new(config.clone(), ci)
        .render()
        .context("failed to render C# bindings")
}

/// Renders C# helper code for all types
///
/// This template is a bit different than others in that it stores internal state from the render
/// process.  Make sure to only call `render()` once.
#[derive(Template)]
#[template(syntax = "cs", escape = "none", path = "Types.cs")]
pub struct TypeRenderer<'a> {
    config: &'a Config,
    ci: &'a ComponentInterface,
    // Track included modules for the `include_once()` macro
    include_once_names: RefCell<HashSet<String>>,
    // Track imports added with the `add_import()` macro
    imports: RefCell<BTreeSet<String>>,
    // Track type aliases added with the `add_type_alias()` macro
    type_aliases: RefCell<BTreeSet<TypeAlias>>,
}

#[derive(PartialEq, Eq, PartialOrd, Ord, Clone)]
pub struct TypeAlias {
    alias: String,
    original_type: String,
}

impl<'a> TypeRenderer<'a> {
    fn new(config: &'a Config, ci: &'a ComponentInterface) -> Self {
        Self {
            config,
            ci,
            include_once_names: RefCell::new(HashSet::new()),
            imports: RefCell::new(BTreeSet::new()),
            type_aliases: RefCell::new(BTreeSet::new()),
        }
    }

    // Get the package name for an external type
    fn external_type_package_name(&self, module_path: &str, namespace: &str) -> String {
        // config overrides are keyed by the crate name, default fallback is the namespace.
        let crate_name = module_path.split("::").next().unwrap();
        match self.config.external_packages.get(crate_name) {
            Some(name) => name.clone(),
            // unreachable in library mode - all deps are in our config with correct namespace.
            None => format!("uniffi.{namespace}"),
        }
    }

    // The following methods are used by the `Types.kt` macros.

    // Helper for the including a template, but only once.
    //
    // The first time this is called with a name it will return true, indicating that we should
    // include the template.  Subsequent calls will return false.
    fn include_once_check(&self, name: &str) -> bool {
        self.include_once_names
            .borrow_mut()
            .insert(name.to_string())
    }

    // Helper to add an import statement
    //
    // Call this inside your template to cause an import statement to be added at the top of the
    // file.  Imports will be sorted and de-deuped.
    //
    // Returns an empty string so that it can be used inside an askama `{{ }}` block.
    fn add_import(&self, name: &str) -> &str {
        self.imports.borrow_mut().insert(name.to_owned());
        ""
    }

    // Add a `using alias = original_type;` statement to the top of the namespace.
    // C# doesn't allow adding type aliases in the middle of the namespace, so this function
    // allows to add type aliases to the top of the namespace.
    //
    // Returns an empty string so that it can be used inside an askama `{{ }}` block.
    fn add_type_alias(&self, alias: &str, original_type: &str) -> &str {
        self.type_aliases.borrow_mut().insert(TypeAlias {
            alias: alias.to_owned(),
            original_type: original_type.to_owned(),
        });
        ""
    }
}

#[derive(Template)]
#[template(syntax = "cs", escape = "none", path = "wrapper.cs")]
pub struct CsWrapper<'a> {
    config: Config,
    ci: &'a ComponentInterface,
    type_helper_code: String,
    type_imports: RefCell<BTreeSet<String>>,
    type_aliases: BTreeSet<TypeAlias>,
}

impl<'a> CsWrapper<'a> {
    pub fn new(config: Config, ci: &'a ComponentInterface) -> Self {
        let type_renderer = TypeRenderer::new(&config, ci);
        let type_helper_code = type_renderer.render().unwrap();
        let type_imports = type_renderer.imports.clone();
        let type_aliases = type_renderer.type_aliases.into_inner();
        Self {
            config,
            ci,
            type_helper_code,
            type_imports,
            type_aliases,
        }
    }

    pub fn initialization_fns(&self) -> Vec<String> {
        self.ci
            .iter_types()
            .map(|t| CsCodeOracle.find(t))
            .filter_map(|ct| ct.initialization_fn())
            .collect()
    }

    // Helper to add an import statement
    //
    // Call this inside your template to cause an import statement to be added at the top of the
    // file.  Imports will be sorted and de-deuped.
    //
    // Returns an empty string so that it can be used inside an askama `{{ }}` block.
    fn add_import(&self, name: &str) -> &str {
        self.type_imports.borrow_mut().insert(name.to_owned());
        ""
    }

    pub fn imports(&self) -> Vec<String> {
        self.type_imports.borrow().iter().cloned().collect()
    }

    pub fn type_aliases(&self) -> Vec<TypeAlias> {
        self.type_aliases.iter().cloned().collect()
    }
}

pub(self) trait AsCodeType {
    fn as_codetype(&self) -> Box<dyn CodeType>;
}

impl<T: AsType> AsCodeType for T {
    // Map `Type` instances to a `Box<dyn CodeType>` for that type.
    //
    // There is a companion match in `templates/Types.cs` which performs a similar function for the
    // template code.
    //
    //   - When adding additional types here, make sure to also add a match arm to the `Types.cs` template.
    //   - To keep things managable, let's try to limit ourselves to these 2 mega-matches
    fn as_codetype(&self) -> Box<dyn CodeType> {
        match self.as_type() {
            Type::UInt8 => Box::new(primitives::UInt8CodeType),
            Type::Int8 => Box::new(primitives::Int8CodeType),
            Type::UInt16 => Box::new(primitives::UInt16CodeType),
            Type::Int16 => Box::new(primitives::Int16CodeType),
            Type::UInt32 => Box::new(primitives::UInt32CodeType),
            Type::Int32 => Box::new(primitives::Int32CodeType),
            Type::UInt64 => Box::new(primitives::UInt64CodeType),
            Type::Int64 => Box::new(primitives::Int64CodeType),
            Type::Float32 => Box::new(primitives::Float32CodeType),
            Type::Float64 => Box::new(primitives::Float64CodeType),
            Type::Boolean => Box::new(primitives::BooleanCodeType),
            Type::String => Box::new(primitives::StringCodeType),
            Type::Bytes => Box::new(primitives::BytesCodeType),

            Type::Timestamp => Box::new(miscellany::TimestampCodeType),
            Type::Duration => Box::new(miscellany::DurationCodeType),

            Type::Enum { name, .. } => Box::new(enum_::EnumCodeType::new(name)),
            Type::Object { name, .. } => Box::new(object::ObjectCodeType::new(name)),
            Type::Record { name, .. } => Box::new(record::RecordCodeType::new(name)),
            Type::CallbackInterface { name, .. } => {
                Box::new(callback_interface::CallbackInterfaceCodeType::new(name))
            }
            Type::Optional { inner_type } => {
                Box::new(compounds::OptionalCodeType::new(*inner_type))
            }
            Type::Sequence { inner_type } => {
                Box::new(compounds::SequenceCodeType::new(*inner_type))
            }
            Type::Map {
                key_type,
                value_type,
            } => Box::new(compounds::MapCodeType::new(*key_type, *value_type)),
            Type::External { name, .. } => Box::new(external::ExternalCodeType::new(name)),
            Type::Custom { name, .. } => Box::new(custom::CustomCodeType::new(name)),
        }
    }
}

#[derive(Clone)]
pub struct CsCodeOracle;

impl CsCodeOracle {
    fn find(&self, type_: &Type) -> Box<dyn CodeType> {
        type_.clone().as_type().as_codetype()
    }

    /// Get the idiomatic C# rendering of a class name (for enums, records, errors, etc).
    fn class_name(&self, nm: &str, ci: &ComponentInterface) -> String {
        let name = nm.to_string().to_upper_camel_case();
        // fixup errors.
        ci.is_name_used_as_error(nm)
            .then(|| self.convert_error_suffix(&name))
            .unwrap_or(name)
    }

    fn convert_error_suffix(&self, nm: &str) -> String {
        match nm.strip_suffix("Error") {
            None => nm.to_string(),
            Some(stripped) => format!("{stripped}Exception"),
        }
    }

    /// Get the idiomatic C# rendering of a function name.
    fn fn_name(&self, nm: &str) -> String {
        nm.to_string().to_upper_camel_case()
    }

    /// Get the idiomatic C# rendering of a variable name.
    fn var_name(&self, nm: &str) -> String {
        format!("@{}", nm.to_string().to_lower_camel_case())
    }

    /// Get the idiomatic C# rendering of an individual enum variant.
    fn enum_variant_name(&self, nm: &str) -> String {
        nm.to_string().to_upper_camel_case()
    }

    /// Get the idiomatic C# rendering of an FFI callback function name
    fn ffi_callback_name(&self, nm: &str) -> String {
        format!("Uniffi{}", nm.to_upper_camel_case())
    }

    /// Get the idiomatic C# rendering of an FFI struct name
    fn ffi_struct_name(&self, nm: &str) -> String {
        format!("Uniffi{}", nm.to_upper_camel_case())
    }

    fn ffi_type_label(&self, ffi_type: &FfiType) -> String {
        match ffi_type {
            FfiType::Int16 => "short".to_string(),
            FfiType::Int32 => "int".to_string(),
            FfiType::Int64 | FfiType::Handle => "long".to_string(),
            FfiType::Int8 => "sbyte".to_string(),
            FfiType::UInt16 => "ushort".to_string(),
            FfiType::UInt32 => "uint".to_string(),
            FfiType::UInt64 => "ulong".to_string(),
            FfiType::UInt8 => "byte".to_string(),
            FfiType::Float32 => "float".to_string(),
            FfiType::Float64 => "double".to_string(),
            FfiType::RustArcPtr(_) => "IntPtr".to_string(),
            FfiType::RustBuffer(_) => "RustBuffer".to_string(),
            FfiType::ForeignBytes => "ForeignBytes".to_string(),
            FfiType::Callback(name) => self.ffi_callback_name(name),
            FfiType::Reference(typ) => format!("ref {}", self.ffi_type_label(typ)),
            FfiType::RustCallStatus => "UniffiRustCallStatus".to_string(),
            FfiType::Struct(name) => self.ffi_struct_name(name),
            FfiType::VoidPointer => "IntPtr".to_string(),
        }
    }
}

pub mod filters {
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
            _ => type_name(typ, ci),
        }
    }

    pub(super) fn canonical_name(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(as_ct.as_codetype().canonical_name())
    }

    pub(super) fn ffi_converter_name(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(as_ct.as_codetype().ffi_converter_name())
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
        literal: &Literal,
        as_ct: &impl AsCodeType,
        ci: &ComponentInterface,
    ) -> Result<String, askama::Error> {
        Ok(as_ct.as_codetype().literal(literal, ci))
    }

    pub(super) fn ffi_type(type_: &impl AsType) -> Result<FfiType, askama::Error> {
        Ok(type_.as_type().into())
    }

    /// Get the C# syntax for representing a given low-level `FFIType`.
    pub(super) fn ffi_type_name(type_: &FfiType) -> Result<String, askama::Error> {
        Ok(oracle().ffi_type_label(type_))
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
    pub(super) fn var_name(nm: &str) -> Result<String, askama::Error> {
        Ok(oracle().var_name(nm))
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

    /// Get the idiomatic C# rendering of an FFI struct name
    pub(super) fn ffi_struct_name(nm: &str) -> Result<String, askama::Error> {
        Ok(oracle().ffi_struct_name(nm))
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

    /// Panic with message
    pub(super) fn panic(message: &str) -> Result<String, askama::Error> {
        panic!("{}", message)
    }
}
