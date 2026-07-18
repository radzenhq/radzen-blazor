#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class GeneratedPageRedactionTests
{
    private static Document GeneratedImagePage()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Margin = Unit.FromPoint(0);
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);
        return builder.Build();
    }

    [Fact]
    public void Redact_RegionOverImageOnGeneratedPage_FailsLoudInsteadOfLeavingImageRecoverable()
    {
        var document = GeneratedImagePage();
        var page = document.Pages[0];
        var whole = PdfRect.FromSize(0, 0, page.Width.Point, page.Height.Point);

        var exception = Assert.Throws<NotSupportedException>(
            () => page.Redact(new[] { whole }, new RedactionOptions { FillColor = Color.Black }));

        Assert.Contains("A redaction region intersects", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_RegionMissingAllContentOnGeneratedPage_PaintsOverlayWithoutFailing()
    {
        var document = GeneratedImagePage();
        var page = document.Pages[0];

        page.Redact(new[] { PdfRect.FromSize(page.Width.Point + 10, page.Height.Point + 10, 5, 5) },
            new RedactionOptions { FillColor = Color.Black });

        using var buffer = new MemoryStream(document.ToArray());
        Document.LoadFromStream(buffer);
    }
}
