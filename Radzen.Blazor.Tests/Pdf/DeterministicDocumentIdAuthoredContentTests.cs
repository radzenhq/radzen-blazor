#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class DeterministicDocumentIdAuthoredContentTests
{
    private static string Id(PortableDocument document)
    {
        var reader = DocumentReader.Parse(document.ToArray());
        var id = Assert.IsType<ArrayObject>(reader.Resolve(reader.Trailer["ID"]));
        return Assert.IsType<StringObject>(id[0]).Value;
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
