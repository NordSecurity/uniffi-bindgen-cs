#!/bin/bash
set -euxo pipefail

docker run \
    -ti --rm \
    --volume $HOME/.cargo/registry:/usr/local/cargo/registry \
    --volume $HOME/.nuget/packages:/root/.nuget/packages \
    --volume $PWD:/mounted_workdir \
    --workdir /mounted_workdir \
    mcr.microsoft.com/dotnet/sdk:6.0 ./test_bindings.sh
