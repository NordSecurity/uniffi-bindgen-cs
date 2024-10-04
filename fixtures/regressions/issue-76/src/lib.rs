/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#[derive(Debug, thiserror::Error, uniffi::Error)]
pub enum Error {
    #[error("Example")]
    Example,
}

#[uniffi::export]
pub fn always_error() -> Result<(), Error> {
    Err(Error::Example)
}

uniffi::setup_scaffolding!();
