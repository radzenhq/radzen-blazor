#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// P4 load-modify-save round-trip integrity. Pins:
// (a) re-saving a loaded document preserves the source page /Resources
//     (fonts, image XObjects), merged with any newly emitted entries;
// (b) loaded content streams are filter-decoded, so text extraction works on
//     compressed input and a re-save emits a readable (or correctly filtered)
//     content stream instead of raw Flate bytes with the /Filter dropped;
// (c) pages produced by DocumentBuilder.Build() expose their generated content
//     through GetContent/ExtractText/Append like loaded pages do;
// (d) Type0 embedding survives text containing surrogate pairs and lone
//     surrogates instead of crashing in char.ConvertFromUtf32.
public class RoundTripIntegrityTests
{
    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    // ----- (a) source page resources survive a re-save -----

    [Fact]
    public void Resave_LoadedBase14Page_KeepsFontResourcesAndText()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Hello Resave", "Helvetica");

        var resaved = Load(builder.ToArray()).ToArray();

        var reader = DocumentReader.Parse(resaved);
        var leaves = BuildTestSupport.PageLeaves(reader);
        Assert.Single(leaves);
        var resources = leaves[0].Resources;
        Assert.NotNull(resources);
        Assert.True(resources!.ContainsKey("Font"), "re-saved page lost its /Font resources");

        Assert.Contains("Hello Resave", Load(resaved).ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Resave_LoadedType0Page_KeepsEmbeddedFontAndText()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Embedded Survives", BuildTestSupport.Latin);

        var resaved = Load(builder.ToArray()).ToArray();

        Assert.NotEmpty(BuildTestSupport.Type0Fonts(DocumentReader.Parse(resaved)));
        Assert.Contains("Embedded Survives", Load(resaved).ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Resave_LoadedImagePage_KeepsImageXObject()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);

        var original = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        var resaved = Load(builder.ToArray()).ToArray();

        var images = BuildTestSupport.ImageXObjects(DocumentReader.Parse(resaved));
        Assert.Single(images);
        Assert.Equal(original, images[0].Data);
    }

    // ----- (b) filter-decoded content streams -----

    private static byte[] FlateFixture(string text)
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 720 Td (" + text + ") Tj ET");
        byte[] compressed;
        using (var buffer = new MemoryStream())
        {
            using (var zlib = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(content, 0, content.Length);
            }

            compressed = buffer.ToArray();
        }

        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica "
                + "/Encoding /WinAnsiEncoding >>\nendobj\n")
            .Mark(5)
            .Append("5 0 obj\n<< /Length " + compressed.Length + " /Filter /FlateDecode >>\nstream\n")
            .Append(compressed)
            .Append("\nendstream\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void Load_FlateCompressedContent_ExtractsText()
    {
        var document = Load(FlateFixture("Hello Flate"));

        Assert.Equal(1, document.Pages.Count);
        Assert.Contains("Hello Flate", document.ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Resave_FlateCompressedContent_EmitsReadableContentStream()
    {
        var resaved = Load(FlateFixture("Hello Flate")).ToArray();

        var reader = DocumentReader.Parse(resaved);
        var leaves = BuildTestSupport.PageLeaves(reader);
        Assert.Single(leaves);

        // Content() honors a /Filter entry, so this passes whether the fix keeps
        // decoded bytes or re-emits compressed bytes with the filter declared.
        var operators = Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[0].Page));
        Assert.Contains("BT", operators, StringComparison.Ordinal);
        Assert.Contains("(Hello Flate) Tj", operators, StringComparison.Ordinal);
    }

    [Fact]
    public void Resave_FlateCompressedContent_TextSurvivesReload()
    {
        var resaved = Load(FlateFixture("Hello Flate")).ToArray();

        Assert.Contains("Hello Flate", Load(resaved).ExtractText(), StringComparison.Ordinal);
    }

    // ----- (c) generated pages expose their content -----

    private static Document BuildBase14(string text)
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, text, "Helvetica");
        return builder.Build();
    }

    [Fact]
    public void BuiltPage_GetContent_ExposesGeneratedContent()
    {
        var built = BuildBase14("Generated body");

        var content = built.Pages[0].GetContent();
        Assert.NotNull(content);
        Assert.Contains("BT", Encoding.Latin1.GetString(content!), StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractText_OnBuiltDocument_ReturnsText()
    {
        var built = BuildBase14("Generated body");

        Assert.Contains("Generated body", built.ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Append_BuiltDocument_PagesCarryContent()
    {
        var target = new Document();
        target.Append(BuildBase14("Appended body"));

        Assert.Equal(1, target.Pages.Count);

        var reader = DocumentReader.Parse(target.ToArray());
        var leaves = BuildTestSupport.PageLeaves(reader);
        Assert.Single(leaves);

        var operators = Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[0].Page));
        Assert.Contains("BT", operators, StringComparison.Ordinal);
        Assert.Contains("Tj", operators, StringComparison.Ordinal);
    }

    // ----- (d) surrogate handling in Type0 embedding -----

    private static DocumentBuilder BuilderWithLatinText(string text)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, text, BuildTestSupport.Latin);
        return builder;
    }

    [Fact]
    public void Build_TextWithSurrogatePair_DoesNotThrow()
    {
        // U+1F600 (emoji) has no glyph in Liberation Sans; both surrogate halves
        // flow through the gid-to-unicode map that ToUnicode is built from.
        var builder = BuilderWithLatinText("A😀B");

        var bytes = builder.ToArray();

        var text = Load(bytes).ExtractText();
        Assert.Contains("A", text, StringComparison.Ordinal);
        Assert.Contains("B", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_TextWithLoneSurrogate_DoesNotThrow()
    {
        var builder = BuilderWithLatinText("X\uDC00Y");

        var bytes = builder.ToArray();

        var text = Load(bytes).ExtractText();
        Assert.Contains("X", text, StringComparison.Ordinal);
        Assert.Contains("Y", text, StringComparison.Ordinal);
    }
}
