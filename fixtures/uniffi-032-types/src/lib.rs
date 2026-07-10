/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Types introduced by uniffi-rs 0.32.0:
//   - `HashSet<T>` (`Type::Set`), which travels through a `RustBuffer` like a sequence.
//   - `&[u8]` / `[ByRef] bytes` arguments, which travel as a borrowed `ForeignBytes`
//     (pointer + length) rather than an owned `RustBuffer`.

use std::collections::HashSet;

#[derive(Debug, thiserror::Error, uniffi::Error)]
pub enum ByteError {
    #[error("empty")]
    Empty,
}

#[uniffi::export]
pub fn identity_set(v: HashSet<String>) -> HashSet<String> {
    v
}

#[uniffi::export]
pub fn identity_int_set(v: HashSet<i32>) -> HashSet<i32> {
    v
}

#[uniffi::export]
pub fn set_contains(v: HashSet<String>, needle: String) -> bool {
    v.contains(&needle)
}

#[uniffi::export]
pub fn optional_set(v: Option<HashSet<u64>>) -> u64 {
    v.map(|s| s.iter().sum()).unwrap_or(0)
}

/// Borrowed bytes: Rust sees `&[u8]` pointing directly at the pinned C# array.
#[uniffi::export]
pub fn byref_bytes_len(v: &[u8]) -> u32 {
    v.len() as u32
}

#[uniffi::export]
pub fn byref_bytes_sum(v: &[u8]) -> u64 {
    v.iter().map(|b| *b as u64).sum()
}

/// Mixes a borrowed-bytes arg with ordinary args, before and after.
#[uniffi::export]
pub fn byref_bytes_mixed(prefix: String, v: &[u8], suffix: String) -> String {
    format!("{prefix}:{}:{suffix}", v.len())
}

/// Two borrowed-bytes args in one call, so both must stay pinned simultaneously.
#[uniffi::export]
pub fn byref_bytes_concat_len(a: &[u8], b: &[u8]) -> u32 {
    (a.len() + b.len()) as u32
}

/// Borrowed bytes on a fallible call, which takes the `RustCallWithError` path.
#[uniffi::export]
pub fn byref_bytes_first(v: &[u8]) -> Result<u8, ByteError> {
    v.first().copied().ok_or(ByteError::Empty)
}

/// Borrowed bytes returning unit, which takes the void `RustCallAction` path.
#[uniffi::export]
pub fn byref_bytes_ignore(_v: &[u8]) {}

#[derive(Default, uniffi::Object)]
pub struct BytesCounter {
    inner: std::sync::Mutex<u64>,
}

/// Borrowed bytes as a method argument, which takes the `thisPtr` prefix path.
#[uniffi::export]
impl BytesCounter {
    #[uniffi::constructor]
    pub fn new() -> Self {
        Self::default()
    }

    pub fn add(&self, v: &[u8]) -> u64 {
        let mut total = self.inner.lock().unwrap();
        *total += v.len() as u64;
        *total
    }
}

// Recursive enums (0.32) — `Box<T>` participates in the cycle and is transparent to bindings.
#[derive(Debug, PartialEq, uniffi::Enum)]
pub enum Tree {
    Leaf { value: i32 },
    Node { left: Box<Tree>, right: Box<Tree> },
}

#[uniffi::export]
pub fn identity_tree(t: Tree) -> Tree {
    t
}

#[uniffi::export]
pub fn sum_tree(t: Tree) -> i64 {
    match t {
        Tree::Leaf { value } => value as i64,
        Tree::Node { left, right } => sum_tree(*left) + sum_tree(*right),
    }
}

/// Excluded from the generated bindings via `exclude` in `uniffi.toml`.
#[uniffi::export]
pub fn excluded_function() -> u32 {
    42
}

uniffi::setup_scaffolding!();
