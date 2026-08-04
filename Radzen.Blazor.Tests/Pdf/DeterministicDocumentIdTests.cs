#nullable enable
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class DeterministicDocumentIdTests
{
    private static PortableDocument PlainDocument()
    {
        var document = new PortableDocument { IncludeDocumentId = true };
        document.Info.Title = "Deterministic";
        document.Pages.Add(PageSizes.A4).SetContent(TestBytes.Ascii("BT (hello) Tj ET"));
        return document;
    }

    private static string[] Id(PortableDocument document)
    {
        var emission = Emit(document);
        var match = Shaped(
            "trailer /ID",
            @"/ID \[\(([0-9A-Fa-f]{32})\) \(([0-9A-Fa-f]{32})\)\]",
            Line(emission, "/ID ["));
        return [match.Groups[1].Value, match.Groups[2].Value];
    }

    [Fact]
    public void EverySavedDocument_HasTrailerId_WithTwoEqual32HexHalves()
    {
        var id = Id(PlainDocument());
        Assert.Equal(id[0], id[1]);
    }

    [Fact]
    public void DocumentId_IsDeterministic_AcrossIndependentSaves()
    {
        Assert.Equal(Id(PlainDocument())[0], Id(PlainDocument())[0]);
    }

    [Fact]
    public void DocumentId_DependsOnContent()
    {
        var a = new PortableDocument { IncludeDocumentId = true };
        a.Pages.Add(PageSizes.A4).SetContent(TestBytes.Ascii("BT (alpha) Tj ET"));

        var b = new PortableDocument { IncludeDocumentId = true };
        b.Pages.Add(PageSizes.A4).SetContent(TestBytes.Ascii("BT (beta) Tj ET"));

        Assert.NotEqual(Id(a)[0], Id(b)[0]);
    }

    [Fact]
    public void PlainSave_StaysByteIdentical_AcrossTwoBuilds()
    {
        Assert.Equal(PlainDocument().ToArray(), PlainDocument().ToArray());
    }

    [Fact]
    public void DocumentId_HasAPinnedValue_ForAKnownDocument()
    {
        Assert.Equal("FB42652C3E52C1AECAC2A39EFA11EC9D", Id(PlainDocument())[0]);
    }
}
