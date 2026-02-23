use std::sync::Arc;
use uniffi_cs_ext_types_base::{BaseEnum, BaseError, BaseInterface, BaseRecord, BaseTrait};

#[derive(uniffi::Record)]
pub struct CompositeRecord {
    pub base: BaseRecord,
    pub extra: String,
    pub variant: BaseEnum,
}

#[uniffi::export]
fn create_composite(name: String, value: i32, extra: String) -> CompositeRecord {
    CompositeRecord {
        base: BaseRecord { name, value },
        extra,
        variant: BaseEnum::Alpha,
    }
}

#[uniffi::export]
fn get_base_from_composite(c: CompositeRecord) -> BaseRecord {
    c.base
}

#[uniffi::export]
fn get_base_interface(label: String) -> Arc<BaseInterface> {
    Arc::new(BaseInterface::new(label))
}

#[uniffi::export]
fn invoke_external_trait(t: Arc<dyn BaseTrait>) -> String {
    t.greet()
}

#[uniffi::export]
fn throw_external_error() -> Result<(), BaseError> {
    Err(BaseError::General("external error".to_string()))
}

#[uniffi::export]
fn get_maybe_base_record(r: Option<BaseRecord>) -> BaseRecord {
    r.unwrap_or_else(|| BaseRecord {
        name: "default".to_string(),
        value: 0,
    })
}

#[uniffi::export]
fn get_base_records(rs: Vec<BaseRecord>) -> Vec<BaseRecord> {
    rs
}

#[uniffi::export]
fn get_maybe_base_enum(e: Option<BaseEnum>) -> Option<BaseEnum> {
    e
}

uniffi::setup_scaffolding!();
