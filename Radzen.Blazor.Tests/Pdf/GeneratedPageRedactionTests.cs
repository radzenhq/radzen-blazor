#nullable enable

using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class GeneratedPageRedactionTests
{
    private static Document GeneratedImagePage()
    {
        var document = new Radzen.Documents.Document();
        var section = document.Sections.Add();
        section.Margins.SetAll(Unit.FromPoint(0));
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);
        return new DocumentRenderer().Render(document);
    }

    private static Document GeneratedTextPage(string text)
    {
        var document = new Radzen.Documents.Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, "Helvetica", 24);
        return new DocumentRenderer().Render(document);
    }

    private static bool Contains(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void Redact_RegionOverImageOnGeneratedPage_RemovesImageBytesFromOutput()
    {
        var jpeg = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        Assert.True(Contains(GeneratedImagePage().ToArray(), jpeg));

        var document = GeneratedImagePage();
        var page = document.Pages[0];
        var whole = PdfRect.FromSize(0, 0, page.Width.Point, page.Height.Point);

        page.Redact(new[] { whole }, new RedactionOptions { FillColor = Color.Black });

        var redacted = document.ToArray();
        Assert.False(Contains(redacted, jpeg));
        using var buffer = new MemoryStream(redacted);
        Document.LoadFromStream(buffer);
    }

    [Fact]
    public void RedactText_OnGeneratedPage_RemovesTextAfterReload()
    {
        var document = GeneratedTextPage("SENSITIVE");
        var page = document.Pages[0];

        var count = page.RedactText("SENSITIVE", null, new RedactionOptions { FillColor = Color.Black });

        Assert.Equal(1, count);
        using var buffer = new MemoryStream(document.ToArray());
        var reloaded = Document.LoadFromStream(buffer);
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
        Document.LoadFromStream(buffer);
    }
}
