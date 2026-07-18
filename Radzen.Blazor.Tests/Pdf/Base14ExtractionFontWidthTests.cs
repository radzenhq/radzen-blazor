#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class Base14ExtractionFontWidthTests
{
    private static DocumentBuilder Base14TextBuilder()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Margin = Unit.FromPoint(0);
        BuildTestSupport.AddText(section, "Hello", "Helvetica");
        return builder;
    }

    [Fact]
    public void FindText_OnFreshlyBuiltBase14Page_MatchesTheReloadedGeometry()
    {
        var builder = Base14TextBuilder();
        var built = builder.Build();

        var generatedHit = Assert.Single(built.Pages[0].FindText("Hello"));

        using var buffer = new MemoryStream(builder.ToArray());
        var reloaded = Document.LoadFromStream(buffer);
        var reloadedHit = Assert.Single(reloaded.Pages[0].FindText("Hello"));

        Assert.False(generatedHit.GeometryEstimated, "base-14 extraction fonts carry AFM widths");
        Assert.Equal(reloadedHit.Bounds.Left, generatedHit.Bounds.Left, 3);
        Assert.Equal(reloadedHit.Bounds.Right, generatedHit.Bounds.Right, 3);
        Assert.Equal(reloadedHit.Bounds.Bottom, generatedHit.Bounds.Bottom, 3);
        Assert.Equal(reloadedHit.Bounds.Top, generatedHit.Bounds.Top, 3);
    }
}
