#!/bin/bash
set -euxo pipefail

GEN_DIR="dotnet-tests/UniffiCS/gen"

rm -rf "$GEN_DIR"
mkdir -p "$GEN_DIR"

osname=$(uname)

libname=""

if [[ "$osname" == "Darwin" ]]; then
  libname="libuniffi_fixtures.dylib"
elif [[ "$osname" == "Linux" ]]; then
  libname="libuniffi_fixtures.so"
fi

target/debug/uniffi-bindgen-cs "target/debug/$libname" --library --out-dir="$GEN_DIR" --no-format
