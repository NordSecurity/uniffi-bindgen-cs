pub struct ASDFObject {}

impl ASDFObject {
    fn new() -> ASDFObject {
        ASDFObject {}
    }
}

uniffi::include_scaffolding!("issue-28");
