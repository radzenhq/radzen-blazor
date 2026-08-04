#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class RoundTripIntegrityTests
{
    private static PortableDocument Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream);
    }

    private static byte[] StreamData(string emission, string marker)
    {
        var dictionary = Line(emission, marker);
        var start = emission.IndexOf(dictionary, StringComparison.Ordinal) + dictionary.Length;
        var body = emission.IndexOf("\nstream\n", start, StringComparison.Ordinal);
        Assert.True(
            body == start,
            $"The object carrying '{marker}' is not a stream.\nObject:\n{Excerpt(dictionary)}");

        var end = emission.IndexOf("\nendstream", body, StringComparison.Ordinal);
        Assert.True(end >= 0, $"The stream carrying '{marker}' has no endstream.");
        return Encoding.Latin1.GetBytes(emission[(body + 8)..end]);
    }


    [Fact]
    public void Resave_LoadedBase14Page_KeepsFontResourcesAndText()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello Resave", "Helvetica");

        var reloaded = Load(new DocumentRenderer().ToArray(document));
        var resaved = reloaded.ToArray();
        var emission = Emit(reloaded);

        References("page tree", "Kids", 1, Line(emission, "/Type /Pages"));
        Shaped("re-saved page", @"/Resources <<.*/Font ", Line(emission, "/Type /Page "));

        Assert.Contains("Hello Resave", Load(resaved).ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Resave_LoadedType0Page_KeepsEmbeddedFontAndText()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Embedded Survives", BuildTestSupport.Latin);

        var reloaded = Load(new DocumentRenderer().ToArray(document));
        var resaved = reloaded.ToArray();

        Carries("emission", "/Subtype /Type0", Emit(reloaded));
        Assert.Contains("Embedded Survives", Load(resaved).ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Resave_LoadedImagePage_KeepsImageXObject()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(200);
        image.Height = Unit.FromPoint(100);

        var original = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        var emission = Emit(Load(new DocumentRenderer().ToArray(document)));

        var count = Regex.Matches(emission, "/Subtype /Image").Count;
        Assert.True(count == 1, $"Expected exactly 1 image XObject in the emission, found {count}.");
        Assert.Equal(original, StreamData(emission, "/Subtype /Image"));
    }


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

        var content = BuildTestSupport.Content(reader, leaves[0].Page);
        Assert.Contains("BT", ContentOperationTestHelpers.Operators(content));
        Assert.Contains(
            ContentStreamTokenizer.Parse(content),
            operation => operation.Operator == "Tj"
                && Encoding.Latin1.GetString(operation.Operands[0].Bytes) == "Hello Flate");
    }

    [Fact]
    public void Resave_FlateCompressedContent_TextSurvivesReload()
    {
        var resaved = Load(FlateFixture("Hello Flate")).ToArray();

        Assert.Contains("Hello Flate", Load(resaved).ExtractText(), StringComparison.Ordinal);
    }


    private static PortableDocument BuildBase14(string text)
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, "Helvetica");
        return new DocumentRenderer().Render(document);
    }

    [Fact]
    public void BuiltPage_GetContent_ExposesGeneratedContent()
    {
        var built = BuildBase14("Generated body");

        var content = built.Pages[0].GetContent();
        Assert.NotNull(content);
        Assert.Contains("BT", ContentOperationTestHelpers.Operators(content!));
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
        var target = new PortableDocument();
        target.Append(BuildBase14("Appended body"));

        Assert.Equal(1, target.Pages.Count);

        var reader = DocumentReader.Parse(target.ToArray());
        var leaves = BuildTestSupport.PageLeaves(reader);
        Assert.Single(leaves);

        var operators = ContentOperationTestHelpers.Operators(BuildTestSupport.Content(reader, leaves[0].Page));
        Assert.Contains("BT", operators);
        Assert.Contains("Tj", operators);
    }


    private static Document BuilderWithLatinText(string text)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, BuildTestSupport.Latin);
        return document;
    }

    [Fact]
    public void Build_TextWithSurrogatePair_DoesNotThrow()
    {
        var document = BuilderWithLatinText("A😀B");

        var bytes = new DocumentRenderer().ToArray(document);

        var text = Load(bytes).ExtractText();
        Assert.Contains("A", text, StringComparison.Ordinal);
        Assert.Contains("B", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_TextWithLoneSurrogate_DoesNotThrow()
    {
        var document = BuilderWithLatinText("X\uDC00Y");

        var bytes = new DocumentRenderer().ToArray(document);

        var text = Load(bytes).ExtractText();
        Assert.Contains("X", text, StringComparison.Ordinal);
        Assert.Contains("Y", text, StringComparison.Ordinal);
    }
}
