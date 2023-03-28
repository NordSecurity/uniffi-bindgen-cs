{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

// This is an implementation detail which will be called internally by the public API.
static class _UniFFILib {
    static _UniFFILib() {
        {% let initialization_fns = self.initialization_fns() %}
        {% for fn in initialization_fns -%}
        {{ fn }}();
        {% endfor -%}
    }

    {% for func in ci.iter_ffi_function_definitions() -%}
    [DllImport("{{ config.cdylib_name() }}")]
    public static extern {%- match func.return_type() -%}{%- when Some with (type_) %} {{ type_.borrow()|ffi_type_name }}{% when None %} void{% endmatch %} {{ func.name() }}(
        {%- call cs::arg_list_ffi_decl(func) %}
    );

    {% endfor %}
}
