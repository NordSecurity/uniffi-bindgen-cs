/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use super::{AsCodeType, CsCodeOracle};
use uniffi_bindgen::backend::{CodeType, Literal};

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
    fn type_label(&self) -> String {
        CsCodeOracle.error_name(&self.name)
    }

    fn canonical_name(&self) -> String {
        format!("Type{}", CsCodeOracle.error_name(&self.name))
    }

    fn literal(&self, _literal: &Literal) -> String {
        unreachable!();
    }
}

pub struct ErrorCodeTypeProvider<'a> {
    pub name: &'a String,
}

impl<'a> AsCodeType for ErrorCodeTypeProvider<'a> {
    fn as_codetype(&self) -> Box<dyn CodeType> {
        Box::new(ErrorCodeType::new(self.name.clone()))
    }
}
