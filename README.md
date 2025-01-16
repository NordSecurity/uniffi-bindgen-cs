# uniffi-bindgen-cs - UniFFI C# bindings generator

Generate [UniFFI](https://github.com/mozilla/uniffi-rs) bindings for C#. `uniffi-bindgen-cs` lives
as a separate project from `uniffi-rs`, as per
[uniffi-rs #1355](https://github.com/mozilla/uniffi-rs/issues/1355).

# How to install

Minimum Rust version required to install `uniffi-bindgen-cs` is `1.72`.
Newer Rust versions should also work fine.

```bash
cargo install uniffi-bindgen-cs --git https://github.com/NordSecurity/uniffi-bindgen-cs --tag v0.8.3+v0.25.0
```

# How to generate bindings

```bash
uniffi-bindgen-cs path/to/definitions.udl
```
Generates bindings file `path/to/definitions.cs`

# How to integrate bindings

To integrate the bindings into your projects, simply add the generated bindings file to your project.
There are a few requirements depending on your target framework version.

- .NET core `6.0` or higher
    ```xml
    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>
    ```

- .NET framework `4.6.1`
    ```xml
    <PropertyGroup>
        <TargetFramework>net461</TargetFramework>
        <LangVersion>10.0</LangVersion>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <PackageReference Include="IsExternalInit" Version="1.0.3"/>
        <PackageReference Include="Microsoft.CSharp" Version="4.7.0" />
    </PropertyGroup>
    ```

# Unsupported features

The following uniffi features are unsupported.

- External types [#40](https://github.com/NordSecurity/uniffi-bindgen-cs/issues/40)

- Async functions [#41](https://github.com/NordSecurity/uniffi-bindgen-cs/issues/41)

# Known Limitations

The following valid Rust type definitions fail to be converted properly, but a simple work-around is given:

- Enum variants having the same name as their member type [#60](https://github.com/NordSecurity/uniffi-bindgen-cs/issues/60)

# Configuration options

It's possible to [configure some settings](docs/CONFIGURATION.md) by passing `--config`
argument to the generator.
```bash
uniffi-bindgen-cs path/to/definitions.udl --config path/to/uniffi.toml
```

# Versioning

`uniffi-bindgen-cs` is versioned separately from `uniffi-rs`. UniFFI follows the [SemVer rules from
the Cargo Book](https://doc.rust-lang.org/cargo/reference/resolver.html#semver-compatibility)
which states "Versions are considered compatible if their left-most non-zero
major/minor/patch component is the same". A breaking change is any modification to the C# bindings
that demands the consumer of the bindings to make corresponding changes to their code to ensure that
the bindings continue to function properly. `uniffi-bindgen-cs` is young, and it's unclear how stable
the generated bindings are going to be between versions. For this reason, major version is currently
0, and most changes are probably going to bump minor version.

To ensure consistent feature set across external binding generators, `uniffi-bindgen-cs` targets
a specific `uniffi-rs` version. A consumer using Go bindings (in `uniffi-bindgen-go`) and C#
bindings (in `uniffi-bindgen-cs`) expects the same features to be available across multiple bindings
generators. This means that the consumer should choose external binding generator versions such that
each generator targets the same `uniffi-rs` version.

To simplify this choice `uniffi-bindgen-cs` and `uniffi-bindgen-go` use tag naming convention
as follows: `vX.Y.Z+vA.B.C`, where `X.Y.Z` is the version of the generator itself, and `A.B.C` is
the version of uniffi-rs it is based on.

The table shows `uniffi-rs` version history for tags that were published before tag naming convention described above was introduced.

| uniffi-bindgen-cs version                | uniffi-rs version                                |
|------------------------------------------|--------------------------------------------------|
| v0.6.0                                   | v0.25.0                                          |
| v0.5.0                                   | v0.24.0                                          |
| ~~v0.3.0~~ (DONT USE, UNFINISHED)        | ~~3142151e v0.24.0?~~                            |
| v0.2.0                                   | v0.23.0                                          |
| v0.1.0                                   | v0.20.0                                          |

# Documentation

More documentation is available in [docs](docs) directory.

# Contributing

For contribution guidelines, read [CONTRIBUTING.md](CONTRIBUTING.md).
