/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using uniffi.futures;

namespace UniffiCS.BindingTests;

public class TestFutures {
    static async Task<long> MeasureTimeMillis(Func<Task> callback) {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        await callback();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    static async Task ReturnsImmediately(Func<Task> callback) {
        var time = await MeasureTimeMillis(callback);
        AssertApproximateTime(0, 4, time);
    }

    static async Task ReturnsIn(long expected, Func<Task> callback) {
        var time = await MeasureTimeMillis(callback);
        AssertApproximateTime(expected, 50, time);
    }

    static void AssertApproximateTime(long expected, long tolerance, long actual) {
        long difference = Math.Abs(expected - actual);
        Assert.True(difference <= tolerance, $"Expected: {expected}, Tolerance: {tolerance}, Actual: {actual}");
    }

    [Fact]
    public async void TestAlwaysReady() {
        await ReturnsImmediately(async () => {
            Assert.True(await FuturesMethods.AlwaysReady());
        });
    }

    [Fact]
    public async void TestVoid() {
        await ReturnsImmediately(async () => {
            await FuturesMethods.Void();
        });
    }

    [Fact]
    public async void TestSleep() {
        await ReturnsIn(200, async () => {
            await FuturesMethods.Sleep(200);
        });
    }

    [Fact]
    public async void TestSequentialFutures() {
        await ReturnsIn(300, async () => {
            for (int i = 0; i < 10; i++) {
                var result = await FuturesMethods.SayAfter(30, i.ToString());
                Assert.Equal($"Hello, {i}!", result);
            }
        });
    }

    [Fact]
    public async void TestConcurrentFutures() {
        await ReturnsIn(100, async () => {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++) {
                var index = i;
                Func<Task> task = async () => {
                    var result = await FuturesMethods.SayAfter(100, index.ToString());
                    Assert.Equal($"Hello, {index}!", result);
                };
                tasks.Add(task());
            }
            await Task.WhenAll(tasks);
        });
    }

    [Fact]
    public async void TestAsyncMethods() {
        using (var megaphone = FuturesMethods.NewMegaphone()) {
            await ReturnsIn(200, async () => {
                var result = await megaphone.SayAfter(200, "Alice");
                Assert.Equal("HELLO, ALICE!", result);
            });            
        }
    }

    [Fact]
    public async void TestAsyncReturningOptionalObject() {
        var megaphone = await FuturesMethods.AsyncMaybeNewMegaphone(true);
        Assert.NotNull(megaphone);
        if (megaphone != null) {
            megaphone.Dispose();
        }

        megaphone = await FuturesMethods.AsyncMaybeNewMegaphone(false);
        Assert.Null(megaphone);
    }

    [Fact]
    public async void TestAsyncWithTokioRuntime() {
        await ReturnsIn(200, async () => {
            var result = await FuturesMethods.SayAfterWithTokio(200, "Alice");
            Assert.Equal("Hello, Alice (with Tokio)!", result);
        });
    }

    [Fact]
    public async void TestAsyncFallibleFunctions() {
        await ReturnsImmediately(async () => {
            await FuturesMethods.FallibleMe(false);
            await Assert.ThrowsAsync<MyException.Foo>(() => FuturesMethods.FallibleMe(true));
             using (var megaphone = FuturesMethods.NewMegaphone()) {
                Assert.Equal(42, await megaphone.FallibleMe(false));
                await Assert.ThrowsAsync<MyException.Foo>(() => megaphone.FallibleMe(true));
            }
        });
    }

    [Fact]
    public async void TestAsyncFallibleStruct() {
        await ReturnsImmediately(async () => {
            await FuturesMethods.FallibleStruct(false);
            await Assert.ThrowsAsync<MyException.Foo>(() => FuturesMethods.FallibleStruct(true));
        });
    }

    [Fact]
    public async void TestRecord() {
        for (int i = 0; i < 1000; i++) {
            await ReturnsImmediately(async () => {
                var record = await FuturesMethods.NewMyRecord("foo", 42U);
                Assert.Equal("foo", record.a);
                Assert.Equal(42U, record.b);
            });
        }
    }

    [Fact]
    public async void TestBrokenSleep() {
        await ReturnsIn(500, async () => {
            // calls the waker twice immediately
            await FuturesMethods.BrokenSleep(100, 0);
            // wait for possible failure
            await Task.Delay(100);
            // calls the waker a second time after 1s
            await FuturesMethods.BrokenSleep(100, 100);
            // wait for possible failure
            await Task.Delay(200);
        });
    }
}