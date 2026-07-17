#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

public class WatermarkFontCapabilityTests
{
    private static Watermark Registered() =>
        new Watermark { Text = "AVATAR", Font = { Name = "Liberation Sans", Size = 60 } };

    private static DocumentBuilder Builder()
    {
        var builder = new DocumentBuilder();
        builder.Fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return builder;
    }

    [Fact]
    public void AuthoredPageEmbedsRegisteredWatermarkFont()
    {
        var builder = Builder();
        var section = builder.Sections.Add();
        section.Watermark = Registered();
        section.Blocks.Add(new Paragraph());

        Assert.Null(Record.Exception(() => builder.Build()));
    }

    [Fact]
    public void BuiltDocumentRejectsRegisteredWatermarkFont()
    {
        var builder = Builder();
        builder.Sections.Add().Blocks.Add(new Paragraph());
        var document = builder.Build();

        document.AddWatermark(Registered());

        var error = Assert.Throws<NotSupportedException>(() => document.ToArray());
        Assert.Contains("cannot embed", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadedDocumentRejectsRegisteredWatermarkFont()
    {
        var builder = Builder();
        builder.Sections.Add().Blocks.Add(new Paragraph());
        using var stream = new MemoryStream(builder.Build().ToArray());
        var document = Document.LoadFromStream(stream);

        document.AddWatermark(Registered());

        Assert.Throws<NotSupportedException>(() => document.ToArray());
    }
}
