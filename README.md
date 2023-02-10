# uniffi-bindgen-cs - UniFFI C# bindings generator

Generate [UniFFI](https://github.com/mozilla/uniffi-rs) bindings for C#. `uniffi-bindgen-cs` lives
as a separate project from `uniffi-rs`, as per
[uniffi-rs #1355](https://github.com/mozilla/uniffi-rs/issues/1355). Currently, `uniffi-bindgen-cs`
uses `uniffi-rs` version `0.20.0`.

# How to install

Minimum Rust version required to install `uniffi-bindgen-cs` is `1.58`.
Newer Rust versions should also work fine.

```
cargo install -e https://github.com/NordSecurity/uniffi-bindgen-cs
```

# How to generate bindings

```
uniffi-bindgen-cs path/to/definitions.udl
```
Generates bindings file `path/to/definitions.cs`

# How to integrate bindings

To integrate the bindings into your projects, simply add the generated binding file to your project.
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
