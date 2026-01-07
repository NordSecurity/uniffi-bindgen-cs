/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use super::CodeType;
use paste::paste;
use uniffi_bindgen::{
    interface::{DefaultValue, Literal, Type},
    ComponentInterface,
};

fn render_literal(literal: &Literal, inner: &Type, ci: &ComponentInterface) -> String {
    match literal {
        Literal::None => "null".into(),
        Literal::Some { inner: default_meta } => {
            match default_meta.as_ref() {
                DefaultValue::Default => super::CsCodeOracle.find(inner).default_value(ci),
                DefaultValue::Literal(lit) => super::CsCodeOracle.find(inner).literal(lit, ci),
            }
        }

        // details/1-empty-list-as-default-method-parameter.md
        Literal::EmptySequence => "null".into(),
        Literal::EmptyMap => "null".into(),

        // For optionals
        _ => super::CsCodeOracle.find(inner).literal(literal, ci),
    }
}

macro_rules! impl_code_type_for_compound {
     ($T:ty, $type_label_pattern:literal, $canonical_name_pattern: literal, $default_pattern:literal) => {
        paste! {
            #[derive(Debug)]
            pub struct $T {
                inner: Type,
            }

            impl $T {
                pub fn new(inner: Type) -> Self {
                    Self { inner }
                }
                fn inner(&self) -> &Type {
                    &self.inner
                }
            }

            impl CodeType for $T  {
                fn type_label(&self, ci: &ComponentInterface) -> String {
                    format!($type_label_pattern, super::CsCodeOracle.find(self.inner()).type_label(ci))
                }

                fn canonical_name(&self) -> String {
                    format!($canonical_name_pattern, super::CsCodeOracle.find(self.inner()).canonical_name())
                }

                fn literal(&self, literal: &Literal, ci: &ComponentInterface) -> String {
                    render_literal(literal, self.inner(), ci)
                }

                fn default_value(&self, _ci: &ComponentInterface) -> String {
                    $default_pattern.into()
                }
            }
        }
    }
 }

impl_code_type_for_compound!(OptionalCodeType, "{}?", "Optional{}", "null");
impl_code_type_for_compound!(SequenceCodeType, "{}[]", "Sequence{}", "null");

#[derive(Debug)]
pub struct MapCodeType {
    key: Type,
    value: Type,
}

impl MapCodeType {
    pub fn new(key: Type, value: Type) -> Self {
        Self { key, value }
    }

    fn key(&self) -> &Type {
        &self.key
    }

    fn value(&self) -> &Type {
        &self.value
    }
}

impl CodeType for MapCodeType {
    fn type_label(&self, ci: &ComponentInterface) -> String {
        format!(
            "Dictionary<{}, {}>",
            super::CsCodeOracle.find(self.key()).type_label(ci),
            super::CsCodeOracle.find(self.value()).type_label(ci),
        )
    }

    fn canonical_name(&self) -> String {
        format!(
            "Dictionary{}{}",
            super::CsCodeOracle.find(self.key()).canonical_name(),
            super::CsCodeOracle.find(self.value()).canonical_name(),
        )
    }

    fn literal(&self, literal: &Literal, ci: &ComponentInterface) -> String {
        render_literal(literal, &self.value, ci)
    }

    fn default_value(&self, _ci: &ComponentInterface) -> String {
        "null".into()
    }
}
