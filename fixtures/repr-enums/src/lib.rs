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

#[derive(uniffi::Enum, Debug, PartialEq)]
#[repr(u64)]
pub enum HugeUnsigned {
    BeforeWrap = 0x7FFF_FFFF_FFFF_FFFF,
    Wrapped,
}

#[derive(uniffi::Enum, Debug, PartialEq)]
#[repr(u8)]
pub enum SmallUnsigned {
    Min = 0,
    Max = 255,
}

#[uniffi::export]
fn roundtrip_index(value: IndexExport) -> IndexExport {
    value
}

#[uniffi::export]
fn roundtrip_small(value: SmallUnsigned) -> SmallUnsigned {
    value
}

#[uniffi::export]
fn roundtrip_large(value: LargeUnsigned) -> LargeUnsigned {
    value
}

#[uniffi::export]
fn roundtrip_huge(value: HugeUnsigned) -> HugeUnsigned {
    value
}

uniffi::setup_scaffolding!();
