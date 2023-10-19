# Uprading upstream version

## C ABI

Check that C ABI still matches between C# and Rust code. This includes , ,
`FfiConverter*` types (`List`, `Map`, `Duration`, etc..), FFI types generated for
functions in `NamespaceLibraryTemplate.cs`.

- C# structs annotated with `StructLayout`.
    - [`RustBuffer`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi/rustbuffer.rs#L52)
    - [`RustCallStatus`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi/rustcalls.rs#L53)
    - [`ForeignBytes`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi/foreignbytes.rs#L26)

- C# delegates annotated with `UnmanagedFunctionPointer`.
    - [`ForeignCallback`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi/foreigncallbacks.rs#L143)

- `FfiConverter*` types.
    - [`CallbackInterfaceRuntime.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/lib.rs#L476)
    - [`DurationHelper.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi_converter_impls.rs#L298)
    - [`EnumTemplate.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_macros/src/enum_.rs#L78)
    - [`ErrorTemplate.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_macros/src/error.rs#L67)
    - [`MapTemplate.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi_converter_impls.rs#L395)
    - [`OptionalTemplate.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi_converter_impls.rs#L324)
    - [`SequenceTemplate.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi_converter_impls.rs#L360)
    - [`TimestampHelper.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_core/src/ffi_converter_impls.rs#L252)

- FFI types generated for functions in [`NamespaceLibraryTemplate.cs`](https://github.com/mozilla/uniffi-rs/blob/v0.24.3/uniffi_bindgen/src/scaffolding/mod.rs#L67).
