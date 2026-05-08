/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Tests for nested sequence types: sequence<sequence<T>>.
// These exercise the array_new_expr filter which ensures correct C# allocation
// syntax for jagged arrays (e.g. `new byte[length][]` not `new byte[][length]`).

#[uniffi::export]
pub fn identity_nested_bytes(v: Vec<Vec<u8>>) -> Vec<Vec<u8>> {
    v
}

#[uniffi::export]
pub fn identity_nested_strings(v: Vec<Vec<String>>) -> Vec<Vec<String>> {
    v
}

#[uniffi::export]
pub fn identity_nested_ints(v: Vec<Vec<i32>>) -> Vec<Vec<i32>> {
    v
}

#[uniffi::export]
pub fn make_grid(rows: u32, cols: u32) -> Vec<Vec<u32>> {
    (0..rows)
        .map(|r| (0..cols).map(|c| r * cols + c).collect())
        .collect()
}

uniffi::setup_scaffolding!();
