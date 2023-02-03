#!/bin/bash
set -euxo pipefail

cargo build --bin uniffi-bindgen-cs
cargo build --lib --features uniffi_fixtures
