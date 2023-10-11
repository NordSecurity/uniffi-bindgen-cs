# uniffi-bindgen-cs - UniFFI C# bindings generator

Generate [UniFFI](https://github.com/mozilla/uniffi-rs) bindings for C#. `uniffi-bindgen-cs` lives
as a separate project from `uniffi-rs`, as per
[uniffi-rs #1355](https://github.com/mozilla/uniffi-rs/issues/1355). Currently, `uniffi-bindgen-cs`
uses `uniffi-rs` version `0.20.0`.

# How to install

Minimum Rust version required to install `uniffi-bindgen-cs` is `1.64`.
Newer Rust versions should also work fine.

```bash
cargo install uniffi-bindgen-cs --git https://github.com/NordSecurity/uniffi-bindgen-cs --tag v0.2.4+v0.23.0
```

# How to generate bindings

```bash
uniffi-bindgen-cs path/to/definitions.udl
```
Generates bindings file `path/to/definitions.cs`

# How to integrate bindings

To integrate the bindings into your projects, simply add the generated bindings file to your project.
There are a couple of requirements to compile the generated bindings file:
- .NET core `6.0` or higher
- allow `unsafe` code
- allow `Nullable`

```xml
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

# Configuration options

It's possible to configure some settings by passing `--config` argument to the generator. All
configuration keys are defined in `bindings.csharp` section.
```bash
uniffi-bindgen-cs path/to/definitions.udl --config path/to/uniffi.toml
```

- `cdylib_name` - override the dynamic library name linked by generated bindings, excluding `lib`
    prefix and `.dll` file extension. When using `--library` mode, defaults to library's name.
    In standalone mode this value is required, and error will be produced if its missing.
    ```toml
    # For library `libgreeter.dll`
    [bindings.csharp]
    cdylib_name = "greeter"
    ```

- `custom_types` - properties for custom type defined in UDL with `[Custom] typedef string Url;`.
    ```toml
    # Represent URL as a C# native `Uri` class. The underlying type of URL is a string.
    [bindings.csharp.custom_types.Url]
    imports = ["System"]
    type_name = "Uri"
    into_custom = "new Uri({})"
    from_custom = "{}.AbsoluteUri"
    ```

    - `imports` (optional) - any imports required to satisfy this type.

    - `type_name` (optional) - the name to represent the type in generated bindings. Default is the
        type alias name from UDL, e.g. `Url`.

    - `into_custom` (required) - an expression to convert from the underlying type into custom type. `{}` will
        will be expanded into variable containing the underlying value. The expression is used in a
        return statement, i.e. `return <expression(value)>;`.

    - `from_custom` (required) - an expression to convert from the custom type into underlying type. `{}` will
        will be expanded into variable containing the custom value. The expression is used in a
        return statement, i.e. `return <expression(value);>`.

- `namespace` - override the `namespace ..;` declaration in generated bindings file. The default is
    `uniffi.{{namespace}}`, where `namespace` is the namespace from UDL file.
    ```toml
    # emits `namespace com.example.greeter;` in generated bindings file
    [bindings.csharp]
    namespace = "com.example.greeter"
    ```

- `global_methods_class_name` - override the class name containing top level functions. The default
    is `{{namespace}}Methods`, where `namespace` is the namespace from UDL file.
    ```toml
    # emits `public static class LibGreeter { .. }` in generated bindings file
    [bindings.csharp]
    namespace = "LibGreeter"
    ```

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
each generator targets the same `uniffi-rs` version.

To simplify this choice `uniffi-bindgen-cs` and `uniffi-bindgen-go` use tag naming convention
as follows: `vX.Y.Z+vA.B.C`, where `X.Y.Z` is the version of the generator itself, and `A.B.C` is
the version of uniffi-rs it is based on.

The table shows `uniffi-rs` version history for tags that were published before tag naming convention described above was introduced.

| uniffi-bindgen-cs version                | uniffi-rs version                                |
|------------------------------------------|--------------------------------------------------|
| ~~v0.3.0~~ (DONT USE, UNFINISHED)        | ~~3142151e v0.24.0?~~                            |
| v0.2.0                                   | v0.23.0                                          |
| v0.1.0                                   | v0.20.0                                          |

### v0.3.0

This is a version somewhere between 0.23.0 and 0.24.0. This was supposed to be a temporary stepping
stone for the actual 0.24.0 version, but ended up never being actually used (at least by us). It
is reverted in main branch. Use v0.2.0 instead.