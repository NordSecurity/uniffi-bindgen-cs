{#/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */#}

// A handful of classes and functions to support the generated data structures.
// This would be a good candidate for isolating in its own ffi-support lib.
// Error runtime.
[StructLayout(LayoutKind.Sequential)]
struct UniffiRustCallStatus {
    public sbyte code;
    public RustBuffer error_buf;

    public bool IsSuccess() {
        return code == 0;
    }

    public bool IsError() {
        return code == 1;
    }

    public bool IsPanic() {
        return code == 2;
    }
}

// Base class for all uniffi exceptions
{{ config.access_modifier() }} class UniffiException: System.Exception {
    public UniffiException(): base() {}
    public UniffiException(string message): base(message) {}
}

{{ config.access_modifier() }} class UndeclaredErrorException: UniffiException {
    public UndeclaredErrorException(string message): base(message) {}
}

{{ config.access_modifier() }} class PanicException: UniffiException {
    public PanicException(string message): base(message) {}
}

{{ config.access_modifier() }} class AllocationException: UniffiException {
    public AllocationException(string message): base(message) {}
}

{{ config.access_modifier() }} class InternalException: UniffiException {
    public InternalException(string message): base(message) {}
}

{{ config.access_modifier() }} class InvalidEnumException: InternalException {
    public InvalidEnumException(string message): base(message) {
    }
}

{{ config.access_modifier() }} class UniffiContractVersionException: UniffiException {
    public UniffiContractVersionException(string message): base(message) {
    }
}

{{ config.access_modifier() }} class UniffiContractChecksumException: UniffiException {
    public UniffiContractChecksumException(string message): base(message) {
    }
}

// Each top-level error class has a companion object that can lift the error from the call status's rust buffer
interface CallStatusErrorHandler<E> where E: System.Exception {
    E Lift(RustBuffer error_buf);
}

// CallStatusErrorHandler implementation for times when we don't expect a CALL_ERROR
class NullCallStatusErrorHandler: CallStatusErrorHandler<UniffiException> {
    public static NullCallStatusErrorHandler INSTANCE = new NullCallStatusErrorHandler();

    public UniffiException Lift(RustBuffer error_buf) {
        RustBuffer.Free(error_buf);
        return new UndeclaredErrorException("library has returned an error not declared in UNIFFI interface file");
    }
}

// Helpers for calling Rust
// In practice we usually need to be synchronized to call this safely, so it doesn't
// synchronize itself
class _UniffiHelpers {
    public delegate void RustCallAction(ref UniffiRustCallStatus status);
    public delegate U RustCallFunc<out U>(ref UniffiRustCallStatus status);

    // Call a rust function that returns a Result<>.  Pass in the Error class companion that corresponds to the Err
    public static U RustCallWithError<U, E>(CallStatusErrorHandler<E> errorHandler, RustCallFunc<U> callback)
        where E: UniffiException
    {
        var status = new UniffiRustCallStatus();
        var return_value = callback(ref status);
        if (status.IsSuccess()) {
            return return_value;
        } else if (status.IsError()) {
            throw errorHandler.Lift(status.error_buf);
        } else if (status.IsPanic()) {
            // when the rust code sees a panic, it tries to construct a rustbuffer
            // with the message.  but if that code panics, then it just sends back
            // an empty buffer.
            if (status.error_buf.len > 0) {
                throw new PanicException({{ Type::String.borrow()|lift_fn }}(status.error_buf));
            } else {
                throw new PanicException("Rust panic");
            }
        } else {
            throw new InternalException($"Unknown rust call status: {status.code}");
        }
    }

    // Call a rust function that returns a Result<>.  Pass in the Error class companion that corresponds to the Err
    public static void RustCallWithError<E>(CallStatusErrorHandler<E> errorHandler, RustCallAction callback)
        where E: UniffiException
    {
        _UniffiHelpers.RustCallWithError(errorHandler, (ref UniffiRustCallStatus status) => {
            callback(ref status);
            return 0;
        });
    }

    // Call a rust function that returns a plain value
    public static U RustCall<U>(RustCallFunc<U> callback) {
        return _UniffiHelpers.RustCallWithError(NullCallStatusErrorHandler.INSTANCE, callback);
    }

    // Call a rust function that returns a plain value
    public static void RustCall(RustCallAction callback) {
        _UniffiHelpers.RustCall((ref UniffiRustCallStatus status) => {
            callback(ref status);
            return 0;
        });
    }
}

static class FFIObjectUtil {
    public static void DisposeAll(params Object?[] list) {
        Dispose(list);
    }

    // Dispose is implemented by recursive type inspection at runtime. This is because
    // generating correct Dispose calls for recursive complex types, e.g. List<List<int>>
    // is quite cumbersome.
   private static void Dispose(Object? obj) {
         if (obj == null) {
             return;
         }

         if (obj is IDisposable disposable) {
             disposable.Dispose();
             return;
         }

         var objType = obj.GetType();
         var typeCode = Type.GetTypeCode(objType);
         if (typeCode != TypeCode.Object) {
             return;
         }

         var genericArguments = objType.GetGenericArguments();
         if (genericArguments.Length == 0 && !objType.IsArray) {
             return;
         }

         if (obj is System.Collections.IDictionary objDictionary) {
             //This extra code tests to not call "Dispose" for a Dictionary<something, double>()
             //for all values as "double" and alike doesn't support interface "IDisposable"
             var valuesType = objType.GetGenericArguments()[1];
             var elementValuesTypeCode = Type.GetTypeCode(valuesType);
             if (elementValuesTypeCode != TypeCode.Object) {
                 return;
             }
             foreach (var value in objDictionary.Values) {
                 Dispose(value);
             }
         }
         else if (obj is System.Collections.IEnumerable listValues) {
             //This extra code tests to not call "Dispose" for a List<int>()
             //for all keys as "int" and alike doesn't support interface "IDisposable"
             var elementType = objType.IsArray ? objType.GetElementType() : genericArguments[0];
             var elementValuesTypeCode = Type.GetTypeCode(elementType);
             if (elementValuesTypeCode != TypeCode.Object) {
                 return;
             }
             foreach (var value in listValues) {
                 Dispose(value);
             }
         }
     }
}
