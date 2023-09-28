/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use std::borrow::Borrow;
use std::cell::RefCell;
use std::collections::{BTreeSet, HashMap, HashSet};

use anyhow::{Context, Result};
use askama::Template;
use heck::{ToLowerCamelCase, ToUpperCamelCase};
use serde::{Deserialize, Serialize};

use uniffi_bindgen::backend::{CodeOracle, CodeType, TemplateExpression, TypeIdentifier};
use uniffi_bindgen::interface::*;
use uniffi_bindgen::MergeWith;

mod callback_interface;
mod compounds;
mod custom;
mod enum_;
mod error;
mod external;
mod miscellany;
mod object;
mod primitives;
mod record;

// config options to customize the generated C# bindings.
#[derive(Debug, Default, Clone, Serialize, Deserialize)]
pub struct Config {
    package_name: Option<String>,
    cdylib_name: Option<String>,
    #[serde(default)]
    custom_types: HashMap<String, CustomTypeConfig>,
    #[serde(default)]
    external_packages: HashMap<String, String>,
    #[serde(default)]
    namespace: Option<String>,
    #[serde(default)]
    global_methods_class_name: Option<String>,
}

#[derive(Debug, Default, Clone, Serialize, Deserialize)]
pub struct CustomTypeConfig {
    imports: Option<Vec<String>>,
    type_name: Option<String>,
    into_custom: TemplateExpression,
    from_custom: TemplateExpression,
}

impl Config {
    pub fn package_name(&self) -> String {
        if let Some(package_name) = &self.package_name {
            package_name.clone()
        } else {
            "uniffi".into()
        }
    }

    pub fn cdylib_name(&self) -> String {
        if let Some(cdylib_name) = &self.cdylib_name {
            cdylib_name.clone()
        } else {
            "uniffi".into()
        }
    }
}

impl MergeWith for Config {
    fn merge_with(&self, other: &Self) -> Self {
        Config {
            package_name: self.package_name.merge_with(&other.package_name),
            cdylib_name: self.cdylib_name.merge_with(&other.cdylib_name),
            custom_types: self.custom_types.merge_with(&other.custom_types),
            external_packages: self.external_packages.merge_with(&other.external_packages),
            namespace: self.namespace.merge_with(&other.namespace),
            global_methods_class_name: self
                .global_methods_class_name
                .merge_with(&other.global_methods_class_name),
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
    cs_config: &'a Config,
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
    fn new(cs_config: &'a Config, ci: &'a ComponentInterface) -> Self {
        Self {
            cs_config,
            ci,
            include_once_names: RefCell::new(HashSet::new()),
            imports: RefCell::new(BTreeSet::new()),
            type_aliases: RefCell::new(BTreeSet::new()),
        }
    }

    // Get the package name for an external type
    fn external_type_package_name(&self, crate_name: &str) -> String {
        match self.cs_config.external_packages.get(crate_name) {
            Some(name) => name.clone(),
            None => crate_name.to_string(),
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
            .filter_map(|t| t.initialization_fn(&CsCodeOracle))
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

#[derive(Clone)]
pub struct CsCodeOracle;

impl CsCodeOracle {
    // Map `Type` instances to a `Box<dyn CodeType>` for that type.
    //
    // There is a companion match in `templates/Types.kt` which performs a similar function for the
    // template code.
    //
    //   - When adding additional types here, make sure to also add a match arm to the `Types.kt` template.
    //   - To keep things managable, let's try to limit ourselves to these 2 mega-matches
    fn create_code_type(&self, type_: TypeIdentifier) -> Box<dyn CodeType> {
        match type_ {
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

            Type::Timestamp => Box::new(miscellany::TimestampCodeType),
            Type::Duration => Box::new(miscellany::DurationCodeType),

            Type::Enum(id) => Box::new(enum_::EnumCodeType::new(id)),
            Type::Object(id) => Box::new(object::ObjectCodeType::new(id)),
            Type::Record(id) => Box::new(record::RecordCodeType::new(id)),
            Type::Error(id) => Box::new(error::ErrorCodeType::new(id)),
            Type::CallbackInterface(id) => {
                Box::new(callback_interface::CallbackInterfaceCodeType::new(id))
            }
            Type::Optional(inner) => Box::new(compounds::OptionalCodeType::new(*inner)),
            Type::Sequence(inner) => Box::new(compounds::SequenceCodeType::new(*inner)),
            Type::Map(key, value) => Box::new(compounds::MapCodeType::new(*key, *value)),
            Type::External { name, .. } => Box::new(external::ExternalCodeType::new(name)),
            Type::Custom { name, .. } => Box::new(custom::CustomCodeType::new(name)),

            Type::Unresolved { name } => {
                unreachable!("Type `{name}` must be resolved before calling create_code_type")
            }
        }
    }
}

impl CodeOracle for CsCodeOracle {
    fn find(&self, type_: &TypeIdentifier) -> Box<dyn CodeType> {
        self.create_code_type(type_.clone())
    }

    /// Get the idiomatic C# rendering of a class name (for enums, records, errors, etc).
    fn class_name(&self, nm: &str) -> String {
        nm.to_string().to_upper_camel_case()
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

    /// Get the idiomatic C# rendering of an exception name
    ///
    /// This replaces "Error" at the end of the name with "Exception".  Rust code typically uses
    /// "Error" for any type of error, but in C# errors are implemented as exceptions, so the
    /// naming should match that.
    fn error_name(&self, nm: &str) -> String {
        // errors are a class in C#.
        let name = self.class_name(nm);
        match name.strip_suffix("Error") {
            None => name,
            Some(stripped) => format!("{}Exception", stripped),
        }
    }

    fn ffi_type_label(&self, ffi_type: &FfiType) -> String {
        match ffi_type {
            FfiType::Int16 => "short".to_string(),
            FfiType::Int32 => "int".to_string(),
            FfiType::Int64 => "long".to_string(),
            FfiType::Int8 => "sbyte".to_string(),
            FfiType::UInt16 => "ushort".to_string(),
            FfiType::UInt32 => "uint".to_string(),
            FfiType::UInt64 => "ulong".to_string(),
            FfiType::UInt8 => "byte".to_string(),
            FfiType::Float32 => "float".to_string(),
            FfiType::Float64 => "double".to_string(),
            FfiType::RustArcPtr(name) => format!("{}SafeHandle", name),
            FfiType::RustArcPtrUnsafe(_) => format!("IntPtr"),
            FfiType::RustBuffer(_) => "RustBuffer".to_string(),
            FfiType::ForeignBytes => "ForeignBytes".to_string(),
            FfiType::ForeignCallback => "ForeignCallback".to_string(),
        }
    }
}

pub mod filters {
    use super::*;

    fn oracle() -> &'static CsCodeOracle {
        &CsCodeOracle
    }

    pub fn type_name(codetype: &impl CodeType) -> Result<String, askama::Error> {
        Ok(codetype.type_label(oracle()))
    }

    pub fn canonical_name(codetype: &impl CodeType) -> Result<String, askama::Error> {
        Ok(codetype.canonical_name(oracle()))
    }

    pub fn ffi_converter_name(codetype: &impl CodeType) -> Result<String, askama::Error> {
        Ok(codetype.ffi_converter_name(oracle()))
    }

    pub fn lower_fn(codetype: &impl CodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.Lower",
            codetype.ffi_converter_name(oracle())
        ))
    }

    pub fn allocation_size_fn(codetype: &impl CodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.AllocationSize",
            codetype.ffi_converter_name(oracle())
        ))
    }

    pub fn write_fn(codetype: &impl CodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.Write",
            codetype.ffi_converter_name(oracle())
        ))
    }

    pub fn lift_fn(codetype: &impl CodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.Lift",
            codetype.ffi_converter_name(oracle())
        ))
    }

    pub fn read_fn(codetype: &impl CodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.Read",
            codetype.ffi_converter_name(oracle())
        ))
    }

    pub fn render_literal(
        literal: &Literal,
        codetype: &impl CodeType,
    ) -> Result<String, askama::Error> {
        Ok(codetype.literal(oracle(), literal))
    }

    /// Get the C# syntax for representing a given low-level `FFIType`.
    pub fn ffi_type_name(type_: &FfiType) -> Result<String, askama::Error> {
        Ok(oracle().ffi_type_label(type_))
    }

    /// Get the idiomatic C# rendering of a class name (for enums, records, errors, etc).
    pub fn class_name(nm: &str) -> Result<String, askama::Error> {
        Ok(oracle().class_name(nm))
    }

    /// Get the idiomatic C# rendering of a function name.
    pub fn fn_name(nm: &str) -> Result<String, askama::Error> {
        Ok(oracle().fn_name(nm))
    }

    /// Get the idiomatic C# rendering of a variable name.
    pub fn var_name(nm: &str) -> Result<String, askama::Error> {
        Ok(oracle().var_name(nm))
    }

    /// Get the idiomatic C# rendering of an individual enum variant.
    pub fn enum_variant(nm: &str) -> Result<String, askama::Error> {
        Ok(oracle().enum_variant_name(nm))
    }

    /// Get the idiomatic C# rendering of an exception name, replacing
    /// `Error` with `Exception`.
    pub fn exception_name(nm: &str) -> Result<String, askama::Error> {
        Ok(oracle().error_name(nm))
    }

    /// Get the idiomatic C# rendering of docstring
    pub fn docstring(docstring: &str, spaces: &i32) -> Result<String, askama::Error> {
        let middle = textwrap::indent(&textwrap::dedent(docstring), "/// ");
        let wrapped = format!("/// <summary>\n{middle}\n/// </summary>");

        let spaces = usize::try_from(*spaces).unwrap_or_default();
        Ok(textwrap::indent(&wrapped, &" ".repeat(spaces)))
    }
}
