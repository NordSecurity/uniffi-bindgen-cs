pub enum Value {
    Null,
    Bool { value: bool },
    Double { value: f64 },
    I64 { value: i64 },
    Binary { value: Vec<u8> },
    String { value: String }
}

uniffi::include_scaffolding!("issue-110");
