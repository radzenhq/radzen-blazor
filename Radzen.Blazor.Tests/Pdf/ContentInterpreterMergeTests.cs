#nullable enable

using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class ContentInterpreterMergeTests
{
    private static ContentCollection Materialize(string stream)
    {
        var target = new ContentCollection();
        ContentInterpreter.Materialize(TestBytes.Ascii(stream), target);
        return target;
    }

    private static string Emit(ContentElement element)
    {
        using var writer = new ContentWriter();
        element.Emit(writer);
        return Encoding.Latin1.GetString(writer.ToArray());
    }

    [Fact]
    public void ConsecutiveShows_NoPositionBetween_FoldIntoOneRun()
    {
        var content = Materialize("BT /F1 12 Tf 10 700 Td (Hello) Tj ( world) Tj ET");

        var text = Assert.IsType<TextContent>(Assert.Single(content));
        Assert.Equal("Hello world", text.Text);
    }

    [Fact]
    public void FoldedRun_ReEmitsAsSingleShow_KeepingBothChunks()
    {
        var content = Materialize("BT /F1 12 Tf 10 700 Td (Hello) Tj ( world) Tj ET");

        var body = Emit(content[0]);

        Assert.Equal(1, body.Split("BT").Length - 1);
        Assert.Contains("(Hello)", body);
        Assert.Contains("( world)", body);
        Assert.Contains("] TJ", body);
    }

    [Fact]
    public void ConsecutiveShows_WithTdBetween_StaySeparateRuns()
    {
        var content = Materialize("BT /F1 12 Tf 10 700 Td (Hello) Tj 0 -14 Td (world) Tj ET");

        Assert.Equal(2, content.Count);
        Assert.Equal("Hello", Assert.IsType<TextContent>(content[0]).Text);
        Assert.Equal("world", Assert.IsType<TextContent>(content[1]).Text);
    }

    [Fact]
    public void ShowsAtDifferentColors_DoNotFold()
    {
        var content = Materialize("BT /F1 12 Tf 10 700 Td 1 0 0 rg (a) Tj 0 1 0 rg (b) Tj ET");

        Assert.Equal(2, content.Count);
    }

    [Fact]
    public void CmykTextFill_IsCarriedAsFillPaint()
    {
        var content = Materialize("BT /F1 12 Tf 10 700 Td 0 1 0 0 k (x) Tj ET");

        var text = Assert.IsType<TextContent>(Assert.Single(content));
        Assert.True(text.FillPaint.HasValue);
        var paint = text.FillPaint.Value;
        Assert.Equal(DeviceColorKind.Cmyk, paint.Kind);
        Assert.Equal(new[] { 0.0, 1.0, 0.0, 0.0 }, paint.Operands);
    }

    [Fact]
    public void CmykTextFill_ReEmitsAsK_NotRg()
    {
        var content = Materialize("BT /F1 12 Tf 10 700 Td 0 1 0 0 k (x) Tj ET");

        var body = Emit(content[0]);

        Assert.Contains(" k\n", body);
        Assert.DoesNotContain(" rg\n", body);
    }

    [Fact]
    public void DoubleQuoteOperator_MapsWordAndCharSpacing()
    {
        var content = Materialize("BT /F1 12 Tf 10 700 Td 2 1 (word) \" ET");

        var text = Assert.IsType<TextContent>(Assert.Single(content));
        Assert.Equal(2, text.WordSpacing);
        Assert.Equal(1, text.CharSpacing);

        var body = Emit(text);
        Assert.Contains("2 Tw", body);
        Assert.Contains("1 Tc", body);
    }

    [Fact]
    public void DashArray_TakesOnlyTheImmediatelyPrecedingArray()
    {
        var content = Materialize("[(a) -20 (b)] TJ 0 d 0 0 m 10 10 l S");

        var path = Assert.IsType<PathContent>(content.Last());
        Assert.NotNull(path.DashArray);
        Assert.True(path.DashArray!.Value.IsEmpty);
    }
}
