#[derive(uniffi::Enum, Debug, PartialEq)]
#[repr(i32)]
pub enum IndexExport {
    Update = -0x6001,
    Snapshot = 0,
    Delete = 1,
    Other = 0x4000,
}

#[derive(uniffi::Enum, Debug, PartialEq)]
#[repr(u32)]
pub enum LargeUnsigned {
    Zero = 0,
    Big = 0xFFFF_FFF0,
}

#[uniffi::export]
fn roundtrip_index(value: IndexExport) -> IndexExport {
    value
}

#[uniffi::export]
fn roundtrip_large(value: LargeUnsigned) -> LargeUnsigned {
    value
}

uniffi::setup_scaffolding!();
