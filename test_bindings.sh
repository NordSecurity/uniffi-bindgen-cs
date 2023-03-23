#!/bin/bash
set -euxo pipefail

SCRIPT_DIR="${SCRIPT_DIR:-$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )}"

CSPROJ_DIR="dotnet-tests/UniffiCS.binding_tests"
GEN_DIR="dotnet-tests/UniffiCS/gen"

rm -rf "$GEN_DIR"
mkdir -p "$GEN_DIR"
function bindings() {
    target/debug/uniffi-bindgen-cs $1 --out-dir="$GEN_DIR" --config="uniffi-test-fixtures.toml"
}

bindings 3rd-party/uniffi-rs/examples/arithmetic/src/arithmetic.udl
bindings 3rd-party/uniffi-rs/examples/callbacks/src/callbacks.udl
bindings 3rd-party/uniffi-rs/examples/custom-types/src/custom-types.udl
bindings 3rd-party/uniffi-rs/examples/geometry/src/geometry.udl
bindings 3rd-party/uniffi-rs/examples/rondpoint/src/rondpoint.udl
bindings 3rd-party/uniffi-rs/examples/sprites/src/sprites.udl
bindings 3rd-party/uniffi-rs/examples/todolist/src/todolist.udl
bindings 3rd-party/uniffi-rs/fixtures/callbacks/src/callbacks.udl
bindings 3rd-party/uniffi-rs/fixtures/coverall/src/coverall.udl
bindings 3rd-party/uniffi-rs/fixtures/external-types/lib/src/external-types-lib.udl
bindings 3rd-party/uniffi-rs/fixtures/uniffi-fixture-time/src/chronological.udl
bindings fixtures/disposable/src/disposable.udl

export LD_LIBRARY_PATH="$SCRIPT_DIR/target/debug/:${LD_LIBRARY_PATH:-}"
cd $CSPROJ_DIR
dotnet test -l "console;verbosity=normal"
