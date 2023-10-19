/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

pub fn hello_world() -> String {
    "Hello, world!".to_string()
}

uniffi::include_scaffolding!("global_methods_class_name");
