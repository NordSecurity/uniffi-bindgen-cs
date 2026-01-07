/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

use super::CodeType;
use paste::paste;
use uniffi_bindgen::{interface::Literal, ComponentInterface};

macro_rules! impl_code_type_for_miscellany {
    ($T:ty, $class_name:literal, $canonical_name:literal, $default:literal) => {
        paste! {
            #[derive(Debug)]
            pub struct $T;

            impl CodeType for $T  {
                fn type_label(&self, _ci: &ComponentInterface) -> String {
                    $class_name.into()
                }

                fn canonical_name(&self) -> String {
                   $canonical_name.into()
               }

                fn literal(&self, _literal: &Literal, _ci: &ComponentInterface) -> String {
                    unreachable!()
                }

                fn default_value(&self, _ci: &ComponentInterface) -> String {
                    $default.into()
                }
            }
        }
    };
}

impl_code_type_for_miscellany!(TimestampCodeType, "DateTime", "Timestamp", "default(DateTime)");

impl_code_type_for_miscellany!(DurationCodeType, "TimeSpan", "Duration", "default(TimeSpan)");
