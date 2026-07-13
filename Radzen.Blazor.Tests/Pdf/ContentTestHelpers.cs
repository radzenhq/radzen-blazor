#nullable enable

using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

internal static class ContentTestHelpers
{
    public static DocumentReader Reload(Document document) => DocumentReader.Parse(document.ToArray());

    public static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    public static DictionaryObject PagesNode(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Pages"]));

    public static DictionaryObject Kid(DocumentReader reader, int index)
        => PdfPageContentTestHelper.PageLeaves(reader, assertStructure: true)[index].Page;

    public static byte[] PageContent(DocumentReader reader, int index)
    {
        return PdfPageContentTestHelper.Content(
            reader, Kid(reader, index), assertStreams: true, appendSeparatorAfterEveryStream: true);
    }

    public static DictionaryObject FontResource(DocumentReader reader, int pageIndex, string resourceName)
    {
        var page = Kid(reader, pageIndex);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
        var key = resourceName.StartsWith('/') ? resourceName[1..] : resourceName;
        return Assert.IsType<DictionaryObject>(reader.Resolve(fonts[key]));
    }
}
