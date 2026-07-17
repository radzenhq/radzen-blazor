#nullable enable

using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentElementPayloadFreezeTests
{
    private const string TextSource =
        "BT\n/F1 12 Tf\n10 700 Td\n[(Hello) -200 (World)] TJ\nET\n";

    private static Page LoadedTextPage()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(TextSource));

        return InterpreterTestSupport.Load(document.ToArray()).Pages[0];
    }

    [Fact]
    public void SetContent_DiscardsElementsMaterializedFromThePreviousBytes()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(TextSource));

        var reloaded = InterpreterTestSupport.Load(document.ToArray()).Pages[0];

        Assert.Single(reloaded.Content);

        reloaded.SetContent(InterpreterTestSupport.Ascii(
            "BT\n/F1 12 Tf\n10 700 Td\n(One) Tj\nET\n" +
            "BT\n/F1 12 Tf\n10 680 Td\n(Two) Tj\nET\n"));

        Assert.Equal(2, reloaded.Content.Count);
    }

    [Fact]
    public void GradientStops_AreNotWritableThroughTheReturnedReference()
    {
        var brush = new LinearGradient(0, 0, 10, 10,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        object stops = brush.Stops;
        if (stops is GradientStop[] array)
        {
            array[0] = new GradientStop(0, Color.Green);
        }

        Assert.Equal(Color.Red, brush.Stops[0].Color);
    }

    [Fact]
    public void SourceAdjustments_AreNotAnAliasedGrowableList()
    {
        var run = Assert.IsType<TextContent>(LoadedTextPage().Content[0]);
        object? adjustments = run.SourceAdjustments;

        Assert.NotNull(adjustments);
        Assert.False(adjustments is List<TextAdjustment>, "SourceAdjustments handed out a growable List the caller can write into.");
    }

    [Fact]
    public void SourceBytes_AreNotWritableThroughTheReturnedReference()
    {
        var run = Assert.IsType<TextContent>(LoadedTextPage().Content[0]);
        object? bytes = run.SourceBytes;

        Assert.NotNull(bytes);
        Assert.False(bytes is byte[], "SourceBytes handed out a writable array.");
    }

    [Fact]
    public void DashArray_IsNotWritableThroughTheReturnedReference()
    {
        var path = new PathContent();
        path.SetDash([3, 2], 0);
        object? dash = path.DashArray;

        Assert.NotNull(dash);
        Assert.False(dash is double[], "DashArray handed out a writable array.");
    }
}
