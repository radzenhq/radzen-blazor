using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormatTests
{
    [Fact]
    public void Clone_CopiesNumberFormat()
    {
        var format = new Format { NumberFormat = "#,##0.00" };
        var clone = format.Clone();
        Assert.Equal("#,##0.00", clone.NumberFormat);
    }

    [Fact]
    public void Merge_OverlaysNumberFormat()
    {
        var baseFormat = new Format { NumberFormat = "0.00" };
        var overlay = new Format { NumberFormat = "#,##0" };
        var merged = baseFormat.Merge(overlay);
        Assert.Equal("#,##0", merged.NumberFormat);
    }

    [Fact]
    public void Merge_PreservesNumberFormat_WhenOverlayIsNull()
    {
        var baseFormat = new Format { NumberFormat = "0.00" };
        var overlay = new Format();
        var merged = baseFormat.Merge(overlay);
        Assert.Equal("0.00", merged.NumberFormat);
    }

    [Fact]
    public void WithNumberFormat_ReturnsCloneWithFormat()
    {
        var format = new Format { Bold = true };
        var result = format.WithNumberFormat("#,##0.00");
        Assert.Equal("#,##0.00", result.NumberFormat);
        Assert.True(result.Bold);
        Assert.Null(format.NumberFormat); // original unchanged
    }

    [Fact]
    public void NumberFormat_ChangedEvent_Fires()
    {
        var format = new Format();
        var fired = false;
        format.Changed += () => fired = true;
        format.NumberFormat = "0%";
        Assert.True(fired);
    }

    [Fact]
    public void NumberFormat_ChangedEvent_DoesNotFire_WhenSameValue()
    {
        var format = new Format { NumberFormat = "0%" };
        var fired = false;
        format.Changed += () => fired = true;
        format.NumberFormat = "0%";
        Assert.False(fired);
    }
}
