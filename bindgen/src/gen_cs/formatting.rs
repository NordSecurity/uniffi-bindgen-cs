use std::io::Write;
use std::process::{Command, Stdio};

pub fn format(bindings: String) -> Result<String, anyhow::Error> {
    let mut csharpier = Command::new("dotnet-csharpier")
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .spawn()?;

    csharpier
        .stdin
        .take()
        .ok_or(anyhow::anyhow!("Failed to open stdin"))?
        .write_all(bindings.as_bytes())?;

    let output = csharpier.wait_with_output()?;
    if !output.status.success() {
        Err(std::io::Error::new(
            std::io::ErrorKind::Other,
            format!(
                "command returned non-zero exit status: {:?}",
                output.status.code()
            ),
        ))?;
    }

    let formatted = String::from_utf8(output.stdout)?;

    Ok(formatted)
}

pub fn add_header(bindings: String) -> String {
    let version = env!("CARGO_PKG_VERSION");
    let header = format!(
        r##"// <auto-generated>
//     This file was generated by uniffi-bindgen-cs v{version}
//     See https://github.com/NordSecurity/uniffi-bindgen-cs for more information.
// </auto-generated>

#nullable enable

"##
    );

    header + &bindings
}