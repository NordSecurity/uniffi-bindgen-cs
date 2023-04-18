# uniffi-bindgen-cs - UniFFI C# bindings generator

Generate [UniFFI](https://github.com/mozilla/uniffi-rs) bindings for C#. `uniffi-bindgen-cs` lives
as a separate project from `uniffi-rs`, as per
[uniffi-rs #1355](https://github.com/mozilla/uniffi-rs/issues/1355). Currently, `uniffi-bindgen-cs`
uses `uniffi-rs` version `0.20.0`.

# How to install

Minimum Rust version required to install `uniffi-bindgen-cs` is `1.58`.
Newer Rust versions should also work fine.

```
cargo install uniffi-bindgen-cs --git https://github.com/NordSecurity/uniffi-bindgen-cs
```

# How to generate bindings

```
uniffi-bindgen-cs path/to/definitions.udl
```
Generates bindings file `path/to/definitions.cs`

# How to integrate bindings

To integrate the bindings into your projects, simply add the generated bindings file to your project.
There are a couple of requirements to compile the generated bindings file:
- `dotnet` version `6.0` or higher
- allow `unsafe` code
    ```
    <PropertyGroup>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>
    ```

# Contributing

For contribution guidelines, read [CONTRIBUTING.md](CONTRIBUTING.md)

# Versioning

`uniffi-bindgen-cs` is versioned separately from `uniffi-rs`. UniFFI follows the [SemVer rules from
the Cargo Book](https://doc.rust-lang.org/cargo/reference/resolver.html#semver-compatibility)
which states "Versions are considered compatible if their left-most non-zero
major/minor/patch component is the same". A breaking change is any modification to the C# bindings
that demands the consumer of the bindings to make corresponding changes to their code to ensure that
the bindings continue to function properly. `uniffi-bindgen-cs` is young, and its unclear how stable
the generated bindings are going to be between versions. For this reason, major version is currently
0, and most changes are probably going to bump minor version.

To ensure consistent feature set across external binding generators, `uniffi-bindgen-cs` targets
a specific `uniffi-rs` version. A consumer using Go bindings (in `uniffi-bindgen-go`) and C#
bindings (in `uniffi-bindgen-cs`) expects the same features to be available across multiple bindings
generators. This means that the consumer should choose external binding generator versions such that
each generator targets the same `uniffi-rs` version. The table shows `uniffi-rs` version history
to make it easier to understand when `uniffi-rs` version has changed.

| uniffi-bindgen-cs version                | uniffi-rs version                                |
|------------------------------------------|--------------------------------------------------|
| v0.3.0                                   | 3142151e v0.24.0?                                |
| v0.2.0                                   | v0.23.0                                          |
| v0.1.0                                   | v0.20.0                                          |
