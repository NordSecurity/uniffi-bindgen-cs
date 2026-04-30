#!/bin/bash
set -euxo pipefail

SOLUTION_DIR="dotnet-tests"
dotnet test $SOLUTION_DIR
