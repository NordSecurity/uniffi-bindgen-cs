# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.

FROM rust:1.58-bullseye as build
RUN cargo install --version 0.20.0 uniffi_bindgen

FROM debian:bullseye
COPY --from=build /usr/local/cargo/bin/uniffi-bindgen /bin/uniffi-bindgen
COPY target/debug/uniffi-bindgen-cs /bin/uniffi-bindgen-cs
