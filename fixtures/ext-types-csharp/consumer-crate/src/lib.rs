use std::sync::Arc;
use uniffi_cs_ext_types_base::{
    BaseBlob, BaseEnum, BaseError, BaseInterface, BaseRecord, BaseTrait, BaseUrl,
};

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

// Custom types defined in the base crate, used directly and inside an optional, a sequence and a
// record. `BaseUrl` is mapped to `System.Uri` by the base crate's `uniffi.toml`.

#[derive(uniffi::Record)]
pub struct CustomTypeHolder {
    pub blob: BaseBlob,
    pub url: BaseUrl,
}

#[uniffi::export]
fn base_blob_len(blob: BaseBlob) -> u32 {
    blob.0.len() as u32
}

#[uniffi::export]
fn get_maybe_base_blob(blob: Option<BaseBlob>) -> Option<BaseBlob> {
    blob
}

#[uniffi::export]
fn get_base_blobs(blobs: Vec<BaseBlob>) -> Vec<BaseBlob> {
    blobs
}

#[uniffi::export]
fn base_url_string(url: BaseUrl) -> String {
    url.0
}

#[uniffi::export]
fn get_maybe_base_url(url: Option<BaseUrl>) -> Option<BaseUrl> {
    url
}

#[uniffi::export]
fn hold_custom_types(blob: BaseBlob, url: BaseUrl) -> CustomTypeHolder {
    CustomTypeHolder { blob, url }
}

uniffi::setup_scaffolding!();
