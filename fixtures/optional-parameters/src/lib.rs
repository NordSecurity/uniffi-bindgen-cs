/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

uniffi::setup_scaffolding!();

#[derive(uniffi::Record)]
pub struct Person {
    #[uniffi(default = None)]
    pub name: Option<String>,
    pub is_special: bool,
}

#[uniffi::export]
fn hello(person: Person) -> String {
    let name = person.name.unwrap_or("stranger".to_string());
    if person.is_special {
        format!("Hello {name}! You are special!")
    } else {
        format!("Hello {name}!")
    }
}
