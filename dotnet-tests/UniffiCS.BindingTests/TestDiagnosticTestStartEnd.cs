// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace UniffiCS.BindingTests;

public class TestDiagnosticTestStartEnd
{
    // Verifies that DiagnosticTestStartEndAttribute resets the stopwatch on each
    // invocation rather than resuming a cumulative one. Because the attribute is
    // applied at assembly scope, a single instance is reused for every test;
    // Restart() is required so each "FINISHED" line reflects only that test's
    // elapsed time.
    [Fact]
    public void StopwatchResetsOnEachInvocation()
    {
        var attr = new DiagnosticTestStartEndAttribute();
        var stopwatchField = typeof(DiagnosticTestStartEndAttribute)
            .GetField("_stopwatch", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var sw = (Stopwatch)stopwatchField.GetValue(attr)!;

        // Simulate first test's Before()/After() cycle by driving the stopwatch
        // directly through reflection — equivalent to calling Before()/After() but
        // avoids needing a real IXunitTest instance.
        sw.Start();
        Thread.Sleep(60);
        sw.Stop();
        var elapsed1 = sw.Elapsed;  // ≥ 60ms

        // Gap between the two simulated tests.
        Thread.Sleep(30);

        // Now simulate what Before() does at the start of the second test.
        // With Start() (bug):    the stopped watch resumes from ~60ms → elapsed2 ≈ elapsed1
        // With Restart() (fix):  the watch resets to 0              → elapsed2 ≈ 0ms
        //
        // We invoke the actual Before() method to exercise the real implementation.
        // Passing null for IXunitTest causes a NullReferenceException only AFTER
        // the stopwatch operation (Start/Restart is the first statement), so we
        // catch that and read the elapsed immediately.
        try { attr.Before(null!, null!); } catch (NullReferenceException) { }
        var elapsed2 = sw.Elapsed;
        sw.Stop(); // stop the clock so it doesn't drift during the assertion

        Assert.True(
            elapsed2 < elapsed1 / 2,
            $"Stopwatch was not reset between invocations. " +
            $"First elapsed: {elapsed1.TotalMilliseconds:F1}ms, " +
            $"Second elapsed immediately after Before(): {elapsed2.TotalMilliseconds:F1}ms. " +
            "Expected Restart() but Start() appears to have been used.");
    }
}
