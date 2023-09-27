# uniffi-bindgen-cs - UniFFI C# bindings generator

Generate [UniFFI](https://github.com/mozilla/uniffi-rs) bindings for C#. `uniffi-bindgen-cs` lives
as a separate project from `uniffi-rs`, as per
[uniffi-rs #1355](https://github.com/mozilla/uniffi-rs/issues/1355). Currently, `uniffi-bindgen-cs`
uses `uniffi-rs` version `0.20.0`.

# How to install

Minimum Rust version required to install `uniffi-bindgen-cs` is `1.64`.
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
- .NET core `6.0` or higher
- allow `unsafe` code
- allow `Nullable`

```
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

# Configuration options

It's possible to configure some settings by passing `--config` argument to the generator.
```
uniffi-bindgen-cs path/to/definitions.udl --config path/to/uniffi.toml
```

- `package_name` - deprecated, use `namespace`.

- `cdylib_name` - override the dynamic library name linked by generated bindings, excluding `lib`
    prefix and `.dll` file extension. E.g. for `libgreeter.dll`, use `greeter`.

- `custom_types` - properties for custom type defined in UDL with `[Custom] typedef string Url;`.

    - `imports` - any imports required to satisfy this type. E.g. `imports = ["System"]`.

    - `type_name` - the name to represent the type in generated bindings.

    - `into_custom` - an expression to convert from the underlying type into custom type. `{}` will
        will be expanded into variable containing the underlying value. The expression is used in a
        return statement, i.e. `return <expression(value)>;`. E.g. `new Uri({})`.

    - `from_custom` - an expression to convert from the custom type into underlying type. `{}` will
        will be expanded into variable containing the custom value. The expression is used in a
        return statement, i.e. `return <expression(value);>`. E.g. `{}.AbsoluteUri`.

- `namespace` - override the `namespace ..;` declaration in generated bindings file. The default is
    `uniffi.{{namespace}}`, where `namespace` is the namespace from UDL file.

- `global_methods_class_name` - override the class name containing top level functions. The default
    is `{{namespace}}Methods`, where `namespace` is the namespace from UDL file.

# Contributing

For contribution guidelines, read [CONTRIBUTING.md](CONTRIBUTING.md)

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
each generator targets the same `uniffi-rs` version. The table shows `uniffi-rs` version history
to make it easier to understand when `uniffi-rs` version has changed.

| uniffi-bindgen-cs version                | uniffi-rs version                                |
|------------------------------------------|--------------------------------------------------|
| v0.2.0                                   | v0.23.0                                          |
| v0.1.0                                   | v0.20.0                                          |
