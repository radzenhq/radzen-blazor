#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

internal static class FeatureEmissionTestHelpers
{
    public static byte[] ContentBytes(Document document)
        => ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0);

    public static string Content(Document document) => Encoding.Latin1.GetString(ContentBytes(document));

    public static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }
}
