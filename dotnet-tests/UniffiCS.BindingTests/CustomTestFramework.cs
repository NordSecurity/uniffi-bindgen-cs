// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(DiagnosticTestPipelineStartup))]

public sealed class DiagnosticTestStartEndAttribute : BeforeAfterTestAttribute
{
    private readonly Stopwatch _stopwatch = new();

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        _stopwatch.Restart();
        TestContext.Current.SendDiagnosticMessage("STARTED: {0}.{1}", test.TestCase.TestClassName, test.TestCase.TestMethodName);
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        _stopwatch.Stop();
        TestContext.Current.SendDiagnosticMessage("FINISHED: {0}.{1} ({2:F3}s)", test.TestCase.TestClassName, test.TestCase.TestMethodName, _stopwatch.Elapsed.TotalSeconds);
    }
}

public sealed class DiagnosticTestPipelineStartup : ITestPipelineStartup
{
    public ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        diagnosticMessageSink.OnMessage(new DiagnosticMessage { Message = "Using DiagnosticTestPipelineStartup" });
        return default;
    }

    public ValueTask StopAsync() => default;
}
