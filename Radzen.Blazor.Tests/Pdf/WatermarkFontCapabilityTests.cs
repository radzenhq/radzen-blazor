#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// The same Watermark object, naming the same registered family, is accepted by the authored
// (section) path and rejected by the post-build Document.AddWatermark path. That asymmetry is
// the documented CanEmbed boundary of every overlay stream, not a watermark-specific gap.
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

    // The capability gap. Same public Watermark, same family, same document's fonts.
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

    // A loaded document has no font registry at all, so the same family is simply unknown.
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
