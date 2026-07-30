#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class WatermarkFontCapabilityTests
{
    private static Watermark Registered() =>
        new Watermark { Text = "AVATAR", Font = { Family = "Liberation Sans", Size = 60 } };

    private static Document Builder()
    {
        var document = new Document();
        document.Fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return document;
    }

    [Fact]
    public void AuthoredPageEmbedsRegisteredWatermarkFont()
    {
        var document = Builder();
        var section = document.Sections.Add();
        section.Watermark = Registered();
        section.Blocks.Add(new Paragraph());

        Assert.Null(Record.Exception(() => new DocumentRenderer().Render(document)));
    }

    [Fact]
    public void BuiltDocumentRejectsRegisteredWatermarkFont()
    {
        var document = Builder();
        document.Sections.Add().Blocks.Add(new Paragraph());
        var pdf = new DocumentRenderer().Render(document);

        pdf.AddWatermark(Registered());

        var error = Assert.Throws<NotSupportedException>(() => pdf.ToArray());
        Assert.Contains("cannot embed", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadedDocumentRejectsRegisteredWatermarkFont()
    {
        var document = Builder();
        document.Sections.Add().Blocks.Add(new Paragraph());
        using var stream = new MemoryStream(new DocumentRenderer().Render(document).ToArray());
        var pdf = PortableDocument.LoadFromStream(stream);

        pdf.AddWatermark(Registered());

        Assert.Throws<NotSupportedException>(() => pdf.ToArray());
    }
}
