#!/bin/bash
set -euxo pipefail

SOLUTION_DIR="dotnet-tests"
dotnet test -l "console;verbosity=normal" $SOLUTION_DIR
