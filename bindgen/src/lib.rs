/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

pub mod gen_cs;

use anyhow::Result;
use camino::{Utf8Path, Utf8PathBuf};
use clap::Parser;
use fs_err::File;
pub use gen_cs::generate_bindings;
use serde::{Deserialize, Serialize};
use std::collections::{BTreeSet, HashMap, HashSet};
use std::io::Write;
use uniffi_bindgen::interface::{AsType, Callable, ComponentInterface, Type};
use uniffi_bindgen::{Component, GenerationSettings};

#[derive(Parser)]
#[clap(name = "uniffi-bindgen")]
#[clap(version = clap::crate_version!())]
#[clap(propagate_version = true)]
struct Cli {
    /// Directory in which to write generated files. Default is same folder as .udl file.
    #[clap(long, short)]
    out_dir: Option<Utf8PathBuf>,

    /// Path to the optional uniffi config file. If not provided, uniffi-bindgen will try to guess it from the UDL's file location.
    #[clap(long, short)]
    config: Option<Utf8PathBuf>,

    /// Pass in a cdylib path rather than a UDL file
    #[clap(long = "library", requires = "out-dir")]
    library_mode: bool,

    /// When `--library` is passed, only generate bindings for one crate
    #[clap(long = "crate", requires = "library-mode")]
    crate_name: Option<String>,

    /// Path to the UDL file, or cdylib if `library-mode` is specified
    source: Utf8PathBuf,

    /// Do not try to format the generated bindings.
    #[clap(long, short)]
    no_format: bool,

    /// With `--library`, read the UDL files the library's metadata names from this directory
    /// instead of locating each crate's source through `cargo metadata`.
    #[clap(long, requires = "library-mode")]
    udl_dir: Option<Utf8PathBuf>,
}

struct UdlDirSupplier(Utf8PathBuf);

impl uniffi_bindgen::BindgenCrateConfigSupplier for UdlDirSupplier {
    fn get_udl(&self, _crate_name: &str, udl_name: &str) -> Result<String> {
        Ok(fs_err::read_to_string(
            self.0.join(format!("{udl_name}.udl")),
        )?)
    }
}

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct ConfigRoot {
    #[serde(default)]
    bindings: ConfigBindings,
}

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct ConfigBindings {
    #[serde(default)]
    csharp: gen_cs::Config,
}

struct BindingGenerator {
    try_format_code: bool,
}

impl uniffi_bindgen::BindingGenerator for BindingGenerator {
    type Config = gen_cs::Config;

    fn new_config(&self, root_toml: &toml::Value) -> Result<Self::Config> {
        Ok(
            match root_toml.get("bindings").and_then(|b| b.get("csharp")) {
                Some(v) => v.clone().try_into()?,
                None => Default::default(),
            },
        )
    }

    fn write_bindings(
        &self,
        settings: &GenerationSettings,
        components: &[Component<Self::Config>],
    ) -> anyhow::Result<()> {
        for Component { ci, config, .. } in components {
            let bindings_file = settings.out_dir.join(format!("{}.cs", ci.namespace()));
            println!("Writing bindings file {bindings_file}");
            let mut f = File::create(&bindings_file)?;

            let mut bindings = generate_bindings(config, ci)?;
            bindings = gen_cs::formatting::add_header(bindings);
            write!(f, "{bindings}")?;

            if self.try_format_code {
                let _ = gen_cs::formatting::format(&bindings_file)
                    .map_err(|e| println!(
                        "Warning: Unable to auto-format {} using CSharpier (hint: 'dotnet tool install -g csharpier'): {e:?}",
                        bindings_file.file_name().unwrap(),
                    ));
            }
        }
        Ok(())
    }

    fn update_component_configs(
        &self,
        settings: &GenerationSettings,
        components: &mut Vec<Component<Self::Config>>,
    ) -> Result<()> {
        // In library mode a `--config` override applies to every component, so an
        // `exclude` entry only has to match in one of them.
        let mut unmatched = BTreeSet::new();
        let mut matched = HashSet::new();
        for c in &mut *components {
            c.config
                .namespace
                .get_or_insert_with(|| format!("uniffi.{}", c.ci.namespace()));

            c.config.cdylib_name.get_or_insert_with(|| {
                settings
                    .cdylib
                    .clone()
                    .unwrap_or_else(|| format!("uniffi_{}", c.ci.namespace()))
            });

            // Exclusions are applied before renaming, so `exclude` names the original items.
            if !c.config.exclude().is_empty() {
                let missing = apply_exclusions(&mut c.ci, c.config.exclude())?;
                for entry in c.config.exclude() {
                    if missing.contains(&entry.as_str()) {
                        unmatched.insert(entry.clone());
                    } else {
                        matched.insert(entry.clone());
                    }
                }
            }

            if !c.config.rename().is_empty() {
                uniffi_bindgen::interface::rename(&mut c.ci, c.config.rename());
            }
        }
        let unmatched: Vec<_> = unmatched
            .iter()
            .filter(|e| !matched.contains(*e))
            .map(String::as_str)
            .collect();
        if !unmatched.is_empty() {
            anyhow::bail!(
                "`exclude` {} nothing in any component, so {} silently not excluding anything. \
                 Note that exclusions are applied before renaming, so they name the original \
                 Rust items rather than the renamed ones. Offending {}: {}",
                if unmatched.len() == 1 {
                    "entry matches"
                } else {
                    "entries match"
                },
                if unmatched.len() == 1 {
                    "it is"
                } else {
                    "they are"
                },
                if unmatched.len() == 1 {
                    "entry"
                } else {
                    "entries"
                },
                unmatched.join(", ")
            );
        }
        let packages = HashMap::<String, String>::from_iter(
            components
                .iter()
                .map(|c| (c.ci.crate_name().to_string(), c.config.package_name())),
        );
        for c in &mut *components {
            for (ext_crate, ext_package) in &packages {
                if ext_crate != c.ci.crate_name()
                    && !c.config.external_packages.contains_key(ext_crate)
                {
                    c.config
                        .external_packages
                        .insert(ext_crate.to_string(), ext_package.clone());
                }
            }
        }
        Ok(())
    }
}

pub fn main() -> Result<()> {
    let cli = Cli::parse();

    if cli.library_mode {
        let out_dir = cli
            .out_dir
            .expect("--out-dir is required when using --library");

        let config_supplier: Box<dyn uniffi_bindgen::BindgenCrateConfigSupplier> = match cli.udl_dir
        {
            Some(udl_dir) => Box::new(UdlDirSupplier(udl_dir)),
            None => {
                use uniffi_bindgen::cargo_metadata::CrateConfigSupplier;
                Box::new(CrateConfigSupplier::from_cargo_metadata_command(false)?)
            }
        };

        uniffi_bindgen::library_mode::generate_bindings(
            &cli.source,
            cli.crate_name,
            &BindingGenerator {
                try_format_code: !cli.no_format,
            },
            config_supplier.as_ref(),
            cli.config.as_deref(),
            &out_dir,
            !cli.no_format,
        )
        .map(|_| ())
    } else {
        uniffi_bindgen::generate_external_bindings(
            &BindingGenerator {
                try_format_code: !cli.no_format,
            },
            &cli.source,
            cli.config.as_deref(),
            cli.out_dir.as_deref(),
            None::<&Utf8Path>,
            cli.crate_name.as_deref(),
            !cli.no_format,
        )
    }
}

/// Apply `exclude` to `ci`, rejecting results that would silently do the wrong thing.
///
/// `uniffi_bindgen::interface::apply_exclusions` is a set of `Vec::retain` calls
/// and returns nothing, so on its own it cannot report any of the cases below.
/// Rather than predicting what it will remove, this snapshots the interface,
/// lets upstream apply the exclusions, and judges what actually changed. That
/// keeps the checks in step with upstream's semantics: excluding a whole type
/// together with one of its members is fine, for instance, because the type
/// goes first and the member entry is then a no-op.
///
/// **A method removed from a type the foreign side implements.** Callback
/// interfaces and traits exported with `with_foreign` cross on a vtable that is
/// a positional struct: one slot per method, matched by ordinal, not by name.
/// Removing a method drops a slot from the foreign side and cannot drop it from
/// Rust, which builds its vtable from the trait declaration. Every later slot
/// then shifts by one, so Rust calls the wrong function pointer with the wrong
/// argument shape, and the final slot reads past the end of the struct and
/// invokes whatever is there as an `extern "C" fn`. The contract check that
/// might have caught it derives its checksums from the same post-exclusion
/// list, so it is narrowed to the surviving methods and stays quiet. Excluding
/// the whole type is fine, since its vtable goes with it.
///
/// **A removed type that a remaining item still uses.** Upstream drops the type
/// definition but leaves every reference to it, so generation succeeds and the
/// C# then fails to compile on an unknown type. Reported here instead, naming
/// the items that still use it.
///
/// **An entry that matches nothing.** Returned rather than rejected, because
/// the caller has to check it across every component. A retain that keeps
/// everything looks exactly like a retain with nothing to drop, so a typo, or
/// an entry written against a renamed item when exclusions are applied before
/// renaming, would fail open: generation succeeds and the item is still there.
/// Where the exclusion existed to keep an internal entry point out of the
/// public surface, failing open is the worst direction to fail in.
fn apply_exclusions<'a>(
    ci: &mut ComponentInterface,
    exclusions: &'a [String],
) -> Result<Vec<&'a str>> {
    let items_before = excludable_items(ci);
    let vtables_before = vtables(ci);
    let types_before = defined_types(ci);

    uniffi_bindgen::interface::apply_exclusions(ci, exclusions);

    // A vtable type that survived, but lost methods.
    let vtables_after = vtables(ci);
    let mut shifted = Vec::new();
    for (name, before) in &vtables_before {
        if let Some(after) = vtables_after.get(name) {
            for method in before.iter().filter(|m| !after.contains(m)) {
                shifted.push(format!("{name}.{method}"));
            }
        }
    }
    if !shifted.is_empty() {
        shifted.sort();
        anyhow::bail!(
            "`exclude` cannot omit a method the foreign side implements, because the vtable \
             it crosses on is positional and Rust still declares every slot. Excluding one \
             shifts the rest, and the calls that follow land on the wrong function pointer. \
             Remove the method on the Rust side instead, exclude the whole type, or leave it \
             in the bindings. Offending {}: {}",
            plural(shifted.len(), "entry", "entries"),
            shifted.join(", ")
        );
    }

    // A type definition that was removed while a remaining item still uses it.
    let types_after = defined_types(ci);
    let removed: BTreeSet<_> = types_before.difference(&types_after).collect();
    let mut dangling = BTreeSet::new();
    for (user, ty) in type_users(ci) {
        if let Some(key) = type_key(ty) {
            if removed.contains(&key) {
                dangling.insert(format!("{} (used by {user})", key.1));
            }
        }
    }
    if !dangling.is_empty() {
        anyhow::bail!(
            "`exclude` removes a type that the remaining bindings still use, so the generated \
             C# would not compile. Exclude the items that use it as well, or keep the type in \
             the bindings. Offending {}: {}",
            plural(dangling.len(), "type", "types"),
            dangling.into_iter().collect::<Vec<_>>().join(", ")
        );
    }

    Ok(exclusions
        .iter()
        .map(String::as_str)
        .filter(|e| !items_before.contains(*e))
        .collect())
}

fn plural<'a>(n: usize, one: &'a str, many: &'a str) -> &'a str {
    if n == 1 {
        one
    } else {
        many
    }
}

/// Every name an `exclude` entry can match, in upstream's `name` / `Type.member` form.
fn excludable_items(ci: &ComponentInterface) -> HashSet<String> {
    let mut items = HashSet::new();
    let mut add_type = |name: &str, members: Vec<&str>| {
        items.insert(name.to_string());
        items.extend(members.into_iter().map(|m| format!("{name}.{m}")));
    };
    for f in ci.function_definitions() {
        add_type(f.name(), vec![]);
    }
    for o in ci.object_definitions() {
        let methods = o.methods().into_iter().map(|m| m.name());
        let constructors = o.constructors().into_iter().map(|c| c.name());
        add_type(o.name(), methods.chain(constructors).collect());
    }
    for r in ci.record_definitions() {
        let methods = r.methods().iter().map(|m| m.name());
        let constructors = r.constructors().iter().map(|c| c.name());
        add_type(r.name(), methods.chain(constructors).collect());
    }
    for e in ci.enum_definitions() {
        let methods = e.methods().iter().map(|m| m.name());
        let constructors = e.constructors().iter().map(|c| c.name());
        add_type(e.name(), methods.chain(constructors).collect());
    }
    for c in ci.callback_interface_definitions() {
        add_type(
            c.name(),
            c.methods().into_iter().map(|m| m.name()).collect(),
        );
    }
    items
}

/// The method names of every type the foreign side implements, keyed by type
/// name: callback interfaces, and traits exported with `with_foreign`.
fn vtables(ci: &ComponentInterface) -> HashMap<String, Vec<String>> {
    let mut vtables = HashMap::new();
    for o in ci.object_definitions() {
        if o.imp().has_callback_interface() {
            let methods = o.methods().iter().map(|m| m.name().to_string()).collect();
            vtables.insert(o.name().to_string(), methods);
        }
    }
    for c in ci.callback_interface_definitions() {
        let methods = c.methods().iter().map(|m| m.name().to_string()).collect();
        vtables.insert(c.name().to_string(), methods);
    }
    vtables
}

/// Identifies a user-defined type independently of how it is referenced.
fn type_key(ty: &Type) -> Option<(String, String)> {
    Some((
        ty.module_path().unwrap_or_default().to_string(),
        ty.name()?.to_string(),
    ))
}

/// The user-defined types this component defines, and so could exclude.
fn defined_types(ci: &ComponentInterface) -> BTreeSet<(String, String)> {
    let records = ci.record_definitions().iter().map(AsType::as_type);
    let enums = ci.enum_definitions().iter().map(AsType::as_type);
    let objects = ci.object_definitions().iter().map(AsType::as_type);
    let callbacks = ci
        .callback_interface_definitions()
        .iter()
        .map(AsType::as_type);
    records
        .chain(enums)
        .chain(objects)
        .chain(callbacks)
        .filter_map(|t| type_key(&t))
        .collect()
}

/// Every type referenced by an item that will be generated, paired with that item's name.
fn type_users(ci: &ComponentInterface) -> Vec<(&str, &Type)> {
    fn add<'a>(
        uses: &mut Vec<(&'a str, &'a Type)>,
        user: &'a str,
        types: impl Iterator<Item = &'a Type>,
    ) {
        for ty in types {
            collect_types(ty, &mut |t| uses.push((user, t)));
        }
    }

    let mut uses = Vec::new();
    for f in ci.function_definitions() {
        add(&mut uses, f.name(), f.iter_types());
    }
    for o in ci.object_definitions() {
        add(&mut uses, o.name(), o.iter_types());
    }
    for r in ci.record_definitions() {
        add(&mut uses, r.name(), r.iter_types());
    }
    for e in ci.enum_definitions() {
        add(&mut uses, e.name(), e.iter_types());
    }
    for c in ci.callback_interface_definitions() {
        add(&mut uses, c.name(), c.iter_types());
    }
    uses
}

/// `Type::iter_types` does not look inside `Type::Box`, which 0.32 uses for
/// recursive records and enums, so step into it here.
fn collect_types<'a>(ty: &'a Type, visit: &mut impl FnMut(&'a Type)) {
    for t in ty.iter_types() {
        visit(t);
        if let Type::Box { inner_type } = t {
            collect_types(inner_type, visit);
        }
    }
}

#[cfg(test)]
mod udl_dir_tests {
    use super::UdlDirSupplier;
    use uniffi_bindgen::BindgenCrateConfigSupplier;

    #[test]
    fn a_missing_udl_names_the_file() {
        let supplier = UdlDirSupplier("no-such-dir".into());
        let err = supplier.get_udl("my_crate", "my_udl").unwrap_err();
        assert!(err.to_string().contains("my_udl.udl"), "{err}");
    }
}

#[cfg(test)]
mod exclusion_tests {
    use super::apply_exclusions;
    use uniffi_bindgen::interface::ComponentInterface;

    /// A free function, an enum that a function uses, a callback interface and
    /// a foreign-implemented trait, both of whose vtables are positional.
    fn interface() -> ComponentInterface {
        ComponentInterface::from_webidl(
            r#"
            namespace test {
                void plain_function();
                Colour pick_colour();
            };

            enum Colour { "Red", "Green" };

            callback interface Getters {
                string get_string();
                boolean get_bool();
            };

            [Trait, WithForeign]
            interface Shape {
                double area();
                string label();
            };
            "#,
            "test",
        )
        .expect("the fixture UDL parses")
    }

    fn exclude(entries: &[&str]) -> Vec<String> {
        entries.iter().map(|e| e.to_string()).collect()
    }

    #[test]
    fn excluding_a_free_function_is_allowed() {
        // The safe case, and the only one the fixture suite covers. It has to
        // stay quiet, or the guard is useless for being unusable.
        let mut ci = interface();
        let entries = exclude(&["plain_function"]);
        let unmatched = apply_exclusions(&mut ci, &entries).unwrap();
        assert!(unmatched.is_empty());
        assert!(ci.get_function_definition("plain_function").is_none());
    }

    #[test]
    fn excluding_a_whole_callback_interface_is_allowed() {
        // Removing the type removes its vtable with it, so nothing shifts.
        // Only excluding *a method* of one is the hazard.
        let mut ci = interface();
        assert!(apply_exclusions(&mut ci, &exclude(&["Getters"])).is_ok());
    }

    #[test]
    fn excluding_a_whole_type_and_one_of_its_methods_is_allowed() {
        // Upstream removes the type first, so the member entry is a no-op and
        // no vtable is left to shift. It still names a real item, so it is not
        // reported as unmatched either.
        let mut ci = interface();
        let entries = exclude(&["Getters", "Getters.get_string", "Shape", "Shape.area"]);
        let unmatched = apply_exclusions(&mut ci, &entries).expect("nothing shifts");
        assert!(unmatched.is_empty(), "unexpected: {unmatched:?}");
    }

    #[test]
    fn excluding_a_callback_interface_method_is_rejected() {
        let mut ci = interface();
        let err = apply_exclusions(&mut ci, &exclude(&["Getters.get_string"]))
            .expect_err("a method on a foreign-implemented type shifts the vtable");
        let msg = err.to_string();
        assert!(msg.contains("Getters.get_string"), "names the entry: {msg}");
        assert!(msg.contains("positional"), "says why: {msg}");
    }

    #[test]
    fn excluding_a_with_foreign_trait_method_is_rejected() {
        let mut ci = interface();
        let err = apply_exclusions(&mut ci, &exclude(&["Shape.area"]))
            .expect_err("a `with_foreign` trait crosses on a vtable too");
        assert!(err.to_string().contains("Shape.area"));
    }

    #[test]
    fn excluding_a_type_that_is_still_used_is_rejected() {
        // Upstream drops the definition but not the references to it, which
        // would only surface later as a C# compile error.
        let mut ci = interface();
        let err = apply_exclusions(&mut ci, &exclude(&["Colour"]))
            .expect_err("`pick_colour` still returns `Colour`");
        let msg = err.to_string();
        assert!(
            msg.contains("Colour (used by pick_colour)"),
            "names both: {msg}"
        );
    }

    #[test]
    fn excluding_a_type_with_everything_that_uses_it_is_allowed() {
        let mut ci = interface();
        assert!(apply_exclusions(&mut ci, &exclude(&["Colour", "pick_colour"])).is_ok());
    }

    #[test]
    fn an_exclusion_that_matches_nothing_is_reported() {
        // The failure this replaces is silent: a retain that keeps everything
        // is indistinguishable from a retain with nothing to drop, so a typo
        // shipped the item it was meant to remove.
        let mut ci = interface();
        let entries = exclude(&["plain_funtcion", "plain_function"]);
        let unmatched = apply_exclusions(&mut ci, &entries).expect("no vtable hazard");
        assert_eq!(unmatched, ["plain_funtcion"]);
    }

    #[test]
    fn the_vtable_hazard_is_reported_ahead_of_a_mere_typo() {
        // Both kinds at once: the dangerous one is what the caller is told
        // about, because acting on the typo first leaves the hazard in place.
        let mut ci = interface();
        let err = apply_exclusions(&mut ci, &exclude(&["Getters.get_bool", "nonexistent"]))
            .expect_err("still an error");
        assert!(err.to_string().contains("Getters.get_bool"));
    }
}
