uniffi::setup_scaffolding!();

#[uniffi::export]
fn nested_bytes(input: Vec<Vec<u8>>) -> Vec<Vec<u8>> {
    input
}

#[uniffi::export]
fn nested_strings(input: Vec<Vec<String>>) -> Vec<Vec<String>> {
    input
}
