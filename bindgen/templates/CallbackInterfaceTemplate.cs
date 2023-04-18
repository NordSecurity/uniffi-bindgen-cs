{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- let cbi = ci.get_callback_interface_definition(name).unwrap() %}
{%- let type_name = cbi|type_name %}
{%- let foreign_callback = format!("ForeignCallback{}", canonical_type_name) %}

{% if self.include_once_check("CallbackInterfaceRuntime.cs") %}{% include "CallbackInterfaceRuntime.cs" %}{% endif %}

{%- call cs::docstring(cbi, 0) %}
public interface {{ type_name }} {
    {%- for meth in cbi.methods() %}
    {%- call cs::docstring(meth, 4) %}
    {%- call cs::method_throws_annotation(meth.throws_type()) %}
    {%- match meth.return_type() %}
    {%- when Some with (return_type) %}
    {{ return_type|type_name }} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %});
    {%- else %}
    void {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %});
    {%- endmatch %}
    {%- endfor %}
}

// The ForeignCallback that is passed to Rust.
class {{ foreign_callback }} {
    // This cannot be a static method. Although C# supports implicitly using a static method as a
    // delegate, the behaviour is incorrect for this use case. Using static method as a delegate
    // argument creates an implicit delegate object, that is later going to be collected by GC. Any
    // attempt to invoke a garbage collected delegate results in an error:
    //   > A callback was made on a garbage collected delegate of type 'ForeignCallback::..'
    public static ForeignCallback INSTANCE = (ulong handle, int method, IntPtr argsData, int argsLength, ref RustBuffer outBuf) => {
        var cb = {{ type_|lift_fn }}(handle);
        switch (method) {
            case 0: {
                // 0 means Rust is done with the callback, and the callback
                // can be dropped by the foreign language.
                {{ ffi_converter_name }}.INSTANCE.Drop(handle);
                // No return value.
                // See docs of ForeignCallback in `uniffi/src/ffi/foreigncallbacks.rs`
                return 0;
            }

            {% for meth in cbi.methods() -%}
            {%- let method_name = meth.name()|fn_name %}
            {%- let method_name = format!("Invoke{}", method_name) %}
            case {{ loop.index }}: {
                try {
                    {%- match meth.throws_type() %}
                    {%- when Some(error_type) %}
                    try {
                        outBuf = {{ method_name }}(cb, RustBuffer.MemoryStream(argsData, argsLength));
                        return UniffiCallbackConstants.SUCCESS;
                    } catch ({{ error_type|type_name }} e) {
                        outBuf = {{ error_type|lower_fn }}(e);
                        return UniffiCallbackConstants.ERROR;
                    }
                    {%- else %}
                    outBuf = {{ method_name }}(cb, RustBuffer.MemoryStream(argsData, argsLength));
                    return UniffiCallbackConstants.SUCCESS;
                    {%- endmatch %}
                } catch (Exception e) {
                    // Unexpected error
                    try {
                        // Try to serialize the error into a string
                        outBuf = {{ Type::String.borrow()|lower_fn }}(e.Message);
                    } catch {
                        // If that fails, then it's time to give up and just return
                    }
                    return UniffiCallbackConstants.UNEXPECTED_ERROR;
                }
            }

            {% endfor %}
            default: {
                // This should never happen, because an out of bounds method index won't
                // ever be used. Once we can catch errors, we should return an InternalException.
                // https://github.com/mozilla/uniffi-rs/issues/351
                return UniffiCallbackConstants.UNEXPECTED_ERROR;
            }
        }
    };

    {% for meth in cbi.methods() -%}
    {% let method_name = meth.name()|fn_name -%}
    {% let method_name = format!("Invoke{}", method_name) -%}
    static RustBuffer {{ method_name }}({{ type_name }} callback, BigEndianStream stream) {
        {%- match meth.return_type() -%}
        {%- when Some with (return_type) -%}
        var result =
        {%- when None -%}
        {%- endmatch -%}
        callback.{{ meth.name()|fn_name }}(
                {%- for arg in meth.arguments() -%}
                {{ arg|read_fn }}(stream)
                {%- if !loop.last %}, {% endif -%}
                {%- endfor -%}
                );

        {% match meth.return_type() -%}
            {%- when Some with (return_type) -%}
                return {{ return_type|ffi_converter_name }}.INSTANCE.LowerIntoRustBuffer(result);
            {%- else -%}
                return new RustBuffer();
            {%- endmatch %}
    }

    {% endfor %}
}

// The ffiConverter which transforms the Callbacks in to Handles to pass to Rust.
class {{ ffi_converter_name }}: FfiConverterCallbackInterface<{{ type_name }}> {
    public static {{ ffi_converter_name }} INSTANCE = new {{ ffi_converter_name }}();

    public override void Register() {
        _UniffiHelpers.RustCall((ref RustCallStatus status) => {
            _UniFFILib.{{ cbi.ffi_init_callback().name() }}({{ foreign_callback }}.INSTANCE, ref status);
        });
    }
}
