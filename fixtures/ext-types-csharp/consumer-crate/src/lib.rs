use std::collections::HashMap;
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
fn throw_external_not_found(name: String, code: i32) -> Result<(), BaseError> {
    Err(BaseError::NotFound { name, code })
}

#[uniffi::export]
fn external_error_identity(val: i32) -> Result<i32, BaseError> {
    if val >= 0 {
        Ok(val)
    } else {
        Err(BaseError::General("negative".to_string()))
    }
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

// External objects and trait interfaces inside a `RustBuffer`.
//
// A plain `Arc<BaseInterface>` argument crosses the FFI as a `u64` handle and never touches a
// `BigEndianStream`. Putting the external object inside an optional, a sequence, a map or a record
// forces the consumer's generated code to read and write it through its own stream.

#[derive(uniffi::Record)]
pub struct InterfaceHolder {
    pub iface: Arc<BaseInterface>,
    pub label: String,
}

#[uniffi::export]
fn get_maybe_base_interface(i: Option<Arc<BaseInterface>>) -> Option<Arc<BaseInterface>> {
    i
}

#[uniffi::export]
fn get_base_interfaces(is: Vec<Arc<BaseInterface>>) -> Vec<Arc<BaseInterface>> {
    is
}

#[uniffi::export]
fn get_base_interface_map(
    m: HashMap<String, Arc<BaseInterface>>,
) -> HashMap<String, Arc<BaseInterface>> {
    m
}

#[uniffi::export]
fn wrap_base_interface(iface: Arc<BaseInterface>, label: String) -> InterfaceHolder {
    InterfaceHolder { iface, label }
}

#[uniffi::export]
fn get_maybe_base_trait(t: Option<Arc<dyn BaseTrait>>) -> Option<Arc<dyn BaseTrait>> {
    t
}

#[uniffi::export]
fn greet_all(ts: Vec<Arc<dyn BaseTrait>>) -> Vec<String> {
    ts.iter().map(|t| t.greet()).collect()
}

uniffi::setup_scaffolding!();
