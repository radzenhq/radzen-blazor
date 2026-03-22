using System.IO;
using System.Text;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormatNewFeaturesTests
{
    // Phase 1: Strikethrough
    [Fact]
    public void Clone_CopiesStrikethrough()
    {
        var format = new Format { Strikethrough = true };
        var clone = format.Clone();
        Assert.True(clone.Strikethrough);
    }

    [Fact]
    public void Merge_OverlaysStrikethrough()
    {
        var baseFormat = new Format();
        var overlay = new Format { Strikethrough = true };
        var merged = baseFormat.Merge(overlay);
        Assert.True(merged.Strikethrough);
    }

    [Fact]
    public void AppendStyle_Strikethrough()
    {
        var format = new Format { Strikethrough = true };
        var sb = new StringBuilder();
        format.AppendStyle(sb);
        Assert.Contains("text-decoration: line-through", sb.ToString());
    }

    [Fact]
    public void AppendStyle_UnderlineAndStrikethrough()
    {
        var format = new Format { Underline = true, Strikethrough = true };
        var sb = new StringBuilder();
        format.AppendStyle(sb);
        Assert.Contains("text-decoration: underline line-through", sb.ToString());
    }

    // Phase 2: Font Family + Font Size
    [Fact]
    public void Clone_CopiesFontFamily()
    {
        var format = new Format { FontFamily = "Arial" };
        var clone = format.Clone();
        Assert.Equal("Arial", clone.FontFamily);
    }

    [Fact]
    public void Clone_CopiesFontSize()
    {
        var format = new Format { FontSize = 14 };
        var clone = format.Clone();
        Assert.Equal(14, clone.FontSize);
    }

    [Fact]
    public void Merge_OverlaysFontFamily()
    {
        var baseFormat = new Format { FontFamily = "Arial" };
        var overlay = new Format { FontFamily = "Verdana" };
        var merged = baseFormat.Merge(overlay);
        Assert.Equal("Verdana", merged.FontFamily);
    }

    [Fact]
    public void Merge_PreservesFontFamily_WhenOverlayIsNull()
    {
        var baseFormat = new Format { FontFamily = "Arial" };
        var overlay = new Format();
        var merged = baseFormat.Merge(overlay);
        Assert.Equal("Arial", merged.FontFamily);
    }

    [Fact]
    public void AppendStyle_FontFamily()
    {
        var format = new Format { FontFamily = "Courier New" };
        var sb = new StringBuilder();
        format.AppendStyle(sb);
        Assert.Contains("font-family: Courier New", sb.ToString());
    }

    [Fact]
    public void AppendStyle_FontSize()
    {
        var format = new Format { FontSize = 16 };
        var sb = new StringBuilder();
        format.AppendStyle(sb);
        Assert.Contains("font-size: 16pt", sb.ToString());
    }

    [Fact]
    public void FontFamily_ChangedEvent_Fires()
    {
        var format = new Format();
        var fired = false;
        format.Changed += () => fired = true;
        format.FontFamily = "Arial";
        Assert.True(fired);
    }

    [Fact]
    public void FontSize_ChangedEvent_Fires()
    {
        var format = new Format();
        var fired = false;
        format.Changed += () => fired = true;
        format.FontSize = 14;
        Assert.True(fired);
    }

    // Phase 3: Text Wrap
    [Fact]
    public void Clone_CopiesWrapText()
    {
        var format = new Format { WrapText = true };
        var clone = format.Clone();
        Assert.True(clone.WrapText);
    }

    [Fact]
    public void Merge_OverlaysWrapText()
    {
        var baseFormat = new Format();
        var overlay = new Format { WrapText = true };
        var merged = baseFormat.Merge(overlay);
        Assert.True(merged.WrapText);
    }

    [Fact]
    public void AppendStyle_WrapText()
    {
        var format = new Format { WrapText = true };
        var sb = new StringBuilder();
        format.AppendStyle(sb);
        Assert.Contains("white-space: pre-wrap", sb.ToString());
    }

    [Fact]
    public void AppendStyle_NoWrap_Default()
    {
        var format = new Format();
        var sb = new StringBuilder();
        format.AppendStyle(sb);
        Assert.Contains("white-space: nowrap", sb.ToString());
    }

    // Phase 5: Cell Borders
    [Fact]
    public void Clone_CopiesBorders()
    {
        var format = new Format
        {
            BorderTop = new BorderStyle { Color = "#FF0000", LineStyle = BorderLineStyle.Thick },
            BorderBottom = new BorderStyle { Color = "#00FF00", LineStyle = BorderLineStyle.Dashed }
        };
        var clone = format.Clone();
        Assert.NotNull(clone.BorderTop);
        Assert.Equal("#FF0000", clone.BorderTop.Color);
        Assert.Equal(BorderLineStyle.Thick, clone.BorderTop.LineStyle);
        Assert.NotNull(clone.BorderBottom);
        Assert.Equal(BorderLineStyle.Dashed, clone.BorderBottom.LineStyle);
        Assert.Null(clone.BorderLeft);
        Assert.Null(clone.BorderRight);
    }

    [Fact]
    public void Merge_OverlaysBorders()
    {
        var baseFormat = new Format { BorderTop = new BorderStyle { LineStyle = BorderLineStyle.Thin } };
        var overlay = new Format { BorderBottom = new BorderStyle { LineStyle = BorderLineStyle.Medium } };
        var merged = baseFormat.Merge(overlay);
        Assert.NotNull(merged.BorderTop);
        Assert.NotNull(merged.BorderBottom);
        Assert.Equal(BorderLineStyle.Medium, merged.BorderBottom.LineStyle);
    }

    [Fact]
    public void AppendStyle_Borders()
    {
        var format = new Format
        {
            BorderTop = new BorderStyle { Color = "#000000", LineStyle = BorderLineStyle.Thin }
        };
        var sb = new StringBuilder();
        format.AppendStyle(sb);
        Assert.Contains("border-top: 1px solid #000000", sb.ToString());
    }

    [Fact]
    public void WithBorders_ReturnsCloneWithBorders()
    {
        var format = new Format { Bold = true };
        var border = new BorderStyle { LineStyle = BorderLineStyle.Medium, Color = "#FF0000" };
        var result = format.WithBorders(border, null, border, null);
        Assert.NotNull(result.BorderTop);
        Assert.NotNull(result.BorderBottom);
        Assert.Null(result.BorderRight);
        Assert.True(result.Bold);
    }

    // XLSX round-trip tests
    [Fact]
    public void XlsxRoundTrip_Strikethrough()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 10, 10);
        sheet.Cells[0, 0].Value = "Strike";
        sheet.Cells[0, 0].Format.Strikethrough = true;

        var reimported = RoundTrip(workbook);
        Assert.True(reimported.Sheets[0].Cells[0, 0].Format.Strikethrough);
    }

    [Fact]
    public void XlsxRoundTrip_FontFamilyAndSize()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 10, 10);
        sheet.Cells[0, 0].Value = "Custom";
        sheet.Cells[0, 0].Format.FontFamily = "Arial";
        sheet.Cells[0, 0].Format.FontSize = 16;

        var reimported = RoundTrip(workbook);
        Assert.Equal("Arial", reimported.Sheets[0].Cells[0, 0].Format.FontFamily);
        Assert.Equal(16, reimported.Sheets[0].Cells[0, 0].Format.FontSize);
    }

    [Fact]
    public void XlsxRoundTrip_WrapText()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 10, 10);
        sheet.Cells[0, 0].Value = "Wrapped";
        sheet.Cells[0, 0].Format.WrapText = true;

        var reimported = RoundTrip(workbook);
        Assert.True(reimported.Sheets[0].Cells[0, 0].Format.WrapText);
    }

    [Fact]
    public void XlsxRoundTrip_Borders()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 10, 10);
        sheet.Cells[0, 0].Value = "Bordered";
        sheet.Cells[0, 0].Format.BorderTop = new BorderStyle { Color = "#000000", LineStyle = BorderLineStyle.Thin };
        sheet.Cells[0, 0].Format.BorderBottom = new BorderStyle { Color = "#FF0000", LineStyle = BorderLineStyle.Medium };

        var reimported = RoundTrip(workbook);
        Assert.NotNull(reimported.Sheets[0].Cells[0, 0].Format.BorderTop);
        Assert.Equal(BorderLineStyle.Thin, reimported.Sheets[0].Cells[0, 0].Format.BorderTop!.LineStyle);
        Assert.NotNull(reimported.Sheets[0].Cells[0, 0].Format.BorderBottom);
        Assert.Equal(BorderLineStyle.Medium, reimported.Sheets[0].Cells[0, 0].Format.BorderBottom!.LineStyle);
    }

    [Fact]
    public void XlsxRoundTrip_Hyperlink()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Test", 10, 10);
        sheet.Cells[0, 0].Value = "Click here";
        sheet.Cells[0, 0].Hyperlink = new Hyperlink
        {
            Url = "https://example.com",
            DisplayText = "Click here"
        };

        var reimported = RoundTrip(workbook);
        Assert.NotNull(reimported.Sheets[0].Cells[0, 0].Hyperlink);
        Assert.Equal("https://example.com", reimported.Sheets[0].Cells[0, 0].Hyperlink!.Url);
        Assert.Equal("Click here", reimported.Sheets[0].Cells[0, 0].Hyperlink!.DisplayText);
    }

    private static Workbook RoundTrip(Workbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        return Workbook.LoadFromStream(stream);
    }
}
