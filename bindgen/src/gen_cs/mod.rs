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

use uniffi_bindgen::backend::{CodeType, TemplateExpression, Type};
use uniffi_bindgen::interface::*;
use uniffi_bindgen::ComponentInterface;

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
    namespace: Option<String>,
    cdylib_name: Option<String>,
    #[serde(default)]
    custom_types: HashMap<String, CustomTypeConfig>,
    #[serde(default)]
    external_packages: HashMap<String, String>,
    global_methods_class_name: Option<String>,
}

#[derive(Debug, Default, Clone, Serialize, Deserialize)]
pub struct CustomTypeConfig {
    imports: Option<Vec<String>>,
    type_name: Option<String>,
    into_custom: TemplateExpression,
    from_custom: TemplateExpression,
}

impl uniffi_bindgen::BindingsConfig for Config {
    fn update_from_ci(&mut self, ci: &ComponentInterface) {
        self.namespace
            .get_or_insert_with(|| format!("uniffi.{}", ci.namespace()));
    }

    fn update_from_cdylib_name(&mut self, cdylib_name: &str) {
        self.cdylib_name
            .get_or_insert_with(|| cdylib_name.to_string());
    }

    fn update_from_dependency_configs(&mut self, _config_map: HashMap<&str, &Self>) {
        // TODO
    }
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

pub trait AsCodeType {
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
            Type::Bytes => Box::new(compounds::SequenceCodeType::new(Type::UInt8)),

            Type::Timestamp => Box::new(miscellany::TimestampCodeType),
            Type::Duration => Box::new(miscellany::DurationCodeType),

            Type::Enum(id) => Box::new(enum_::EnumCodeType::new(id)),
            Type::Object { name, .. } => Box::new(object::ObjectCodeType::new(name)),
            Type::Record(id) => Box::new(record::RecordCodeType::new(id)),
            Type::CallbackInterface(id) => {
                Box::new(callback_interface::CallbackInterfaceCodeType::new(id))
            }
            Type::ForeignExecutor => panic!("TODO implement async"),
            Type::Optional(inner) => Box::new(compounds::OptionalCodeType::new(*inner)),
            Type::Sequence(inner) => Box::new(compounds::SequenceCodeType::new(*inner)),
            Type::Map(key, value) => Box::new(compounds::MapCodeType::new(*key, *value)),
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
            FfiType::RustBuffer(_) => "RustBuffer".to_string(),
            FfiType::ForeignBytes => "ForeignBytes".to_string(),
            FfiType::ForeignCallback => "ForeignCallback".to_string(),
            FfiType::ForeignExecutorHandle => panic!("TODO implement async"),
            FfiType::ForeignExecutorCallback => panic!("TODO implement async"),
            FfiType::FutureCallback { .. } => {
                panic!("TODO implement async")
            }
            FfiType::FutureCallbackData => panic!("TODO implement async"),
        }
    }
}

pub mod filters {
    use super::*;

    fn oracle() -> &'static CsCodeOracle {
        &CsCodeOracle
    }

    pub fn type_name(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(as_ct.as_codetype().type_label())
    }

    pub fn type_name_custom(typ: &Type) -> Result<String, askama::Error> {
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
            _ => type_name(typ),
        }
    }

    pub fn canonical_name(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(as_ct.as_codetype().canonical_name())
    }

    pub fn ffi_converter_name(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(as_ct.as_codetype().ffi_converter_name())
    }

    pub fn lower_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.Lower",
            as_ct.as_codetype().ffi_converter_name()
        ))
    }

    pub fn allocation_size_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.AllocationSize",
            as_ct.as_codetype().ffi_converter_name()
        ))
    }

    pub fn write_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.Write",
            as_ct.as_codetype().ffi_converter_name()
        ))
    }

    pub fn lift_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.Lift",
            as_ct.as_codetype().ffi_converter_name()
        ))
    }

    pub fn read_fn(as_ct: &impl AsCodeType) -> Result<String, askama::Error> {
        Ok(format!(
            "{}.INSTANCE.Read",
            as_ct.as_codetype().ffi_converter_name()
        ))
    }

    pub fn render_literal(
        literal: &Literal,
        as_ct: &impl AsCodeType,
    ) -> Result<String, askama::Error> {
        Ok(as_ct.as_codetype().literal(literal))
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

    // Get C# error code type representation.
    pub fn as_error(type_: &Type) -> Result<error::ErrorCodeTypeProvider, askama::Error> {
        match type_ {
            Type::Enum(id) => Ok(error::ErrorCodeTypeProvider { id }),
            // XXX - not sure how we are supposed to return askama::Error?
            _ => panic!("unsupported type for error: {type_:?}"),
        }
    }

    /// Get the idiomatic C# rendering of docstring
    pub fn docstring(docstring: &str, spaces: &i32) -> Result<String, askama::Error> {
        let middle = textwrap::indent(&textwrap::dedent(docstring), "/// ");
        let wrapped = format!("/// <summary>\n{middle}\n/// </summary>");

        let spaces = usize::try_from(*spaces).unwrap_or_default();
        Ok(textwrap::indent(&wrapped, &" ".repeat(spaces)))
    }

    /// Panic with message
    pub fn panic(message: &str) -> Result<String, askama::Error> {
        panic!("{}", message)
    }
}
