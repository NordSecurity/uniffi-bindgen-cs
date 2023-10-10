/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use uniffi_bindgen::backend::{CodeType, Literal};
use super::{CsCodeOracle, AsCodeType};

#[derive(Debug)]
pub struct ErrorCodeType {
    id: String,
}

impl ErrorCodeType {
    pub fn new(id: String) -> Self {
        Self { id }
    }
}

impl CodeType for ErrorCodeType {
    fn type_label(&self) -> String {
        CsCodeOracle.error_name(&self.id)
    }

    fn canonical_name(&self) -> String {
        format!("Type{}", CsCodeOracle.error_name(&self.id))
    }

    fn literal(&self, _literal: &Literal) -> String {
        unreachable!();
    }
}

pub struct ErrorCodeTypeProvider<'a> {
    pub id: &'a String,
}

impl<'a> AsCodeType for ErrorCodeTypeProvider<'a> {
    fn as_codetype(&self) -> Box<dyn CodeType> {
        Box::new(ErrorCodeType::new(self.id.clone()))
    }
}
