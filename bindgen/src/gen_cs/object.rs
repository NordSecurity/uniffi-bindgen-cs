/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use super::CodeType;
use uniffi_bindgen::{interface::{Literal, ObjectImpl}, ComponentInterface};

#[derive(Debug)]
pub struct ObjectCodeType {
    id: String,
    imp: ObjectImpl,
}

impl ObjectCodeType {
    pub fn new(id: String, imp: ObjectImpl) -> Self {
        Self { id, imp }
    }
}

impl CodeType for ObjectCodeType {
    fn type_label(&self, ci: &ComponentInterface) -> String {
        super::CsCodeOracle.class_name(&self.id, ci)
    }

    fn canonical_name(&self) -> String {
        format!("Type{}", self.id)
    }

    fn error_converter_name(&self) -> String {
        format!("{}ErrorHandler", self.ffi_converter_name())
    }

    fn literal(&self, _literal: &Literal, _ci: &ComponentInterface) -> String {
        unreachable!();
    }

    fn initialization_fn(&self) -> Option<String> {
        self.imp
            .has_callback_interface()
            .then(|| format!("UniffiCallbackInterface{}.Register", self.id))
    }
}
