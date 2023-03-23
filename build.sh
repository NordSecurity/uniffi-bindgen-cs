#!/bin/bash
set -euxo pipefail

cargo build --bin uniffi-bindgen-cs --lib --features uniffi_fixtures
