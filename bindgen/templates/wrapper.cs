{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Common helper code.
//
// Ideally this would live in a separate .cs file where it can be unittested etc
// in isolation, and perhaps even published as a re-useable package.
//
// However, it's important that the detils of how this helper code works (e.g. the
// way that different builtin types are passed across the FFI) exactly match what's
// expected by the Rust code on the other side of the interface. In practice right
// now that means coming from the exact some version of `uniffi` that was used to
// compile the Rust component. The easiest way to ensure this is to bundle the C#
// helpers directly inline like we're doing here.
#}

using System.IO;
using System.Runtime.InteropServices;
using System;

{%- for imported_class in self.imports() %}
using {{ imported_class }};
{%- endfor %}

{%- call cs::docstring(ci.namespace_definition().unwrap(), 0) %}
namespace {{ config.package_name() }}.{{ ci.namespace() }};

{%- for alias in self.type_aliases() %}
using {{ alias.alias }} = {{ alias.original_type }};
{%- endfor %}

{% include "RustBufferTemplate.cs" %}
{% include "FfiConverterTemplate.cs" %}
{% include "Helpers.cs" %}
{% include "BigEndianStream.cs" %}

// Contains loading, initialization code,
// and the FFI Function declarations in a com.sun.jna.Library.
{% include "NamespaceLibraryTemplate.cs" %}

// Public interface members begin here.
{# details/1-empty-list-as-default-method-parameter.md #}
#pragma warning disable 8625
{{ type_helper_code }}
#pragma warning restore 8625

public static class {{ ci.namespace().to_upper_camel_case() }}Methods {
{%- for func in ci.function_definitions() %}
{%- include "TopLevelFunctionTemplate.cs" %}
{%- endfor %}
}

{% import "macros.cs" as cs %}
