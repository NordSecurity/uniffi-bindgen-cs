# uniffi-bindgen-cs - UniFFI C# bindings generator

Generate [UniFFI](https://github.com/mozilla/uniffi-rs) bindings for C#. `uniffi-bindgen-cs` lives as a separate project from `uniffi-rs`, as per [uniffi-rs #1355](https://github.com/mozilla/uniffi-rs/issues/1355). Currently, `uniffi-bindgen-cs` uses `uniffi-rs` version `0.20.0`.

# How to install

```
cargo install -e https://github.com/NordSecurity/uniffi-bindgen-cs
```

# How to use

```
uniffi-bindgen-cs path/to/definitions.udl
# generates `path/to/definitions.cs`
```

# Directory structure

```
3rd-party/uniffi-rs/ - fork of uniffi-rs
dotnet-tests/UniffiCS/gen/ - generated test bindings
dotnet-tests/UniffiCS.binding_tests/ - C# tests for bindings
fixtures/ - additional test fixtures specific to C# bindings
src/gen_cs/ - generator CLI code
templates/ - generator C# templates
```

# How to build

Minimum Rust version required to build `uniffi-bindgen-cs` is `1.58`.
Newer Rust versions should also work fine.
```
cd uniffi-bindgen-cs
git submodule update --init --recursive
cargo build
file target/debug/uniffi-bindgen-cs
```

Build in Docker.
```
./docker_build.sh
```

# How to run tests

To run tests, `dotnet` installation is required. [Docker container from Microsoft](mcr.microsoft.com/dotnet/sdk:6.0) can be used to run tests. Unlike `uniffi-rs`, there is no integration with `cargo test`. Tests are written using `xunit`.

```
cd uniffi-bindgen-cs
# Build `uniffi-bindgen-cs` executable, and `libuniffi_fixtures.so`
./build.sh
# Generate test bindings using `uniffi-bindgen-cs`, and run `dotnet test` command
./test_bindings.sh
```

Run tests in Docker.
```
./docker_build.sh
./docker_test_bindings.sh
```