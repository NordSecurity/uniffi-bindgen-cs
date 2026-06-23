# AGENTS.md

Canonical guidance for AI coding agents working in this repository. This is the single
source of truth; `CLAUDE.md` and `.github/copilot-instructions.md` point here.

## What This Project Is

A UniFFI C# bindings generator. It takes UniFFI component definitions (UDL files or proc-macro
libraries) and generates C# code that calls into Rust shared libraries via FFI. Standalone from the
main `uniffi-rs` repository.

Current version: `0.11.0+v0.31.0` (generator v0.11, targeting uniffi-rs v0.31.0). Requires Rust 1.88+.

## Build, Generate, and Test

There is no `cargo test` integration. Tests are xUnit v3 tests (running on Microsoft.Testing.Platform)
that execute against generated bindings.

```bash
# Full pipeline (build → generate → test)
make all

# Individual steps
./build.sh              # cargo build of bindgen CLI + fixture shared library
./generate_bindings.sh  # generate C# bindings into dotnet-tests/UniffiCS/gen/
./test_bindings.sh      # dotnet test on dotnet-tests/
```

### Test stack

- Tests run on **xUnit v3** via **Microsoft.Testing.Platform (MTP)** — `dotnet-tests/global.json`
  sets `"runner": "Microsoft.Testing.Platform"`, and `UniffiCS.BindingTests.csproj` references
  `xunit.v3.mtp-v2` with `<OutputType>Exe</OutputType>` and
  `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`.
- `UniffiCS.BindingTests` targets **net9.0** (downgraded from net10.0 in commit 6395dea until the CI
  image is updated).
- The generated bindings library `UniffiCS` dual-targets **netstandard2.0** + **net8.0**.

### Run a single test

xUnit v3 on MTP uses framework-specific filter flags (not the VSTest `--filter` expression). Pass MTP
arguments after `--`:

```bash
dotnet test dotnet-tests -- --filter-class "*TestArithmetic*"
```

Other MTP filter flags: `--filter-method`, `--filter-namespace`, `--filter-trait "name=value"`, and
their `--filter-not-*` negations; `--filter-query` for the path-segment query language.

> Note: on the .NET 10 SDK the legacy VSTest bridge that `dotnet test` used is removed, so the command
> above requires opting into the new dotnet-test experience (or running against a .NET 9 SDK, which CI
> uses). The test project is an MTP executable, so you can also run the built binary directly with the
> same filter flags, e.g.
> `dotnet dotnet-tests/UniffiCS.BindingTests/bin/Debug/net9.0/UniffiCS.BindingTests.dll --filter-class "*TestArithmetic*"`.

### Docker alternative (no local `dotnet` needed)

`./docker.sh` drops you into an interactive shell in the
`ghcr.io/nordsecurity/uniffi-bindgen-cs-test-runner:v0.2.1` container with the repo mounted at
`/mounted_workdir`. From that shell, run the pipeline:

```bash
./docker.sh        # opens the container shell
# then, inside the container:
make all
```

## Architecture

### Code Generation Pipeline

```
UDL / compiled library → [uniffi_bindgen parses → ComponentInterface] → [gen_cs + Askama templates] → single .cs file per component
```

### Key Directories

- **`bindgen/src/gen_cs/`** — Rust modules implementing the `CodeType` trait for each UniFFI type.
  `mod.rs` is the orchestrator containing `CsWrapper`, `TypeRenderer`, `CsCodeOracle`, and `Config`.
- **`bindgen/templates/`** — Askama templates (`.cs` files) that produce the generated C# code. Each
  template maps to a UniFFI type or helper construct. Template syntax is configured in
  `bindgen/askama.toml`.
- **`bindgen/src/gen_cs/filters.rs`** — Template filters bridging Rust codegen with Askama rendering
  (`type_name`, `ffi_converter_name`, `lower_fn`, `lift_fn`, etc.).
- **`fixtures/`** — Rust crates defining test fixture libraries with UniFFI interfaces, compiled into a
  single shared library (`uniffi_fixtures`).
- **`dotnet-tests/UniffiCS.BindingTests/`** — xUnit tests exercising generated bindings against the
  fixture library.

### How Types Flow Through the Generator

Each UniFFI type has a Rust module in `gen_cs/` implementing the `CodeType` trait (`type_label`,
`canonical_name`, `ffi_converter_name`, `literal`). The corresponding Askama template in
`bindgen/templates/` renders the C# FFI converter and helper code. The main wrapper template
(`wrapper.cs`) and type dispatcher (`Types.cs`) orchestrate the output.

### C# Output Targets

Generated code supports both .NET Framework 4.6.1+ and .NET 8.0+, with conditional compilation
(`NET8_0_OR_GREATER`) for platform differences like `LibraryImport` vs `DllImport`.

## Key Conventions

- **Naming**: C# output uses `UpperCamelCase` for types/methods, `lowerCamelCase` for parameters.
  Conversions via the `heck` crate.
- **Configuration**: TOML under `[bindings.csharp]` — see `docs/CONFIGURATION.md` for `cdylib_name`,
  `custom_types`, `namespace`, `access_modifier`, `rename`, etc.
- **DCO sign-off**: All commits require `git commit -s`.
- **Adding a fixture**: Create a Rust crate in `fixtures/`, add it to the `fixtures/Cargo.toml`
  workspace, and write tests in `dotnet-tests/UniffiCS.BindingTests/`.
- **Upgrading uniffi-rs**: Follow `docs/VERSION_UPGRADE.md` — verify C ABI compatibility of
  `RustBuffer`, `RustCallStatus`, `ForeignBytes`, and FFI converters.
- **Versioning**: Tags use `vX.Y.Z+vA.B.C` format. Update `bindgen/Cargo.toml`, `README.md`, and
  `CHANGELOG.md` on release (see `docs/RELEASE.md`).
- **Debugging hanging tests**: Set `diagnosticMessages` to `true` in
  `dotnet-tests/UniffiCS.BindingTests/xunit.runner.json`.
