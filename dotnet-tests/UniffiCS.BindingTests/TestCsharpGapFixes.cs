using uniffi.csharp_gap_fixes;

namespace UniffiCS.BindingTests;

public class TestCsharpGapFixes
{
    [Fact]
    public void MethodNameConflictsAndNestedJaggedArraysWork()
    {
        var monitor = new Monitor();
        Assert.Equal("monitor", monitor.MonitorClassMethod());
        Assert.Equal("monitor_method", monitor.MonitorMethod());

        var nested = CsharpGapFixesMethods.NestedBytes();
        Assert.Equal(2, nested.Length);

        Assert.Equal(2, nested[0].Length);
        Assert.Equal(new byte[] { 1, 2 }, nested[0][0]);
        Assert.Equal(new byte[] { 3 }, nested[0][1]);

        Assert.Single(nested[1]);
        Assert.Equal(new byte[] { 4, 5, 6 }, nested[1][0]);
    }
}
