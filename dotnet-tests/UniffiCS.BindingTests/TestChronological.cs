// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading;
using uniffi.chronological;

namespace UniffiCS.BindingTests;

public class TestChronological
{
    static DateTime EpochSecond(int seconds, int nanoseconds)
    {
        return DateTime.UnixEpoch.AddSeconds(seconds).AddTicks(nanoseconds / 100);
    }

    static TimeSpan TimeSpanSecond(int seconds, int nanoseconds)
    {
        return new TimeSpan(seconds * TimeSpan.TicksPerSecond + nanoseconds / 100);
    }

    [Fact]
    public void ChronologicalWorks()
    {
        // Test passing timestamp and duration while returning timestamp
        Assert.Equal(EpochSecond(101, 200), ChronologicalMethods.Add(EpochSecond(100, 100), TimeSpanSecond(1, 100)));

        // Test passing timestamp while returning duration
        Assert.Equal(TimeSpanSecond(1, 100), ChronologicalMethods.Diff(EpochSecond(101, 200), EpochSecond(100, 100)));

        Assert.Throws<ChronologicalException.TimeDiffException>(() =>
        {
            ChronologicalMethods.Diff(EpochSecond(100, 0), EpochSecond(101, 0));
        });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            ChronologicalMethods.Add(DateTime.MaxValue, TimeSpan.MaxValue);
        });
    }

    [Fact]
    public void DateTimeMinMax()
    {
        Assert.Equal(DateTime.MinValue, ChronologicalMethods.ReturnTimestamp(DateTime.MinValue));

        Assert.Equal(DateTime.MaxValue.ToUniversalTime(), ChronologicalMethods.ReturnTimestamp(DateTime.MaxValue));
    }

    [Fact]
    public void TimeSpanMax()
    {
        // Rust does not allow negative timespan, so only maximum value is tested.

        Assert.Equal(TimeSpan.MaxValue, ChronologicalMethods.ReturnDuration(TimeSpan.MaxValue));
    }

    [Fact]
    public void PreEpochTimestampsSerializeCorrectly()
    {
        Assert.Equal(
            "1969-12-12T00:00:00.000000000Z",
            ChronologicalMethods.ToStringTimestamp(DateTime.Parse("1969-12-12T00:00:00.000000000Z"))
        );

        // [-999_999_999; 0) is unrepresentable
        // https://github.com/mozilla/uniffi-rs/issues/1433
        // Assert.Equal(
        //     "1969-12-31T23:59:59.999999900Z",
        //     ChronologicalMethods.ToStringTimestamp(
        //         DateTime.Parse("1969-12-31T23:59:59.999999900Z")));

        Assert.Equal(
            "1969-12-31T23:59:58.999999900Z",
            ChronologicalMethods.ToStringTimestamp(DateTime.Parse("1969-12-31T23:59:58.999999900Z"))
        );

        Assert.Equal(
            DateTime.Parse("1955-11-05T00:06:01.283000200Z").ToUniversalTime(),
            ChronologicalMethods.Add(DateTime.Parse("1955-11-05T00:06:00.283000100Z"), TimeSpanSecond(1, 100))
        );
    }

    [Fact]
    public void TestDateTimeWorksLikeRustSystemTime()
    {
        // Sleep inbetween to make sure that the clock has enough resolution
        var before = DateTime.UtcNow;
        Thread.Sleep(1);
        var now = ChronologicalMethods.Now();
        Thread.Sleep(1);
        var after = DateTime.UtcNow;
        Assert.Equal(-1, before.CompareTo(now));
        Assert.Equal(1, after.CompareTo(now));
    }

    [Fact]
    public void DateTimeAndTimeSpanOptionals()
    {
        Assert.True(ChronologicalMethods.Optional(DateTime.MaxValue, TimeSpanSecond(0, 0)));
        Assert.False(ChronologicalMethods.Optional(null, TimeSpanSecond(0, 0)));
        Assert.False(ChronologicalMethods.Optional(DateTime.MaxValue, null));
    }
}
