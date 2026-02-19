/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use super::CodeType;
use uniffi_bindgen::{interface::Literal, ComponentInterface};

#[derive(Debug)]
pub struct ExternalCodeType {
    name: String,
}

impl CodeType for ExternalCodeType {
    fn type_label(&self, _ci: &ComponentInterface) -> String {
        self.name.clone()
    }

    fn canonical_name(&self) -> String {
        format!("Type{}", self.name)
    }

    fn literal(&self, _literal: &Literal, _ci: &ComponentInterface) -> String {
        unreachable!("Can't have a literal of an external type");
    }
}
