#nullable enable

using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class GeneratedPageRedactionTests
{
    private static PortableDocument GeneratedImagePage()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Margins.SetAll(Unit.FromPoint(0));
        var image = section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);
        return new DocumentRenderer().Render(document);
    }

    private static PortableDocument GeneratedTextPage(string text)
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, "Helvetica", 24);
        return new DocumentRenderer().Render(document);
    }

    [Fact]
    public void Redact_RegionOverImageOnGeneratedPage_RemovesImageBytesFromOutput()
    {
        var jpeg = Encoding.Latin1.GetString(PdfTestResources.ReadAllBytes("Images/rgb.jpg"));
        Carries("unredacted emission", jpeg, Emit(GeneratedImagePage()));

        var document = GeneratedImagePage();
        var page = document.Pages[0];
        var whole = PdfRect.FromSize(0, 0, page.Width.Point, page.Height.Point);

        page.Redact(new[] { whole }, new RedactionOptions { FillColor = Color.Black });

        var redacted = Emit(document);
        Lacks("redacted emission", jpeg, redacted);

        using var buffer = new MemoryStream(Encoding.Latin1.GetBytes(redacted));
        PortableDocument.LoadFromStream(buffer);
    }

    [Fact]
    public void RedactText_OnGeneratedPage_RemovesTextAfterReload()
    {
        var document = GeneratedTextPage("SENSITIVE");
        var page = document.Pages[0];

        var count = page.RedactText("SENSITIVE", null, new RedactionOptions { FillColor = Color.Black });

        Assert.Equal(1, count);
        using var buffer = new MemoryStream(document.ToArray());
        var reloaded = PortableDocument.LoadFromStream(buffer);
        Assert.DoesNotContain("SENSITIVE", reloaded.Pages[0].ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_RegionMissingAllContentOnGeneratedPage_PaintsOverlayWithoutFailing()
    {
        var document = GeneratedImagePage();
        var page = document.Pages[0];

        page.Redact(new[] { PdfRect.FromSize(page.Width.Point + 10, page.Height.Point + 10, 5, 5) },
            new RedactionOptions { FillColor = Color.Black });

        Assert.Contains(page.Content, e => e is PathContent { Fill: true });
        using var buffer = new MemoryStream(document.ToArray());
        PortableDocument.LoadFromStream(buffer);
    }
}
