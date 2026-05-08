/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Threading.Tasks;
using uniffi.issue_165;

namespace UniffiCS.BindingTests;

class CSharpAsyncCallback : AsyncCallback
{
    public async Task<string> DoAsync(string v)
    {
        await Task.Yield();
        return v;
    }

    public async Task DoAsyncVoid(string v)
    {
        await Task.Yield();
    }

    public async Task<string> DoAsyncThrows(string v)
    {
        await Task.Yield();
        if (v == "throw") throw new AsyncCallbackException.Unexpected();
        return v;
    }

    public async Task DoAsyncVoidThrows(string v)
    {
        await Task.Yield();
        if (v == "throw") throw new AsyncCallbackException.Unexpected();
    }
}

public class TestAsyncCallbackInterface
{
    [Fact]
    public async Task TestAsyncReturn()
    {
        var cb = new CSharpAsyncCallback();
        var result = await Issue165Methods.CallDoAsync(cb, "hello");
        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task TestAsyncVoid()
    {
        var cb = new CSharpAsyncCallback();
        await Issue165Methods.CallDoAsyncVoid(cb, "hello");
    }

    [Fact]
    public async Task TestAsyncThrows()
    {
        var cb = new CSharpAsyncCallback();
        await Assert.ThrowsAsync<AsyncCallbackException.Unexpected>(
            () => Issue165Methods.CallDoAsyncThrows(cb, "throw"));
    }

    [Fact]
    public async Task TestAsyncVoidThrows()
    {
        var cb = new CSharpAsyncCallback();
        await Assert.ThrowsAsync<AsyncCallbackException.Unexpected>(
            () => Issue165Methods.CallDoAsyncVoidThrows(cb, "throw"));
    }
}
