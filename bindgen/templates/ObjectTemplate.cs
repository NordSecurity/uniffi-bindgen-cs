{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}
{{- self.add_import("System.Threading")}}

{%- let obj = ci.get_object_definition(name).unwrap() %}
{%- let is_error = ci.is_name_used_as_error(name) %}
{%- let (interface_name, impl_name) = obj|object_names(ci) %}

{%- call cs::docstring(obj, 0) %}
{{ config.access_modifier() }} interface {{ interface_name }}
    {%- for tm in obj.uniffi_traits() -%}
    {%- match tm -%}
    {%- when UniffiTrait::Eq { eq, ne } -%}
    : IEquatable<{{impl_name}}>
    {%- else -%}
    {%- endmatch -%}
    {%- endfor %} {
    {%- for meth in obj.methods() %}
    {%- call cs::docstring(meth, 4) %}
    {%- call cs::method_throws_annotation(meth.throws_type()) %}
    {%  call cs::return_type(meth) %} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %});
    {%- endfor %}
}

{%- call cs::docstring(obj, 0) %}
{{ config.access_modifier() }} class {{ impl_name }} : {% if is_error -%}UniffiException, {% endif -%}{{ interface_name }}, IDisposable {
    protected ulong pointer;
    private int _wasDestroyed = 0;
    private long _callCounter = 1;

    public {{ impl_name }}(ulong pointer) {
        this.pointer = pointer;
    }

    ~{{ impl_name }}() {
        Destroy();
    }

    {%- match obj.primary_constructor() %}
    {%- when Some with (cons) %}
    {%- call cs::docstring(cons, 4) %}
    {%- if cons.is_async() %}
    public static async Task<{{ impl_name }}> {{ impl_name }}Async ({%- call cs::arg_list_decl(cons) -%}) {
        {%- call cs::async_call(cons, false) %}
    }
    {%- else %}
    public {{ impl_name }}({% call cs::arg_list_decl(cons) -%}) :
        this({% call cs::to_ffi_call(cons) %}) {}
    {%- endif %}
    {%- when None %}
    {%- endmatch %}

    protected void FreeRustArcPtr() {
        _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            _UniFFILib.{{ obj.ffi_object_free().name() }}(this.pointer, ref status);
        });
    }

    protected ulong CloneRustArcPtr() {
        return _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            return _UniFFILib.{{ obj.ffi_object_clone().name() }}(this.pointer, ref status);
        });
    }

    public void Destroy()
    {
        // Only allow a single call to this method.
        if (Interlocked.CompareExchange(ref _wasDestroyed, 1, 0) == 0)
        {
            // This decrement always matches the initial count of 1 given at creation time.
            if (Interlocked.Decrement(ref _callCounter) == 0)
            {
                FreeRustArcPtr();
            }
        }
    }

    public void Dispose()
    {
        Destroy();
        GC.SuppressFinalize(this); // Suppress finalization to avoid unnecessary GC overhead.
    }

    private void IncrementCallCounter() 
    {
        // Check and increment the call counter, to keep the object alive.
        // This needs a compare-and-set retry loop in case of concurrent updates.
        long count;
        do
        {
            count = Interlocked.Read(ref _callCounter);
            if (count == 0L) throw new System.ObjectDisposedException(String.Format("'{0}' object has already been destroyed", this.GetType().Name));
            if (count == long.MaxValue) throw new System.OverflowException(String.Format("'{0}' call counter would overflow", this.GetType().Name));

        } while (Interlocked.CompareExchange(ref _callCounter, count + 1, count) != count);
    }

    private void DecrementCallCounter() 
    {
        // This decrement always matches the increment we performed above.
        if (Interlocked.Decrement(ref _callCounter) == 0) {
            FreeRustArcPtr();
        }
    }

    internal void CallWithPointer(Action<ulong> action)
    {
        IncrementCallCounter();
        try {
            action(CloneRustArcPtr());
        }
        finally {
            DecrementCallCounter();
        }
    }

    internal T CallWithPointer<T>(Func<ulong, T> func)
    {   
        IncrementCallCounter();
        try {
            return func(CloneRustArcPtr());
        }
        finally {
            DecrementCallCounter();
        }
    }

    {% for meth in obj.methods() -%}
    {%- call cs::docstring(meth, 4) %}
    {%- call cs::method_throws_annotation(meth.throws_type()) %}
    {%- if meth.is_async() %}
    public async {% call cs::return_type(meth) %} {{ meth.name()|fn_name }}({%- call cs::arg_list_decl(meth) -%}) {
        {%- call cs::async_call(meth, true) %}
    }
    {%- else %}

    {%- match meth.return_type() -%}
    {%- when Some with (return_type) %}
    {%- if meth.name() == "Message" %}
    public new {{ return_type|type_name(ci) }} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %}) {
        return CallWithPointer(thisPtr => {{ return_type|lift_fn }}({%- call cs::to_ffi_call_with_prefix("thisPtr", meth) %}));
    }
    {%- else %}
    public {{ return_type|type_name(ci) }} {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %}) {
        return CallWithPointer(thisPtr => {{ return_type|lift_fn }}({%- call cs::to_ffi_call_with_prefix("thisPtr", meth) %}));
    }
    {%- endif %}

    {%- when None %}
    public void {{ meth.name()|fn_name }}({% call cs::arg_list_decl(meth) %}) {
        CallWithPointer(thisPtr => {%- call cs::to_ffi_call_with_prefix("thisPtr", meth) %});
    }
    {% endmatch %}
    {% endif %}
    {% endfor %}

    {%- for tm in obj.uniffi_traits() -%}
    {%- match tm %}
    {%- when UniffiTrait::Display { fmt } %}
    public override string ToString() {
        return CallWithPointer(thisPtr => {{ Type::String.borrow()|lift_fn  }}({%- call cs::to_ffi_call_with_prefix("thisPtr", fmt)  %}));
    }
    {%- when UniffiTrait::Eq { eq, ne } %}
    public bool Equals({{ impl_name }}? other)
    {
        if (other is null) return false;
        return CallWithPointer(thisPtr => {{ Type::Boolean.borrow()|lift_fn }}({%- call cs::to_ffi_call_with_prefix("thisPtr", eq) %}));
    }
    public override bool Equals(object? obj)
    {
        if (obj is null || !(obj is {{ impl_name }})) return false;
        return Equals(obj as {{ impl_name }});
    }
    {%- when UniffiTrait::Hash  { hash }  %}
    public override int GetHashCode() { 
        return (int)CallWithPointer(thisPtr => {{ Type::UInt64.borrow()|lift_fn }}({%- call cs::to_ffi_call_with_prefix("thisPtr", hash)  %}));
    }
    {%- else %}
    {%- endmatch %}
    {%- endfor %}

    {% if !obj.alternate_constructors().is_empty() -%}
    {% for cons in obj.alternate_constructors() -%}
    {%- call cs::docstring(cons, 4) %}
    {%- call cs::method_throws_annotation(cons.throws_type()) %}
    {%- if cons.is_async() %}
    public static async Task<{{ impl_name }}> {{ cons.name()|fn_name }} ({%- call cs::arg_list_decl(cons) -%}) {
        {%- call cs::async_call(cons, false) %}
    }
    {%- else %}
    public static {{ impl_name }} {{ cons.name()|fn_name }}({% call cs::arg_list_decl(cons) %}) {
        return new {{ impl_name }}({% call cs::to_ffi_call(cons) %});
    }
    {%- endif %}
    {% endfor %}
    {% endif %}
}

{%- let ffi_converter_type = obj|ffi_converter_name %}
{%- let ffi_converter_var = format!("{}.INSTANCE", ffi_converter_type)%}

{%- if obj.has_callback_interface() %}
{%- let vtable = obj.vtable().expect("Trait interface should have a vtable") %}
{%- let vtable_methods = obj.vtable_methods() %}
{%- let ffi_init_callback = obj.ffi_init_callback() %}

{%- let callback_impl_name = interface_name|ffi_callback_impl %}
{% include "CallbackInterfaceImpl.cs" %}

class {{ ffi_converter_type }}: FfiConverter<{{ interface_name }}, ulong> {
    public ConcurrentHandleMap<{{ interface_name }}> handleMap = new ConcurrentHandleMap<{{ interface_name }}>();
    
    public static {{ ffi_converter_type }} INSTANCE = new {{ ffi_converter_type }}();


    public override ulong Lower({{ interface_name }} value) {
        return handleMap.Insert(value);
    }

    public override {{ interface_name }} Lift(ulong value) {
        return new {{ impl_name }}(value);
    }

    public override {{ interface_name }} Read(BigEndianStream stream) {
        return Lift(stream.ReadULong());
    }

    public override int AllocationSize({{ interface_name }} value) {
        return 8;
    }

    public override void Write({{ interface_name }} value, BigEndianStream stream) {
        stream.WriteULong(Lower(value));
    }
}
{%- else %}
class {{ ffi_converter_type }}: FfiConverter<{{ impl_name }}, ulong> {
    public static {{ ffi_converter_type }} INSTANCE = new {{ ffi_converter_type }}();


    public override ulong Lower({{ impl_name }} value) {
        return value.CallWithPointer(thisPtr => thisPtr);
    }

    public override {{ impl_name }} Lift(ulong value) {
        return new {{ impl_name }}(value);
    }

    public override {{ impl_name }} Read(BigEndianStream stream) {
        return Lift(stream.ReadULong());
    }

    public override int AllocationSize({{ impl_name }} value) {
        return 8;
    }

    public override void Write({{ impl_name }} value, BigEndianStream stream) {
        stream.WriteULong(Lower(value));
    }
}
{%- endif %}

{%- if (is_error) %}
{%- let error_handler_name = obj|error_converter_name %}
class {{ error_handler_name }} : CallStatusErrorHandler<{{ impl_name }}> {
    public static {{ error_handler_name }} INSTANCE = new {{ error_handler_name }}();
    public {{ impl_name }} Lift(RustBuffer error_buf) {
        return {{ ffi_converter_type }}.INSTANCE.Read(error_buf.AsStream());
    }
}
{%- endif %}