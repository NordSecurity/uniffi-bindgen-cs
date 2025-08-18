#!/bin/bash
set -euxo pipefail

docker run \
    -ti --rm \
    --volume $PWD:/mounted_workdir \
    --workdir /mounted_workdir \
    ghcr.io/nordsecurity/uniffi-bindgen-cs-test-runner:v0.2.1 bash
