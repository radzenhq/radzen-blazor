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
    public void BuiltDocumentRejectsRegisteredWatermarkFontAtTheCall()
    {
        var document = Builder();
        document.Sections.Add().Blocks.Add(new Paragraph());
        var pdf = new DocumentRenderer().Render(document);

        var error = Assert.Throws<NotSupportedException>(() => pdf.AddWatermark(Registered()));

        Assert.Contains("cannot embed", error.Message, StringComparison.Ordinal);
        Assert.Null(Record.Exception(() => pdf.ToArray()));
    }

    [Fact]
    public void LoadedDocumentRejectsRegisteredWatermarkFontAtTheCall()
    {
        var document = Builder();
        document.Sections.Add().Blocks.Add(new Paragraph());
        using var stream = new MemoryStream(new DocumentRenderer().Render(document).ToArray());
        var pdf = PortableDocument.LoadFromStream(stream);

        Assert.Throws<NotSupportedException>(() => pdf.AddWatermark(Registered()));
    }

    [Fact]
    public void BuiltDocumentAcceptsABase14WatermarkFont()
    {
        var document = Builder();
        document.Sections.Add().Blocks.Add(new Paragraph());
        var pdf = new DocumentRenderer().Render(document);

        pdf.AddWatermark(new Watermark { Text = "DRAFT", Font = { Family = "Helvetica", Size = 60 } });

        Assert.Null(Record.Exception(() => pdf.ToArray()));
    }

    [Fact]
    public void ImageOnlyWatermarkIsAcceptedWhateverTheFont()
    {
        var document = Builder();
        document.Sections.Add().Blocks.Add(new Paragraph());
        var pdf = new DocumentRenderer().Render(document);

        var watermark = new Watermark { Font = { Family = "Liberation Sans" } };
        watermark.SetImage(new MemoryStream(ImageTestHelpers.OneBitGrayPng(4, 4)));

        Assert.Null(Record.Exception(() => pdf.AddWatermark(watermark)));
    }
}
