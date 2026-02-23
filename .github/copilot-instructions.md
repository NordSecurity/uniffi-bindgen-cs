# Copilot Instructions for uniffi-bindgen-cs

## What This Project Is

A UniFFI C# bindings generator — it takes UniFFI component definitions (UDL files or proc-macro libraries) and generates C# code that calls into Rust shared libraries via FFI. It is a standalone project, separate from the main `uniffi-rs` repository.

## Build, Generate, and Test

Requires Rust 1.88+ and `dotnet`. Three scripts run sequentially:

```bash
./build.sh              # cargo build of bindgen CLI + fixture shared library
./generate_bindings.sh  # runs the built CLI to generate C# bindings into dotnet-tests/UniffiCS/gen/
./test_bindings.sh      # dotnet test on the solution in dotnet-tests/
```

Or use `make all` to run all three. There is no `cargo test` integration.

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

1. **Input**: UniFFI component interface (UDL or proc-macro library `.so`/`.dylib`/`.dll`)
2. **Parsing**: `uniffi_bindgen` / `uniffi_udl` crates parse the interface into a `ComponentInterface`
3. **Rendering**: Askama templates in `bindgen/templates/` produce C# source code
4. **Output**: A single generated `.cs` file per component

### Key Directories

- `bindgen/src/gen_cs/` — Rust modules implementing the `CodeType` trait for each UniFFI type (primitives, records, enums, objects, callbacks, etc.). `mod.rs` is the orchestrator.
- `bindgen/templates/` — Askama (`.cs`) templates that define the generated C# code structure. Each template corresponds to a UniFFI type or helper construct.
- `fixtures/` — Rust crates that define test fixture libraries with UniFFI interfaces. These get compiled into a single shared library (`uniffi_fixtures`) used for testing.
- `dotnet-tests/UniffiCS.BindingTests/` — xUnit test files that exercise the generated C# bindings against the fixture library.

### How Types Flow Through the Generator

Each UniFFI type has a corresponding Rust module in `bindgen/src/gen_cs/` that implements the `CodeType` trait (defining `type_label`, `canonical_name`, `ffi_converter_name`, `literal`). The module's Askama template in `bindgen/templates/` renders the C# FFI converter and helper code. Filters in `filters.rs` and formatting in `formatting.rs` bridge the Rust code generation with template rendering.

## Key Conventions

- **Naming**: C# output uses `UpperCamelCase` for types/methods and `lowerCamelCase` for parameters — conversions handled via `heck` crate in the Rust generator.
- **Configuration**: Generator behavior is configurable via TOML files passed with `--config`. Options are under `[bindings.csharp]` — see `docs/CONFIGURATION.md` for `cdylib_name`, `custom_types`, `namespace`, `access_modifier`, etc.
- **Versioning**: Tags follow `vX.Y.Z+vA.B.C` where `X.Y.Z` is the generator version and `A.B.C` is the targeted `uniffi-rs` version. Both `bindgen/Cargo.toml` and `README.md` must be updated on release.
- **DCO sign-off**: All commits require `git commit -s` (DCO sign-off enforced).
- **Adding a new fixture**: Create a Rust crate in `fixtures/`, add it to `fixtures/Cargo.toml` workspace, write corresponding tests in `dotnet-tests/UniffiCS.BindingTests/`.
- **Upgrading uniffi-rs**: Follow `docs/VERSION_UPGRADE.md` — verify C ABI compatibility of `RustBuffer`, `RustCallStatus`, `ForeignBytes`, and FFI converters between the Rust and C# sides.
