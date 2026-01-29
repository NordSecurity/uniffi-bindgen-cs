/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
        AssertApproximateTime(0, 20, time);
    }

    static async Task ReturnsIn(long expected, Func<Task> callback) {
        await ReturnsIn(expected, 100, callback);
    }

    static async Task ReturnsIn(long expected, long tolerance, Func<Task> callback) {
        var time = await MeasureTimeMillis(callback);
        AssertApproximateTime(expected, tolerance, time);
    }

    static void AssertApproximateTime(long expected, long tolerance, long actual) {
        long difference = Math.Abs(expected - actual);
        Assert.True(difference <= tolerance, $"Expected: {expected}, Tolerance: {tolerance}, Actual: {actual}");
    }

    [Fact]
    public async Task TestAlwaysReady() {
        await ReturnsImmediately(async () => {
            Assert.True(await FuturesMethods.AlwaysReady());
        });
    }

    [Fact]
    public async Task TestVoid() {
        await ReturnsImmediately(async () => {
            await FuturesMethods.Void();
        });
    }

    [Fact]
    public async Task TestSleep() {
        await ReturnsIn(200, async () => {
            await FuturesMethods.Sleep(200);
        });
    }

    [Fact]
    public async Task TestSequentialFutures() {
        await ReturnsIn(300, async () => {
            for (int i = 0; i < 10; i++) {
                var result = await FuturesMethods.SayAfter(30, i.ToString());
                Assert.Equal($"Hello, {i}!", result);
            }
        });
    }

    [Fact]
    public async Task TestConcurrentFutures() {
        await ReturnsIn(100, async () => {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++) {
                var index = i;
                var task = Task.Run(async () => {
                    var result = await FuturesMethods.SayAfter(100, index.ToString());
                    Assert.Equal($"Hello, {index}!", result);
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        });
    }

    [Fact]
    public async Task TestAsyncMethods() {
        using (var megaphone = FuturesMethods.NewMegaphone()) {
            await ReturnsIn(200, async () => {
                var result = await megaphone.SayAfter(200, "Alice");
                Assert.Equal("HELLO, ALICE!", result);
            });
        }
    }

    [Fact]
    public async Task TestAsyncConstructors() {
        var megaphone = await Megaphone.Secondary();
        Assert.Equal("HELLO, HI!", await megaphone.SayAfter(1, "hi"));
    }

    [Fact]
    public async Task TestAsyncReturningOptionalObject() {
        var megaphone = await FuturesMethods.AsyncMaybeNewMegaphone(true);
        Assert.NotNull(megaphone);
        if (megaphone != null) {
            megaphone.Dispose();
        }

        megaphone = await FuturesMethods.AsyncMaybeNewMegaphone(false);
        Assert.Null(megaphone);
    }

    [Fact]
    public async Task TestAsyncMethodsInTraits() {
        var traits = FuturesMethods.GetSayAfterTraits();
        var time = await MeasureTimeMillis(async () => {
            var result1 = await traits[0].SayAfter(100, "Alice");
            var result2 = await traits[1].SayAfter(100, "Bob");

            Assert.Equal("Hello, Alice!", result1);
            Assert.Equal("Hello, Bob!", result2);
        });

        AssertApproximateTime(200, 100, time);
    }

    [Fact]
    public async Task TestAsyncMethodsInUdlDefinedTraits() {
        var traits = FuturesMethods.GetSayAfterUdlTraits();
        var time = await MeasureTimeMillis(async () => {
            var result1 = await traits[0].SayAfter(100, "Alice");
            var result2 = await traits[1].SayAfter(100, "Bob");

            Assert.Equal("Hello, Alice!", result1);
            Assert.Equal("Hello, Bob!", result2);
        });

        AssertApproximateTime(200, 100, time);
    }

    class CSharpAsyncParser : AsyncParser {
        public int completedDelays = 0;

        public async Task<String> AsString(int @delayMs, int @value) {
            await Task.Delay(@delayMs);
            return @value.ToString();
        }

        public async Task<int> TryFromString(int @delayMs, String @value) {
            await Task.Delay(@delayMs);

            if (value == "force-unexpected-exception") {
                throw new System.Exception("UnexpectedException");
            }

            try {
                return int.Parse(value);
            } catch (System.FormatException) {
                throw new ParserException.NotAnInt();
            }
        }

        public async Task Delay(int @delayMs) {
            await Task.Delay(@delayMs);
            completedDelays += 1;
        }

        public async Task TryDelay(String @delayMs) {
            try {
                var delayParsed = int.Parse(delayMs);
                await Task.Delay(delayParsed);
                completedDelays += 1;
            } catch (System.FormatException) {
                throw new ParserException.NotAnInt();
            }
        }
    }

    [Fact]
    public async Task TestAsyncParser() {
        var obj = new CSharpAsyncParser();
        Assert.Equal("42", await FuturesMethods.AsStringUsingTrait(obj, 1, 42));
        Assert.Equal(42, await FuturesMethods.TryFromStringUsingTrait(obj, 1, "42"));

        await Assert.ThrowsAsync<ParserException.NotAnInt>(() => FuturesMethods.TryFromStringUsingTrait(obj, 1, "fourty-two"));

        await Assert.ThrowsAsync<ParserException.UnexpectedException>(
            () => FuturesMethods.TryFromStringUsingTrait(obj, 1, "force-unexpected-exception")
        );

        await FuturesMethods.DelayUsingTrait(obj, 1);
        await Assert.ThrowsAsync<ParserException.NotAnInt>(() => FuturesMethods.TryDelayUsingTrait(obj, "one"));

        var completedDelaysBefore = obj.completedDelays;
        await FuturesMethods.CancelDelayUsingTrait(obj, 10);
        Assert.Equal(completedDelaysBefore, obj.completedDelays);
    }


    [Fact]
    public async Task TestAsyncWithTokioRuntime() {
        await ReturnsIn(200, async () => {
            var result = await FuturesMethods.SayAfterWithTokio(200, "Alice");
            Assert.Equal("Hello, Alice (with Tokio)!", result);
        });
    }

    [Fact]
    public async Task TestAsyncFallibleFunctions() {
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
    public async Task TestAsyncFallibleStruct() {
        await ReturnsImmediately(async () => {
            await FuturesMethods.FallibleStruct(false);
            await Assert.ThrowsAsync<MyException.Foo>(() => FuturesMethods.FallibleStruct(true));
        });
    }

    [Fact]
    public async Task TestRecord() {
        for (int i = 0; i < 1000; i++) {
            await ReturnsImmediately(async () => {
                var record = await FuturesMethods.NewMyRecord("foo", 42U);
                Assert.Equal("foo", record.A);
                Assert.Equal(42U, record.B);
            });
        }
    }

    [Fact]
    public async Task TestBrokenSleep() {
        await ReturnsIn(500, 100, async () => {
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

    [Fact]
    public async Task TestFutureWithLock() {
        var time = await MeasureTimeMillis(async () => {
            await FuturesMethods.UseSharedResource(new SharedResourceOptions(100, 100));
            await FuturesMethods.UseSharedResource(new SharedResourceOptions(0, 100));
        });
        AssertApproximateTime(100, 100, time);
    }
}