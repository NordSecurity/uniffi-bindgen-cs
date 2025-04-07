#[derive(uniffi::Enum)]
pub enum PositionalEnum {
    GoodVariant(String),
    NiceVariant(i32, String),
}
uniffi::setup_scaffolding!();