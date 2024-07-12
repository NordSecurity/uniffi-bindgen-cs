#!/bin/bash
set -euxo pipefail

GEN_DIR="dotnet-tests/UniffiCS/gen"

case $(uname | tr '[:upper:]' '[:lower:]') in
	linux*)
		LIB=libuniffi_fixtures.so
	;;
	darwin*)
		LIB=libuniffi_fixtures.dylib
	;;
	msys* | mingw64*)
		LIB=uniffi_fixtures.dll
	;;
	*)
		echo "Cannot find binary - unrecognized uname" >&2
		exit 1
	;;
esac

rm -rf "$GEN_DIR"
mkdir -p "$GEN_DIR"

target/debug/uniffi-bindgen-cs target/debug/libuniffi_fixtures.so --library --out-dir="$GEN_DIR" --no-format