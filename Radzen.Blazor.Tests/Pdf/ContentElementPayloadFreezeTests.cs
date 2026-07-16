#nullable enable

using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Change detection tracks assignment through a property. A payload handed out as a mutable
// array or list can be written through behind the door, so every array-typed payload is
// frozen at the boundary. The payloads are boxed to object here so each assertion compiles
// against both the array-typed member and its frozen replacement.
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

    // P1: replacing the raw stream must discard elements parsed from the previous bytes,
    // otherwise the stale elements re-emit and the new content is silently dropped.
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

    // P3: the one hole reflection can never see. A caller can cast the IReadOnlyList back to
    // the GradientStop[] the constructor cloned into and write straight through it.
    [Fact]
    public void GradientStops_AreNotWritableThroughTheReturnedReference()
    {
        var brush = new LinearGradient(0, 0, 10, 10,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        // Boxed so the cast stays expressible once Stops is no longer an array-typed reference.
        object stops = brush.Stops;
        if (stops is GradientStop[] array)
        {
            array[0] = new GradientStop(0, Color.Green);
        }

        Assert.Equal(Color.Red, brush.Stops[0].Color);
    }

    // P2: the interpreter assigned its growable merge list onto the run and then kept
    // AddRange-ing into it, so the run's payload stayed an alias the caller can grow.
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
