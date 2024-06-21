/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use super::{AsCodeType, CodeType, CsCodeOracle};
use uniffi_bindgen::{backend::Literal, ComponentInterface};

#[derive(Debug)]
pub struct ErrorCodeType {
    name: String,
}

impl ErrorCodeType {
    pub fn new(name: String) -> Self {
        Self { name }
    }
}

impl CodeType for ErrorCodeType {
    fn type_label(&self, _ci: &ComponentInterface) -> String {
        CsCodeOracle.error_name(&self.name)
    }

    fn canonical_name(&self) -> String {
        format!("Type{}", CsCodeOracle.error_name(&self.name))
    }

    fn literal(&self, _literal: &Literal, _ci: &ComponentInterface) -> String {
        unreachable!();
    }
}

pub(super) struct ErrorCodeTypeProvider<'a> {
    pub name: &'a String,
}

impl<'a> AsCodeType for ErrorCodeTypeProvider<'a> {
    fn as_codetype(&self) -> Box<dyn CodeType> {
        Box::new(ErrorCodeType::new(self.name.clone()))
    }
}
