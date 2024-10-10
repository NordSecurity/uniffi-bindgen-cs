#!/bin/bash
set -euxo pipefail

SCRIPT_DIR="${SCRIPT_DIR:-$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )}"

SOLUTION_DIR="dotnet-tests"

export LD_LIBRARY_PATH="$SCRIPT_DIR/target/debug/:${LD_LIBRARY_PATH:-}"

if [[ $(uname) == "Darwin" ]]; then
  export DYLD_LIBRARY_PATH="$SCRIPT_DIR/target/debug/:${DYLD_LIBRARY_PATH:-}"
fi

cd $SOLUTION_DIR
dotnet test -l "console;verbosity=normal"
