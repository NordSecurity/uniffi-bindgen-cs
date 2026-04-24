/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use std::sync::Arc;

/// A simple object used as a field in enum variants, forcing `contains_object_references = true`
/// and therefore Dispose() generation for the enum.
#[derive(uniffi::Object)]
pub struct Resource {
    pub name: String,
}

#[uniffi::export]
impl Resource {
    #[uniffi::constructor]
    pub fn new(name: String) -> Arc<Self> {
        Arc::new(Resource { name })
    }

    pub fn get_name(&self) -> String {
        self.name.clone()
    }
}

/// An enum that mirrors the loro-ffi `ListDiffItem` pattern: each variant has a field
/// whose name (after PascalCase conversion) collides with the variant class name.
/// - Insert { insert: Arc<Resource> }  →  property InsertValue (not Insert, to avoid conflict)
/// - Delete { delete: u32 }            →  property DeleteValue (not Delete, to avoid conflict)
/// - Retain { retain: u32 }            →  property RetainValue (not Retain, to avoid conflict)
///
/// When `contains_object_references` is true, a Dispose() method is generated that
/// must reference these renamed properties. Previously, destroy_fields used plain
/// property_name without applying enum_field_name, causing CS0572/CS0119 errors.
#[derive(uniffi::Enum)]
pub enum Change {
    Insert { insert: Arc<Resource> },
    Delete { delete: u32 },
    Retain { retain: u32 },
}

#[uniffi::export]
pub fn make_change_insert(resource: Arc<Resource>) -> Change {
    Change::Insert { insert: resource }
}

#[uniffi::export]
pub fn make_change_delete(count: u32) -> Change {
    Change::Delete { delete: count }
}

#[uniffi::export]
pub fn make_change_retain(count: u32) -> Change {
    Change::Retain { retain: count }
}

uniffi::setup_scaffolding!();
