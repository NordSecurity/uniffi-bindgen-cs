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

target/debug/uniffi-bindgen-cs target/debug/$LIB --library --out-dir="$GEN_DIR" --no-format

# With `--udl-dir`, library mode must not need `cargo metadata`, so generate a UDL fixture from
# outside the workspace and compare it with the bindings generated above.
ROOT=$(pwd)
UDL_DIR_OUT=$(mktemp -d)
printf '[bindings.csharp]\ncdylib_name = "uniffi_fixtures"\n' > "$UDL_DIR_OUT/uniffi.toml"
(
	cd "$UDL_DIR_OUT"
	"$ROOT/target/debug/uniffi-bindgen-cs" "$ROOT/target/debug/${LIB/uniffi_fixtures/issue_28}" \
		--library --udl-dir="$ROOT/fixtures/regressions/issue-28/src" --config=uniffi.toml --out-dir=. --no-format
)
diff "$GEN_DIR/issue_28.cs" "$UDL_DIR_OUT/issue_28.cs"
rm -rf "$UDL_DIR_OUT"
