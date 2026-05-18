// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using uniffi.issue_165;

namespace UniffiCS.BindingTests;

class CSharpAsyncCallback : AsyncCallback
{
    public async Task<string> DoAsync(string @value)
    {
        await Task.Delay(1);
        return @value.ToUpper();
    }

    public async Task DoAsyncVoid(string @value)
    {
        await Task.Delay(1);
    }

    public async Task<string> DoAsyncThrows(string @value)
    {
        await Task.Delay(1);
        if (@value == "throw")
        {
            throw new AsyncCallbackException.Unexpected();
        }
        return @value.ToUpper();
    }

    public async Task DoAsyncVoidThrows(string @value)
    {
        await Task.Delay(1);
        if (@value == "throw")
        {
            throw new AsyncCallbackException.Unexpected();
        }
    }
}

public class TestAsyncCallbackInterface
{
    [Fact]
    public async Task TestAsyncReturn()
    {
        var callback = new CSharpAsyncCallback();
        var result = await Issue165Methods.CallDoAsync(callback, "hello");
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public async Task TestAsyncVoid()
    {
        var callback = new CSharpAsyncCallback();
        await Issue165Methods.CallDoAsyncVoid(callback, "hello");
    }

    [Fact]
    public async Task TestAsyncThrows()
    {
        var callback = new CSharpAsyncCallback();
        var result = await Issue165Methods.CallDoAsyncThrows(callback, "hello");
        Assert.Equal("HELLO", result);

        await Assert.ThrowsAsync<AsyncCallbackException.Unexpected>(
            () => Issue165Methods.CallDoAsyncThrows(callback, "throw")
        );
    }

    [Fact]
    public async Task TestAsyncVoidThrows()
    {
        var callback = new CSharpAsyncCallback();
        await Issue165Methods.CallDoAsyncVoidThrows(callback, "hello");

        await Assert.ThrowsAsync<AsyncCallbackException.Unexpected>(
            () => Issue165Methods.CallDoAsyncVoidThrows(callback, "throw")
        );
    }
}
