/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use std::sync::Arc;

// CS0542 regression: object "Monitor" with method "monitor()" — without the
// method_name filter, the generated class Monitor would declare `void Monitor()`
// which is a CS0542 error (member name same as enclosing type).
#[derive(uniffi::Object)]
pub struct Monitor {}

#[uniffi::export]
impl Monitor {
    #[uniffi::constructor]
    pub fn new() -> Arc<Monitor> {
        Arc::new(Monitor {})
    }

    pub fn monitor(&self) {}
}

// Jagged-array regression: sequence<sequence<u8>> — without the array_new_expr
// filter, the generated code would emit `new byte[][length]` which is a C# syntax
// error. The fix emits `new byte[length][]`.
#[uniffi::export]
pub fn make_nested_bytes(rows: u32, cols: u32) -> Vec<Vec<u8>> {
    (0..rows).map(|_| vec![0u8; cols as usize]).collect()
}

#[uniffi::export]
pub fn sum_nested_bytes(matrix: Vec<Vec<u8>>) -> u32 {
    matrix.iter().flatten().map(|&b| b as u32).sum()
}

uniffi::setup_scaffolding!();
