#nullable enable
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendTextExtractionTests
{
    [Fact]
    public void Append_GeneratedPage_CarriesTextExtractionFonts()
    {
        var document = new Radzen.Documents.Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello Append", BuildTestSupport.Latin);

        var built = new DocumentRenderer().Render(document);
        Assert.Contains("Hello Append", built.ExtractText());

        var merged = new Document();
        merged.Append(built);

        Assert.Contains("Hello Append", merged.ExtractText());
    }
}
