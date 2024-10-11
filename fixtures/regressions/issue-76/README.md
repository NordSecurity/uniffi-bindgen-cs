# https://github.com/NordSecurity/uniffi-bindgen-cs/issues/76

`Error` is translated into `Exception`, and causes ambiguous type references between `System.Exception` and `Exception`.
