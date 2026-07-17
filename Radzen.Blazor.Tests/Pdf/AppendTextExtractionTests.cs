#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

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
