#!/bin/bash
set -euxo pipefail

cargo build --package uniffi-bindgen-cs --package uniffi-bindgen-cs-fixtures

GEN_DIR="dotnet-tests/UniffiCS/gen"

rm -rf "$GEN_DIR"
mkdir -p "$GEN_DIR"

target/debug/uniffi-bindgen-cs target/debug/libuniffi_fixtures.so --library --out-dir="$GEN_DIR"
