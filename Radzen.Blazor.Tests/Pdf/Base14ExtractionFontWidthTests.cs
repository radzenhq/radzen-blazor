#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class Base14ExtractionFontWidthTests
{
    private static Document Base14TextBuilder()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Margins.SetAll(Unit.FromPoint(0));
        BuildTestSupport.AddText(section, "Hello", "Helvetica");
        return document;
    }

    [Fact]
    public void FindText_OnFreshlyBuiltBase14Page_MatchesTheReloadedGeometry()
    {
        var document = Base14TextBuilder();
        var built = new DocumentRenderer().Render(document);

        var generatedHit = Assert.Single(built.Pages[0].FindText("Hello"));

        using var buffer = new MemoryStream(new DocumentRenderer().ToArray(document));
        var reloaded = PortableDocument.LoadFromStream(buffer);
        var reloadedHit = Assert.Single(reloaded.Pages[0].FindText("Hello"));

        Assert.False(generatedHit.GeometryEstimated, "base-14 extraction fonts carry AFM widths");
        Assert.Equal(reloadedHit.Bounds.Left, generatedHit.Bounds.Left, 3);
        Assert.Equal(reloadedHit.Bounds.Right, generatedHit.Bounds.Right, 3);
        Assert.Equal(reloadedHit.Bounds.Bottom, generatedHit.Bounds.Bottom, 3);
        Assert.Equal(reloadedHit.Bounds.Top, generatedHit.Bounds.Top, 3);
    }
}
