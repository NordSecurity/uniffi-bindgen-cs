class {{ callback_impl_name }} {
    {%- for (ffi_callback, meth) in vtable_methods.iter() %}
    static {% call cs::ffi_return_type(ffi_callback) %} {{ meth.name()|fn_name }}({% call cs::arg_list_ffi_decl(ffi_callback) %}) {
        var handle = @uniffiHandle;
        {%- if !meth.is_async() %}
        try {
            if (!{{ ffi_converter_var }}.handleMap.TryGet(handle, out var uniffiObject)) {
                throw new InternalException($"No callback in handlemap '{handle}'");
            }

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

            _uniffi_out_err.code = UniffiCallbackResponseStatus.SUCCESS;
        }
        {%- match meth.throws_type() %}
        {%- when Some with (error_type) %}
        catch ({{ error_type|type_name(ci) }} e) {
            try {
                _uniffi_out_err.code = UniffiCallbackResponseStatus.ERROR;
                _uniffi_out_err.error_buf = {{ error_type|ffi_converter_name }}.INSTANCE.Lower(e);
            } catch {
                _uniffi_out_err.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
            }
        }
        {%- when None %}
        {%- endmatch %}
        catch (System.Exception e){
            _uniffi_out_err.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
            try {
                _uniffi_out_err.error_buf = FfiConverterString.INSTANCE.Lower(e.Message);
            }
            catch {
            }
        }

        {%- else %}
        var futureHandle = new UniffiForeignFutureHandle();
        var foreignHandle = _UniFFIAsync._foreign_futures_map.Insert(futureHandle);
        unsafe {
            (*(_UniFFILib.UniffiForeignFutureDroppedCallbackStruct*)@uniffiOutDroppedCallback).handle = foreignHandle;
            (*(_UniFFILib.UniffiForeignFutureDroppedCallbackStruct*)@uniffiOutDroppedCallback).free = Marshal.GetFunctionPointerForDelegate(_UniFFIAsync.UniffiForeignFutureDroppedCallbackImpl.callback);
        }
        if (!{{ ffi_converter_var }}.handleMap.TryGet(handle, out var uniffiObject)) {
            var ret = new _UniFFILib.{{ meth.foreign_future_ffi_result_struct().name()|ffi_struct_name }}();
            ret.@callStatus = new UniffiRustCallStatus();
            ret.@callStatus.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
            try {
                ret.@callStatus.error_buf = FfiConverterString.INSTANCE.Lower($"No callback in handlemap '{handle}'");
            } catch { }
            {%- match meth.return_type() %}
            {%- when Some with (return_type) %}
            {%- let complete_fn_type = return_type|ffi_foreign_future_complete %}
            var earlyCb = Marshal.GetDelegateForFunctionPointer<_UniFFILib.{{ complete_fn_type }}>(@uniffiFutureCallback);
            {%- when None %}
            var earlyCb = Marshal.GetDelegateForFunctionPointer<_UniFFILib.UniffiForeignFutureCompleteVoid>(@uniffiFutureCallback);
            {%- endmatch %}
            futureHandle.InvokeCallbackOnce(() => { earlyCb(@uniffiCallbackData, ret); });
            futureHandle.Dispose();
            return;
        }

        // Optimization: skip queuing if already cancelled before Task.Run schedules.
        // TryInvokeCallback is the definitive cancellation guard inside the task body.
        Task.Run(async () => {
            var ret = new _UniFFILib.{{ meth.foreign_future_ffi_result_struct().name()|ffi_struct_name }}();
            ret.@callStatus = new UniffiRustCallStatus();

            try {
            try {

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
                .WaitAsync(futureHandle.Cts.Token)
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
                try {
                    ret.@callStatus.code = UniffiCallbackResponseStatus.ERROR;
                    ret.@callStatus.error_buf = {{ error_type|ffi_converter_name }}.INSTANCE.Lower(e);
                } catch {
                    ret.@callStatus.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
                }
            } catch (OperationCanceledException) when (futureHandle.Cts.IsCancellationRequested) {
                ret.@callStatus.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
                try {
                    ret.@callStatus.error_buf = FfiConverterString.INSTANCE.Lower("Future cancelled");
                } catch { }
            } catch (System.Exception e) {
                ret.@callStatus.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
                try {
                    ret.@callStatus.error_buf = FfiConverterString.INSTANCE.Lower(e.Message);
                }
                catch {
                }
            }
            {%- when None %}
            } catch (OperationCanceledException) when (futureHandle.Cts.IsCancellationRequested) {
                ret.@callStatus.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
                try {
                    ret.@callStatus.error_buf = FfiConverterString.INSTANCE.Lower("Future cancelled");
                } catch { }
            } catch (System.Exception e) {
                ret.@callStatus.code = UniffiCallbackResponseStatus.UNEXPECTED_ERROR;
                try {
                    ret.@callStatus.error_buf = FfiConverterString.INSTANCE.Lower(e.Message);
                }
                catch {
                }
            }
            {%- endmatch %}

            {% match meth.return_type() %}
            {%- when Some with (return_type) %}
            {%- let complete_fn_type = return_type|ffi_foreign_future_complete %}
            var cb = Marshal.GetDelegateForFunctionPointer<_UniFFILib.{{ complete_fn_type }}>(@uniffiFutureCallback);
            {%- when None %}
            var cb = Marshal.GetDelegateForFunctionPointer<_UniFFILib.UniffiForeignFutureCompleteVoid>(@uniffiFutureCallback);
            {%- endmatch %}
            futureHandle.InvokeCallbackOnce(() => {
                cb(@uniffiCallbackData, ret);
            });
            } finally {
                futureHandle.Dispose();
            }
        }, futureHandle.Cts.Token);
        {%- endif %}
    }
    {%- endfor %}

    static void UniffiFree(ulong @handle) {
        {{ ffi_converter_var }}.handleMap.Remove(@handle);
    }

    static ulong UniffiClone(ulong @handle) {
        try {
            if (!{{ ffi_converter_var }}.handleMap.TryGet(@handle, out var obj)) {
                throw new InternalException($"No callback in handlemap '{@handle}'");
            }
            return {{ ffi_converter_var }}.handleMap.Insert(obj);
        } catch (System.Exception) {
            return 0; // 0 is never a valid handle; ConcurrentHandleMap starts at 1
        }
    }

    {%- for (ffi_callback, meth) in vtable_methods.iter() %}
    {%- let fn_type = format!("_UniFFILib.{}Method", callback_impl_name) %}
    static {{ fn_type }}{{ loop.index0 }} _m{{ loop.index0 }} = new {{ fn_type }}{{ loop.index0 }}({{ meth.name()|fn_name }});
    {%- endfor %}
    static _UniFFILib.UniffiCallbackInterfaceFree _callback_interface_free = new _UniFFILib.UniffiCallbackInterfaceFree(UniffiFree);
    static _UniFFILib.UniffiCallbackInterfaceClone _callback_interface_clone = new _UniFFILib.UniffiCallbackInterfaceClone(UniffiClone);

    private static GCHandle? _vtablePin;

    public static void Register() {
        if (_vtablePin.HasValue) return;
        _UniFFILib.{{ vtable|ffi_type_name }} _vtable = new _UniFFILib.{{ vtable|ffi_type_name }} {
            {%- for (ffi_callback, meth) in vtable_methods.iter() %}
            {%- let fn_type = format!("_UniFFILib.{}Method", callback_impl_name) %}
            {{ meth.name()|var_name() }} = Marshal.GetFunctionPointerForDelegate(_m{{ loop.index0 }}),
            {%- endfor %}
            @uniffiFree = Marshal.GetFunctionPointerForDelegate(_callback_interface_free),
            @uniffiClone = Marshal.GetFunctionPointerForDelegate(_callback_interface_clone),
        };

        // Pin the vtable so the GC never moves it. The GCHandle is intentionally never freed —
        // this pin must remain valid for the process lifetime.
        _vtablePin = GCHandle.Alloc(_vtable, GCHandleType.Pinned);
        _UniFFILib.{{ ffi_init_callback.name() }}(_vtablePin.Value.AddrOfPinnedObject());
    }
}

{% if self.include_once_check("ConcurrentHandleMap.cs") %}{% include "ConcurrentHandleMap.cs" %}{% endif %}
{% if self.include_once_check("CallbackResponseStatus.cs") %}{% include "CallbackResponseStatus.cs" %}{% endif %}
