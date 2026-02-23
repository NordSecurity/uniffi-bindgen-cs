use std::sync::Arc;

#[derive(uniffi::Record, Debug, PartialEq)]
pub struct BaseRecord {
    pub name: String,
    pub value: i32,
}

#[derive(uniffi::Enum, Debug, PartialEq)]
pub enum BaseEnum {
    Alpha,
    Beta { detail: String },
}

#[derive(uniffi::Object)]
pub struct BaseInterface {
    label: String,
}

#[uniffi::export]
impl BaseInterface {
    #[uniffi::constructor]
    pub fn new(label: String) -> Self {
        Self { label }
    }

    pub fn label(&self) -> String {
        self.label.clone()
    }
}

#[uniffi::export(with_foreign)]
pub trait BaseTrait: Send + Sync {
    fn greet(&self) -> String;
}

#[derive(Debug, thiserror::Error, uniffi::Error)]
pub enum BaseError {
    #[error("{0}")]
    General(String),
}

#[uniffi::export]
fn create_base_record(name: String, value: i32) -> BaseRecord {
    BaseRecord { name, value }
}

#[uniffi::export]
fn get_base_enum_alpha() -> BaseEnum {
    BaseEnum::Alpha
}

#[uniffi::export]
fn invoke_base_trait(t: Arc<dyn BaseTrait>) -> String {
    t.greet()
}

#[uniffi::export]
fn throw_base_error(msg: String) -> Result<(), BaseError> {
    Err(BaseError::General(msg))
}

uniffi::setup_scaffolding!();
