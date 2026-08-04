#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class DeterministicDocumentIdAuthoredContentTests
{
    private static string Id(PortableDocument document)
    {
        var emission = Emit(document);
        var match = Shaped(
            "trailer /ID",
            @"/ID \[\(([0-9A-Fa-f]{32})\) \([0-9A-Fa-f]{32}\)\]",
            Line(emission, "/ID ["));
        return match.Groups[1].Value;
    }

    private static PortableDocument Authored(string text)
    {
        var document = new PortableDocument { IncludeDocumentId = true };
        var page = document.Pages.Add(PageSizes.A4);
        page.Content.Add(new TextContent(text, Unit.FromPoint(72), Unit.FromPoint(720)));
        return document;
    }

    [Fact]
    public void DifferentAuthoredContent_ProducesDifferentId()
    {
        Assert.NotEqual(Id(Authored("alpha")), Id(Authored("beta")));
    }

    [Fact]
    public void SameAuthoredContent_ProducesSameId()
    {
        Assert.Equal(Id(Authored("gamma")), Id(Authored("gamma")));
    }
}
