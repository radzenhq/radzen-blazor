#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Appending a document produced by DocumentBuilder must carry each generated page's
// text-extraction fonts, otherwise its Type0/Identity-H content cannot be reversed to
// Unicode and ExtractText() returns garbage for the merged pages.
public class AppendTextExtractionTests
{
    [Fact]
    public void Append_GeneratedPage_CarriesTextExtractionFonts()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Hello Append", BuildTestSupport.Latin);

        var built = builder.Build();
        Assert.Contains("Hello Append", built.ExtractText());

        var merged = new Document();
        merged.Append(built);

        Assert.Contains("Hello Append", merged.ExtractText());
    }
}
