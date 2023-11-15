{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

{%- import "macros.cs" as cs %}

{%- for type_ in ci.iter_types() %}
{%- let type_name = type_|type_name %}
{%- let ffi_converter_name = type_|ffi_converter_name %}
{%- let canonical_type_name = type_|canonical_name %}
{%- let contains_object_references = ci.item_contains_object_references(type_) %}

{#
# Map `Type` instances to an include statement for that type.
#
# There is a companion match in `CsCodeOracle::create_code_type()` which performs a similar function for the
# Rust code.
#
#   - When adding additional types here, make sure to also add a match arm to that function.
#   - To keep things managable, let's try to limit ourselves to these 2 mega-matches
#}
{%- match type_ %}

{%- when Type::Boolean %}
{%- include "BooleanHelper.cs" %}

{%- when Type::Int8 %}
{%- include "Int8Helper.cs" %}

{%- when Type::Int16 %}
{%- include "Int16Helper.cs" %}

{%- when Type::Int32 %}
{%- include "Int32Helper.cs" %}

{%- when Type::Int64 %}
{%- include "Int64Helper.cs" %}

{%- when Type::UInt8 %}
{%- include "UInt8Helper.cs" %}

{%- when Type::UInt16 %}
{%- include "UInt16Helper.cs" %}

{%- when Type::UInt32 %}
{%- include "UInt32Helper.cs" %}

{%- when Type::UInt64 %}
{%- include "UInt64Helper.cs" %}

{%- when Type::Float32 %}
{%- include "Float32Helper.cs" %}

{%- when Type::Float64 %}
{%- include "Float64Helper.cs" %}

{%- when Type::String %}
{%- include "StringHelper.cs" %}

{%- when Type::Enum { name, module_path } %}
{%- let e = ci.get_enum_definition(name).unwrap() %}
{%- if !ci.is_name_used_as_error(name) %}
{% include "EnumTemplate.cs" %}
{%- else %}
{% include "ErrorTemplate.cs" %}
{%- endif -%}

{%- when Type::Object{ name, imp, module_path } %}
{% include "ObjectTemplate.cs" %}

{%- when Type::Record{ name, module_path } %}
{% include "RecordTemplate.cs" %}

{%- when Type::Optional { inner_type } %}
{% include "OptionalTemplate.cs" %}

{%- when Type::Sequence { inner_type } %}
{% include "SequenceTemplate.cs" %}

{%- when Type::Bytes %}
{% include "BytesTemplate.cs" %}

{%- when Type::Map { key_type, value_type } %}
{% include "MapTemplate.cs" %}

{%- when Type::CallbackInterface { name, module_path } %}
{% include "CallbackInterfaceTemplate.cs" %}

{%- when Type::Timestamp %}
{% include "TimestampHelper.cs" %}

{%- when Type::Duration %}
{% include "DurationHelper.cs" %}

{%- when Type::Custom { module_path, name, builtin } %}
{% include "CustomTypeTemplate.cs" %}

{%- when Type::External { module_path, name, namespace, kind, tagged } %}
{% include "ExternalTypeTemplate.cs" %}

{%- when Type::ForeignExecutor %}
{{ "ForeignExecutor not implemented in Types.cs {}"|panic }}

{%- endmatch %}
{%- endfor %}
