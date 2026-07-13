#nullable enable
using System.Text;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

internal static class FeatureEmissionTestHelpers
{
    public static string Content(DocumentBuilder builder)
        => Encoding.Latin1.GetString(ContentTestHelpers.PageContent(BuildTestSupport.Read(builder), 0));

    public static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }
}
