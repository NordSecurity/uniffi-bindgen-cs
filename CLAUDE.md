# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

A UniFFI C# bindings generator. It takes UniFFI component definitions (UDL files or proc-macro libraries) and generates C# code that calls into Rust shared libraries via FFI. Standalone from the main `uniffi-rs` repository.

Current version: `0.11.0+v0.31.0` (generator v0.11, targeting uniffi-rs v0.31.0). Requires Rust 1.88+.

## Build, Generate, and Test

There is no `cargo test` integration. Tests are C# xUnit tests that run against generated bindings.

```bash
# Full pipeline (build → generate → test)
make all

# Individual steps
./build.sh              # cargo build of bindgen CLI + fixture shared library
./generate_bindings.sh  # generate C# bindings into dotnet-tests/UniffiCS/gen/
./test_bindings.sh      # dotnet test on dotnet-tests/
```

Run a single test by name:
```bash
dotnet test dotnet-tests -l "console;verbosity=normal" --filter "FullyQualifiedName~TestArithmetic"
```

Docker alternative (no local `dotnet` needed):
```bash
./docker_build.sh && ./docker_test_bindings.sh
```

## Architecture

### Code Generation Pipeline

```
UDL / compiled library → [uniffi_bindgen parses → ComponentInterface] → [gen_cs + Askama templates] → single .cs file per component
```

### Key Directories

- **`bindgen/src/gen_cs/`** — Rust modules implementing the `CodeType` trait for each UniFFI type. `mod.rs` is the orchestrator containing `CsWrapper`, `TypeRenderer`, `CsCodeOracle`, and `Config`.
- **`bindgen/templates/`** — Askama templates (`.cs` files) that produce the generated C# code. Each template maps to a UniFFI type or helper construct. Template syntax is configured in `bindgen/askama.toml`.
- **`bindgen/src/gen_cs/filters.rs`** — Template filters bridging Rust codegen with Askama rendering (`type_name`, `ffi_converter_name`, `lower_fn`, `lift_fn`, etc.).
- **`fixtures/`** — Rust crates defining test fixture libraries with UniFFI interfaces, compiled into a single shared library (`uniffi_fixtures`).
- **`dotnet-tests/UniffiCS.BindingTests/`** — xUnit tests exercising generated bindings against the fixture library.

### How Types Flow Through the Generator

Each UniFFI type has a Rust module in `gen_cs/` implementing the `CodeType` trait (`type_label`, `canonical_name`, `ffi_converter_name`, `literal`). The corresponding Askama template in `bindgen/templates/` renders the C# FFI converter and helper code. The main wrapper template (`wrapper.cs`) and type dispatcher (`Types.cs`) orchestrate the output.

### C# Output Targets

Generated code supports both .NET Framework 4.6.1+ and .NET 8.0+, with conditional compilation (`NET8_0_OR_GREATER`) for platform differences like `LibraryImport` vs `DllImport`.

## Key Conventions

- **Naming**: C# output uses `UpperCamelCase` for types/methods, `lowerCamelCase` for parameters. Conversions via `heck` crate.
- **Configuration**: TOML under `[bindings.csharp]` — see `docs/CONFIGURATION.md` for `cdylib_name`, `custom_types`, `namespace`, `access_modifier`, `rename`, etc.
- **DCO sign-off**: All commits require `git commit -s`.
- **Adding a fixture**: Create Rust crate in `fixtures/`, add to `fixtures/Cargo.toml` workspace, write tests in `dotnet-tests/UniffiCS.BindingTests/`.
- **Upgrading uniffi-rs**: Follow `docs/VERSION_UPGRADE.md` — verify C ABI compatibility of `RustBuffer`, `RustCallStatus`, `ForeignBytes`, and FFI converters.
- **Versioning**: Tags use `vX.Y.Z+vA.B.C` format. Update `bindgen/Cargo.toml`, `README.md`, and `CHANGELOG.md` on release (see `docs/RELEASE.md`).
- **Debugging hanging tests**: Set `diagnosticMessages` to `true` in `dotnet-tests/UniffiCS.BindingTests/xunit.runner.json`.
