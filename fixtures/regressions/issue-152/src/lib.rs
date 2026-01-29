/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

/// A person record to test PascalCase property names
#[derive(uniffi::Record)]
pub struct Person {
    pub first_name: String,
    pub last_name: String,
    pub age: u32,
}

/// A record with a field that becomes a C# keyword when PascalCased
#[derive(uniffi::Record)]
pub struct KeywordFields {
    /// This becomes "Event" which is a C# keyword
    pub event: String,
    /// This becomes "Class" which is a C# keyword
    pub class: String,
    /// This stays "Name" which is not a keyword
    pub name: String,
}

/// Helper function to create a person
#[uniffi::export]
pub fn create_person(first_name: String, last_name: String, age: u32) -> Person {
    Person {
        first_name,
        last_name,
        age,
    }
}

/// Helper function to extract first name from a person
#[uniffi::export]
pub fn get_first_name(person: Person) -> String {
    person.first_name
}

/// Helper function to create keyword fields record
#[uniffi::export]
pub fn create_keyword_fields(event: String, class: String, name: String) -> KeywordFields {
    KeywordFields { event, class, name }
}

/// Helper function to extract event field
#[uniffi::export]
pub fn get_event(fields: KeywordFields) -> String {
    fields.event
}

uniffi::setup_scaffolding!();
