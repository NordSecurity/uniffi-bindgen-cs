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

use uniffi_bindgen::backend::Type;
use uniffi_bindgen::interface::*;
use uniffi_bindgen::ComponentInterface;

mod callback_interface;
mod compounds;
mod custom;
mod enum_;
mod external;
mod filters;
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

    fn error_converter_name(&self) -> String {
        self.ffi_converter_name()
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
    into_custom: String,
    from_custom: String,
}

impl CustomTypeConfig {
    fn lift(&self, name: &str) -> String {
        self.into_custom.replace("{}", name)
    }
    fn lower(&self, name: &str) -> String {
        self.from_custom.replace("{}", name)
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
            .iter_local_types()
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

trait AsCodeType {
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
            Type::Object { name, imp, .. } => Box::new(object::ObjectCodeType::new(name, imp)),
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
        if ci.is_name_used_as_error(nm) { self.convert_error_suffix(&name) } else { name }
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

    /// Get the idiomatic C# rendering of a property name (for record positional parameters).
    /// Uses PascalCase per Microsoft naming conventions. Prefixes with @ only for C# keywords.
    fn property_name(&self, nm: &str) -> String {
        let name = nm.to_string().to_upper_camel_case();
        if Self::is_csharp_keyword(&name) {
            format!("@{}", name)
        } else {
            name
        }
    }

    /// Check if a name is a C# keyword that requires an @ prefix.
    fn is_csharp_keyword(name: &str) -> bool {
        matches!(
            name,
            // C# reserved keywords (case-sensitive)
            "Abstract" | "As" | "Base" | "Bool" | "Break" | "Byte" | "Case" | "Catch"
            | "Char" | "Checked" | "Class" | "Const" | "Continue" | "Decimal" | "Default"
            | "Delegate" | "Do" | "Double" | "Else" | "Enum" | "Event" | "Explicit"
            | "Extern" | "False" | "Finally" | "Fixed" | "Float" | "For" | "Foreach"
            | "Goto" | "If" | "Implicit" | "In" | "Int" | "Interface" | "Internal" | "Is"
            | "Lock" | "Long" | "Namespace" | "New" | "Null" | "Object" | "Operator"
            | "Out" | "Override" | "Params" | "Private" | "Protected" | "Public"
            | "Readonly" | "Ref" | "Return" | "Sbyte" | "Sealed" | "Short" | "Sizeof"
            | "Stackalloc" | "Static" | "String" | "Struct" | "Switch" | "This" | "Throw"
            | "True" | "Try" | "Typeof" | "Uint" | "Ulong" | "Unchecked" | "Unsafe"
            | "Ushort" | "Using" | "Virtual" | "Void" | "Volatile" | "While"
        )
    }

    /// Get the idiomatic C# rendering of an individual enum variant.
    fn enum_variant_name(&self, nm: &str) -> String {
        nm.to_string().to_upper_camel_case()
    }

    /// Get the idiomatic C# rendering of an FFI callback function name
    fn ffi_callback_name(&self, nm: &str) -> String {
        format!("Uniffi{}", nm.to_upper_camel_case())
    }

    fn ffi_callback_impl(&self, nm: &str) -> String {
        format!("UniffiCallbackInterface{nm}")
    }

    /// Get the idiomatic C# rendering of an FFI struct name
    fn ffi_struct_name(&self, nm: &str) -> String {
        format!("Uniffi{}", nm.to_upper_camel_case())
    }

    fn interface_name(&self, nm: &str) -> String {
        format!("I{nm}")
    }

    fn impl_name(&self, nm: &str) -> String {
        format!("{nm}Impl")
    }

    fn object_names(&self, obj: &Object, ci: &ComponentInterface) -> (String, String) {
        let class_name = self.class_name(obj.name(), ci);
        if obj.has_callback_interface() {
            // If the object has callback interface we will generate
            // An interface Object and an implementation ObjectImpl
            let impl_name = self.impl_name(&class_name);
            (class_name, impl_name)
        } else {
            // In regular cases we will use C# convention
            // An interface IObject and an implementation Object
            (self.interface_name(&class_name), class_name)
        }
    }

    fn ffi_type_label(&self, ffi_type: &FfiType, prefix_struct: bool) -> String {
        match ffi_type {
            FfiType::Int16 => "short".to_string(),
            FfiType::Int32 => "int".to_string(),
            FfiType::Int64 => "long".to_string(),
            FfiType::Handle => "IntPtr".to_string(),
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
            FfiType::Callback(_) => "IntPtr".to_string(),
            FfiType::Reference(typ) => format!("IntPtr /*{}*/", self.ffi_type_label(typ, prefix_struct)),
            FfiType::MutReference(typ) => {
                format!("IntPtr /*{}*/", self.ffi_type_label(typ, prefix_struct))
            }
            FfiType::RustCallStatus => "UniffiRustCallStatus".to_string(),
            FfiType::Struct(name) => {
                if prefix_struct {
                    format!("_UniFFILib.{}", self.ffi_struct_name(name))
                } else {
                    self.ffi_struct_name(name)
                }
            }
            FfiType::VoidPointer => "IntPtr".to_string(),
        }
    }
}
