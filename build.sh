#!/bin/bash
set -euxo pipefail

cargo build --package uniffi-bindgen-cs --package uniffi-bindgen-cs-fixtures
