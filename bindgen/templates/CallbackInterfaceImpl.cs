class {{ callback_impl_name }} {
    {%- for (ffi_callback, meth) in vtable_methods.iter() %}
    static {% call cs::ffi_return_type(ffi_callback) %} {{ meth.name()|fn_name }}({% call cs::arg_list_ffi_decl(ffi_callback) %}) {
        var handle = @uniffiHandle;
        if ({{ ffi_converter_var }}.handleMap.TryGet(handle, out var uniffiObject)) {
            {%- if !meth.is_async() %}
            {%- match meth.throws_type() %}
            {%- when Some with (error_type) %}
            try {
            {%- when None %}
            {%- endmatch %}

            {%- match meth.return_type() %}
            {%- when Some with (return_type) %}
            var result =
            {%- when None %}
            {%- endmatch %}
            uniffiObject.{{ meth.name()|fn_name }}(
                {%- for arg in meth.arguments() %}
                {{ arg|lift_fn }}({{ arg.name()|var_name }}){%- if !loop.last %}, {% endif -%}
                {%- endfor %});

            {%- match meth.return_type() %}
            {%- when Some with (return_type) %}
            unsafe {
                {%- let return_ffi_type = return_type|ffi_type %}
                *({{ return_ffi_type|ffi_type_name }}*)uniffiOutReturn = {{ return_type|ffi_converter_name }}.INSTANCE.Lower(result);
            }
            {%- when None %}
            {%- endmatch %}

            {%- match meth.throws_type() %}
            {%- when Some with (error_type) %}
            _uniffi_out_err.code = UniffiCallbackResponseStatus.SUCCESS;
            }
            catch ({{ error_type|type_name(ci) }} e) {
                _uniffi_out_err.code = UniffiCallbackResponseStatus.ERROR;
                _uniffi_out_err.error_buf = {{ error_type|ffi_converter_name }}.INSTANCE.Lower(e);
            }
            catch (System.Exception e){
                _uniffi_out_err.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
                try {
                    _uniffi_out_err.error_buf = FfiConverterString.INSTANCE.Lower(e.Message);
                }
                catch {
                }
            }
            {%- when None %}
            {%- endmatch %}

            {%- else %}
            CancellationTokenSource cts = new CancellationTokenSource();

            Task.Run(async () => {
                var ret = new _UniFFILib.{{ meth.foreign_future_ffi_result_struct().name()|ffi_struct_name }}();
                ret.@callStatus = new UniffiRustCallStatus();

                {%- match meth.throws_type() %}
                {%- when Some with (error_type) %}
                try {
                {%- when None %}
                {%- endmatch %}

                {%- match meth.return_type() %}
                {%- when Some with (return_type) %}
                var result =
                {%- when None %}
                {%- endmatch %}

                await uniffiObject.{{ meth.name()|fn_name }}(
                    {%- for arg in meth.arguments() %}
                    {{ arg|lift_fn }}({{ arg.name()|var_name }}){%- if !loop.last %}, {% endif -%}
                    {%- endfor %})
                #if NET6_0_OR_GREATER
                    .WaitAsync(cts.Token)
                #endif
                    ;

                {%- match meth.return_type() %}
                {%- when Some with (return_type) %}
                {%- let complete_fn_type = return_type|ffi_foreign_future_complete %}
                ret.@returnValue = {{ return_type|ffi_converter_name }}.INSTANCE.Lower(result);
                {%- when None %}
                {%- let complete_fn_type = "ForeignFutureCompleteVoid" %}
                {%- endmatch %}

                ret.@callStatus.code = UniffiCallbackResponseStatus.SUCCESS;
                {%- match meth.throws_type() %}
                {%- when Some with (error_type) %}
                } catch ({{ error_type|type_name(ci) }} e) {
                    ret.@callStatus.code = UniffiCallbackResponseStatus.ERROR;
                    ret.@callStatus.error_buf = {{ error_type|ffi_converter_name }}.INSTANCE.Lower(e);
                } catch (System.Exception e){
                    ret.@callStatus.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
                    try {
                        ret.@callStatus.error_buf = FfiConverterString.INSTANCE.Lower(e.Message);
                    }
                    catch {
                    }
                }
                {%- when None %}
                {%- endmatch %}

                {% match meth.return_type() %}
                {%- when Some with (return_type) %}
                {%- let complete_fn_type = return_type|ffi_foreign_future_complete %}
                var cb = Marshal.GetDelegateForFunctionPointer<_UniFFILib.{{ complete_fn_type }}>(@uniffiFutureCallback);
                {%- when None %}
                var cb = Marshal.GetDelegateForFunctionPointer<_UniFFILib.UniffiForeignFutureCompleteVoid>(@uniffiFutureCallback);
                {%- endmatch %}
                cb(@uniffiCallbackData, ret);
            }, cts.Token);

            var foreignHandle = _UniFFIAsync._foreign_futures_map.Insert(cts);
            unsafe {
                (*(_UniFFILib.UniffiForeignFutureDroppedCallbackStruct*)@uniffiOutDroppedCallback).handle = foreignHandle;
                (*(_UniFFILib.UniffiForeignFutureDroppedCallbackStruct*)@uniffiOutDroppedCallback).free = Marshal.GetFunctionPointerForDelegate(_UniFFIAsync.UniffiForeignFutureDroppedCallbackImpl.callback);
            }
            {%- endif %}
        } else {
            throw new InternalException($"No callback in handlemap '{handle}'");
        }
    }
    {%- endfor %}

    static void UniffiFree(ulong @handle) {
        {{ ffi_converter_var }}.handleMap.Remove(@handle);
    }

    {%- for (ffi_callback, meth) in vtable_methods.iter() %}
    {%- let fn_type = format!("_UniFFILib.{}Method", callback_impl_name) %}
    static {{ fn_type }}{{ loop.index0 }} _m{{ loop.index0 }} = new {{ fn_type }}{{ loop.index0 }}({{ meth.name()|fn_name }});
    {%- endfor %}
    static _UniFFILib.UniffiCallbackInterfaceFree _callback_interface_free = new _UniFFILib.UniffiCallbackInterfaceFree(UniffiFree);

    public static void Register() {
        _UniFFILib.{{ vtable|ffi_type_name }} _vtable = new _UniFFILib.{{ vtable|ffi_type_name }} {
            {%- for (ffi_callback, meth) in vtable_methods.iter() %}
            {%- let fn_type = format!("_UniFFILib.{}Method", callback_impl_name) %}
            {{ meth.name()|var_name() }} = Marshal.GetFunctionPointerForDelegate(_m{{ loop.index0 }}),
            {%- endfor %}
            @uniffiFree = Marshal.GetFunctionPointerForDelegate(_callback_interface_free)
        };

        // Pin vtable to ensure GC does not move the vtable across the heap
        _UniFFILib.{{ ffi_init_callback.name() }}(GCHandle.Alloc(_vtable, GCHandleType.Pinned).AddrOfPinnedObject());
    }
}

{% if self.include_once_check("ConcurrentHandleMap.cs") %}{% include "ConcurrentHandleMap.cs" %}{% endif %}
{% if self.include_once_check("CallbackResponseStatus.cs") %}{% include "CallbackResponseStatus.cs" %}{% endif %}
