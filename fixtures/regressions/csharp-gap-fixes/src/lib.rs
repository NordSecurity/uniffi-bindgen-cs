pub fn nested_bytes() -> Vec<Vec<Vec<u8>>> {
    vec![vec![vec![1, 2], vec![3]], vec![vec![4, 5, 6]]]
}

pub struct Monitor {}

impl Monitor {
    fn new() -> Monitor {
        Monitor {}
    }

    fn monitor(&self) -> String {
        "monitor".to_string()
    }

    fn monitor_method(&self) -> String {
        "monitor_method".to_string()
    }
}

uniffi::include_scaffolding!("csharp-gap-fixes");
