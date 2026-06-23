/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Regression for CS8866 / enum_field_name collision:
// When a variant's field name PascalCases to the same name as the variant record
// class (e.g. variant "Insert" with field "insert"), the generated record used to
// declare `string Insert` inside `record Insert(...)` which is CS8866.
// The fix renames the property to "InsertValue".
//
// Also tests that Dispose() on enum variants with object-reference fields correctly
// references the renamed property ("InsertValue", not "Insert").

#[derive(uniffi::Enum)]
pub enum Change {
    Insert { insert: String },
    Delete { delete: String },
    Move { from: u32, to: u32 },
}

#[uniffi::export]
pub fn make_insert(s: String) -> Change {
    Change::Insert { insert: s }
}

#[uniffi::export]
pub fn make_delete(s: String) -> Change {
    Change::Delete { delete: s }
}

#[uniffi::export]
pub fn get_insert_value(c: Change) -> Option<String> {
    match c {
        Change::Insert { insert } => Some(insert),
        _ => None,
    }
}

uniffi::setup_scaffolding!();
