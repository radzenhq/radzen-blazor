#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

internal static class FeatureEmissionTestHelpers
{
    public static string Content(Document document)
        => Encoding.Latin1.GetString(ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0));

    public static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }
}
