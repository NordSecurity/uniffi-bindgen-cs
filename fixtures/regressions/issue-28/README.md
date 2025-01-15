https://github.com/NordSecurity/uniffi-bindgen-cs/issues/28

If the interface's name is like `ASDFObject`, typenames are not generated uniformly.
There will be a difference between the class name `AsdfObject` and the return type of the ffi constructor `ASDFObject`.