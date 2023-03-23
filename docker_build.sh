#!/bin/bash
set -euxo pipefail

docker run \
    -ti --rm \
    --volume $HOME/.cargo/registry:/usr/local/cargo/registry \
    --volume $PWD:/mounted_workdir \
    --workdir /mounted_workdir \
    rust:1.64 ./build.sh
