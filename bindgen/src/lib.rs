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
use std::collections::HashMap;
use std::io::Write;
use uniffi_bindgen::interface::ComponentInterface;
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
                check_exclusions(&c.ci, c.config.exclude())?;
                uniffi_bindgen::interface::apply_exclusions(&mut c.ci, c.config.exclude());
            }

            if !c.config.rename().is_empty() {
                uniffi_bindgen::interface::rename(&mut c.ci, c.config.rename());
            }
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

        let config_supplier = {
            use uniffi_bindgen::cargo_metadata::CrateConfigSupplier;
            CrateConfigSupplier::from_cargo_metadata_command(false)?
        };

        uniffi_bindgen::library_mode::generate_bindings(
            &cli.source,
            cli.crate_name,
            &BindingGenerator {
                try_format_code: !cli.no_format,
            },
            &config_supplier,
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

/// Reject exclusions that would silently do the wrong thing.
///
/// `apply_exclusions` is a set of `Vec::retain` calls and returns nothing, so
/// on its own it cannot report either of the cases below. Both are checked
/// here, before anything is removed, because after the retain the evidence is
/// gone.
///
/// **An entry that matches nothing.** Indistinguishable from one that matched,
/// because a retain that keeps everything looks exactly like a retain with
/// nothing to drop. A typo, or an entry written against a renamed item when
/// exclusions are applied before renaming, would fail open: generation
/// succeeds and the item is still there. Where the exclusion existed to keep
/// an internal entry point out of the public surface, failing open is the
/// worst direction to fail in.
///
/// **A method on a type the foreign side implements.** Callback interfaces and
/// traits exported with `with_foreign` cross on a vtable that is a positional
/// struct: one slot per method, matched by ordinal, not by name. Excluding a
/// method removes a slot from the foreign side and cannot remove it from Rust,
/// which builds its vtable from the trait declaration. Every later slot then
/// shifts by one, so Rust calls the wrong function pointer with the wrong
/// argument shape, and the final slot reads past the end of the struct and
/// invokes whatever is there as an `extern "C" fn`. The contract check that
/// might have caught it derives its checksums from the same post-exclusion
/// list, so it is narrowed to the surviving methods and stays quiet.
fn check_exclusions(ci: &ComponentInterface, exclusions: &[String]) -> Result<()> {
    let mut unmatched: Vec<&str> = Vec::new();
    let mut vtable: Vec<&str> = Vec::new();

    for entry in exclusions {
        let entry = entry.as_str();
        let mut matched = false;
        let mut shifts_a_vtable = false;

        // A bare name is a free function or a type.
        matched |= ci.function_definitions().iter().any(|f| f.name() == entry);
        matched |= ci.record_definitions().iter().any(|r| r.name() == entry);
        matched |= ci.enum_definitions().iter().any(|e| e.name() == entry);
        matched |= ci.object_definitions().iter().any(|o| o.name() == entry);
        matched |= ci
            .callback_interface_definitions()
            .iter()
            .any(|c| c.name() == entry);

        // `Type.member` is a method or a constructor.
        if let Some((type_name, member)) = entry.split_once('.') {
            for o in ci.object_definitions() {
                if o.name() != type_name {
                    continue;
                }
                let hit = o.methods().iter().any(|m| m.name() == member)
                    || o.constructors().iter().any(|c| c.name() == member);
                if hit {
                    matched = true;
                    // Trait(Both) and Trait(ForeignOnly): the foreign side
                    // implements it, so there is a vtable to shift.
                    shifts_a_vtable |= o.imp().has_callback_interface();
                }
            }
            for c in ci.callback_interface_definitions() {
                if c.name() == type_name && c.methods().iter().any(|m| m.name() == member) {
                    matched = true;
                    shifts_a_vtable = true;
                }
            }
            for r in ci.record_definitions() {
                if r.name() != type_name {
                    continue;
                }
                matched |= r.methods().iter().any(|m| m.name() == member)
                    || r.constructors().iter().any(|c| c.name() == member);
            }
            for e in ci.enum_definitions() {
                if e.name() != type_name {
                    continue;
                }
                matched |= e.methods().iter().any(|m| m.name() == member)
                    || e.constructors().iter().any(|c| c.name() == member);
            }
        }

        if shifts_a_vtable {
            vtable.push(entry);
        } else if !matched {
            unmatched.push(entry);
        }
    }

    if !vtable.is_empty() {
        anyhow::bail!(
            "`exclude` cannot omit a method the foreign side implements, because the vtable \
             it crosses on is positional and Rust still declares every slot. Excluding one \
             shifts the rest, and the calls that follow land on the wrong function pointer. \
             Remove the method on the Rust side instead, or leave it in the bindings. \
             Offending {}: {}",
            if vtable.len() == 1 { "entry" } else { "entries" },
            vtable.join(", ")
        );
    }
    if !unmatched.is_empty() {
        anyhow::bail!(
            "`exclude` {} nothing in this component, so {} silently not excluding anything. \
             Note that exclusions are applied before renaming, so they name the original \
             Rust items rather than the renamed ones. Offending {}: {}",
            if unmatched.len() == 1 { "entry matches" } else { "entries match" },
            if unmatched.len() == 1 { "it is" } else { "they are" },
            if unmatched.len() == 1 { "entry" } else { "entries" },
            unmatched.join(", ")
        );
    }
    Ok(())
}

#[cfg(test)]
mod exclusion_tests {
    use super::check_exclusions;
    use uniffi_bindgen::interface::ComponentInterface;

    /// A free function, and a callback interface whose vtable is positional.
    fn interface() -> ComponentInterface {
        ComponentInterface::from_webidl(
            r#"
            namespace test {
                void plain_function();
            };

            callback interface Getters {
                string get_string();
                boolean get_bool();
            };
            "#,
            "test",
        )
        .expect("the fixture UDL parses")
    }

    #[test]
    fn excluding_a_free_function_is_allowed() {
        // The safe case, and the only one the fixture suite covers. It has to
        // stay quiet, or the guard is useless for being unusable.
        let ci = interface();
        assert!(check_exclusions(&ci, &["plain_function".to_string()]).is_ok());
    }

    #[test]
    fn excluding_a_whole_callback_interface_is_allowed() {
        // Removing the type removes its vtable with it, so nothing shifts.
        // Only excluding *a method* of one is the hazard.
        let ci = interface();
        assert!(check_exclusions(&ci, &["Getters".to_string()]).is_ok());
    }

    #[test]
    fn excluding_a_callback_interface_method_is_rejected() {
        let ci = interface();
        let err = check_exclusions(&ci, &["Getters.get_string".to_string()])
            .expect_err("a method on a foreign-implemented type shifts the vtable");
        let msg = err.to_string();
        assert!(msg.contains("Getters.get_string"), "names the entry: {msg}");
        assert!(msg.contains("positional"), "says why: {msg}");
    }

    #[test]
    fn an_exclusion_that_matches_nothing_is_rejected() {
        // The failure this replaces is silent: a retain that keeps everything
        // is indistinguishable from a retain with nothing to drop, so a typo
        // shipped the item it was meant to remove.
        let ci = interface();
        let err = check_exclusions(&ci, &["plain_funtcion".to_string()])
            .expect_err("a typo must not pass for a successful exclusion");
        let msg = err.to_string();
        assert!(msg.contains("plain_funtcion"), "names the entry: {msg}");
        assert!(msg.contains("before renaming"), "names the likely cause: {msg}");
    }

    #[test]
    fn the_vtable_hazard_is_reported_ahead_of_a_mere_typo() {
        // Both kinds at once: the dangerous one is what the caller is told
        // about, because acting on the typo first leaves the hazard in place.
        let ci = interface();
        let err = check_exclusions(
            &ci,
            &["Getters.get_bool".to_string(), "nonexistent".to_string()],
        )
        .expect_err("still an error");
        assert!(err.to_string().contains("Getters.get_bool"));
    }
}
