// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UniffiCS.BindingTests;

public class TestIl2cppCallbacks
{
    // The generated code keeps every delegate it hands to native code in a static field.
    [Fact]
    public void NativeCallbacksAreMarkedForIl2cpp()
    {
        var fields = typeof(uniffi.callbacks.UniffiException).Assembly
            .GetTypes()
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute)))
            .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(field => typeof(Delegate).IsAssignableFrom(field.FieldType))
            .ToList();
        Assert.NotEmpty(fields);

        foreach (var field in fields)
        {
            var method = ((Delegate)field.GetValue(null)!).Method;
            var attribute = method.CustomAttributes.SingleOrDefault(a => a.AttributeType.Name == "MonoPInvokeCallbackAttribute");
            Assert.True(attribute != null, $"{field.DeclaringType}.{method.Name} lacks [MonoPInvokeCallback]");
            Assert.Equal(field.FieldType, attribute!.ConstructorArguments.Single().Value);
        }
    }
}
