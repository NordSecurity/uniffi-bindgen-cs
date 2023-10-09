/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

pub mod gen_cs;
use anyhow::{bail, Result};
use camino::{Utf8Path, Utf8PathBuf};
use clap::Parser;
use fs_err::File;
pub use gen_cs::generate_bindings;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::io::Write;
use uniffi_bindgen::{interface::ComponentInterface, BindingsConfig};

#[derive(Parser)]
#[clap(name = "uniffi-bindgen")]
#[clap(version = clap::crate_version!())]
#[clap(propagate_version = true)]
struct Cli {
    /// Directory in which to write generated files. Default is same folder as .udl file.
    #[clap(long, short)]
    out_dir: Option<Utf8PathBuf>,

    /// Do not try to format the generated bindings.
    #[clap(long, short)]
    no_format: bool,

    /// Path to the optional uniffi config file. If not provided, uniffi-bindgen will try to guess it from the UDL's file location.
    #[clap(long, short)]
    config: Option<Utf8PathBuf>,

    /// Path to the UDL file.
    udl_file: Utf8PathBuf,
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

impl BindingsConfig for ConfigRoot {
    fn update_from_ci(&mut self, ci: &ComponentInterface) {
        self.bindings.csharp.update_from_ci(ci);
    }

    fn update_from_cdylib_name(&mut self, cdylib_name: &str) {
        self.bindings.csharp.update_from_cdylib_name(cdylib_name);
    }

    fn update_from_dependency_configs(&mut self, config_map: HashMap<&str, &Self>) {
        self.bindings.csharp.update_from_dependency_configs(
            config_map
                .iter()
                .map(|(key, config)| (*key, &config.bindings.csharp))
                .collect(),
        );
    }
}

struct BindingGenerator {
    _try_format_code: bool,
}

impl uniffi_bindgen::BindingGenerator for BindingGenerator {
    type Config = ConfigRoot;

    fn write_bindings(
        &self,
        ci: &ComponentInterface,
        config: &Self::Config,
        out_dir: &Utf8Path,
    ) -> anyhow::Result<()> {
        let bindings_file = out_dir.join(format!("{}.cs", ci.namespace()));
        let mut f = File::create(&bindings_file)?;
        write!(f, "{}", generate_bindings(&config.bindings.csharp, &ci)?)?;

        // TODO: find a way to easily format standalone C# files
        // https://github.com/dotnet/format

        Ok(())
    }

    fn check_library_path(&self, library_path: &Utf8Path, cdylib_name: Option<&str>) -> Result<()> {
        if cdylib_name.is_none() {
            bail!("Generate bindings for C# requires a cdylib, but {library_path} was given");
        }
        Ok(())
    }
}

pub fn main() {
    let cli = Cli::parse();
    uniffi_bindgen::generate_external_bindings(
        BindingGenerator {
            _try_format_code: !cli.no_format,
        },
        &cli.udl_file,
        cli.config,
        cli.out_dir,
    )
    .unwrap();
}
