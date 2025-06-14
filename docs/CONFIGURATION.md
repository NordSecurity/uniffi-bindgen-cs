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
    global_methods_class_name = "LibGreeter"
    ```

- `access_modifier` - override the default `internal` access modifier for "exported" uniffi symbols.

- `null_string_to_empty` - when set to `true`, `null` strings will be converted to empty strings even if they are not optional.
