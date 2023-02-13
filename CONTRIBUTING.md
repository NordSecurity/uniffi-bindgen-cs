# How to submit changes

Pull requests are welcome!

# How to report issues/bugs?

Create an issue on Github, we will try to get back to you ASAP.

# Checkout the code

```
git clone https://github.com/NordSecurity/uniffi-bindgen-cs.git
cd uniffi-bindgen-cs
git submodule update --init --recursive
```

# Run tests

To run tests, `dotnet` installation is required. Unlike `uniffi-rs`, there is no integration with
`cargo test`. Tests are written using `xunit`.

- Build `uniffi-bindgen-cs` executable, and `libuniffi_fixtures.so` shared library.
    ```
    ./build.sh
    ```

- Generate test bindings using `uniffi-bindgen-cs`, and run `dotnet test` command.
    ```
    ./test_bindings.sh
    ```

# Run tests in Docker

Running tests in Docker containers is easier, because manual `dotnet` installation is not required.

```
./docker_build.sh
./docker_test_bindings.sh
```

# Directory structure

| Directory                                | Description                                      |
|------------------------------------------|--------------------------------------------------|
| 3rd-party/uniffi-rs/                     | fork of uniffi-rs, used for tests                |
| dotnet-tests/UniffiCS/gen/               | generated test bindings                          |
| dotnet-tests/UniffiCS.binding_tests/     | C# tests for bindings                            |
| fixtures/                                | additional test fixtures specific to C# bindings |
| src/gen_cs/                              | generator CLI code                               |
| templates/                               | generator C# templates                           |


# Thank you!
