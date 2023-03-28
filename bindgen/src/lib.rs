/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

pub mod gen_cs;
use camino::{Utf8Path, Utf8PathBuf};
use clap::Parser;
use fs_err::File;
pub use gen_cs::{generate_bindings, Config};
use std::io::Write;
use uniffi_bindgen;
use uniffi_bindgen::interface::ComponentInterface;

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

impl uniffi_bindgen::BindingGeneratorConfig for gen_cs::Config {
    fn get_entry_from_bindings_table(bindings: &toml::Value) -> Option<toml::Value> {
        bindings.get("csharp").map(|v| v.clone())
    }

    fn get_config_defaults(_ci: &ComponentInterface) -> Vec<(String, toml::Value)> {
        vec![]
    }
}

struct BindingGeneratorCs {
    _try_format_code: bool,
}

impl uniffi_bindgen::BindingGenerator for BindingGeneratorCs {
    type Config = gen_cs::Config;

    fn write_bindings(
        &self,
        ci: ComponentInterface,
        config: Self::Config,
        out_dir: &Utf8Path,
    ) -> anyhow::Result<()> {
        let bindings_file = out_dir.join(format!("{}.cs", ci.namespace()));
        let mut f = File::create(&bindings_file)?;
        write!(f, "{}", generate_bindings(&config, &ci)?)?;

        // TODO: find a way to easily format standalone C# files
        // https://github.com/dotnet/format

        Ok(())
    }
}

pub fn main() {
    let cli = Cli::parse();
    uniffi_bindgen::generate_external_bindings(
        BindingGeneratorCs {
            _try_format_code: !cli.no_format,
        },
        &cli.udl_file,
        cli.config,
        cli.out_dir,
    )
    .unwrap();
}
