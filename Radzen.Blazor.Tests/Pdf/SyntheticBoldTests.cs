#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class SyntheticBoldTests
{
    private const string Family = "Liberation Sans";
    private const double Size = 20;

    private static Document Author(bool registerBoldFace)
    {
        var document = new Document();
        document.Fonts.Register(Family, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        if (registerBoldFace)
        {
            document.Fonts.Register(Family, new MemoryStream(
                PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf")), bold: true, italic: false);
        }

        var section = document.Sections.Add();
        var paragraph = section.Blocks.Add(new Paragraph());
        var lead = paragraph.Inlines.Add("Normal ");
        lead.Font.Family = Family;
        lead.Font.Size = Size;
        var heavy = paragraph.Inlines.Add("Heavy");
        heavy.Font.Family = Family;
        heavy.Font.Size = Size;
        heavy.Font.Bold = true;
        var tail = paragraph.Inlines.Add(" tail");
        tail.Font.Family = Family;
        tail.Font.Size = Size;
        return document;
    }

    [Fact]
    public void BoldWithoutBoldFace_EmitsFillStrokeRenderMode()
    {
        var content = CascadeTestSupport.FirstPageContent(Author(registerBoldFace: false));

        Assert.Contains("2 Tr", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BoldWithoutBoldFace_RestoresRenderModeAfterRun()
    {
        var content = CascadeTestSupport.FirstPageContent(Author(registerBoldFace: false));

        var start = content.IndexOf("2 Tr", StringComparison.Ordinal);
        Assert.True(start >= 0, "expected synthetic bold to set render mode 2");
        Assert.Contains("0 Tr", content[(start + 4)..], StringComparison.Ordinal);
    }

    [Fact]
    public void BoldWithoutBoldFace_SetsSmallStrokeWidth()
    {
        var content = CascadeTestSupport.FirstPageContent(Author(registerBoldFace: false));

        var widths = Regex.Matches(content, @"(\d+(?:\.\d+)?) w[\r\n ]")
            .Select(m => double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToList();

        Assert.Contains(widths, w => w >= Size * 0.01 && w <= Size * 0.06);
    }

    [Fact]
    public void BoldWithoutBoldFace_StrokeAppliesToBoldRunOnly()
    {
        var content = CascadeTestSupport.FirstPageContent(Author(registerBoldFace: false));

        var start = content.IndexOf("2 Tr", StringComparison.Ordinal);
        Assert.True(start >= 0, "expected synthetic bold to set render mode 2");
        var rest = content[(start + 4)..];
        var end = rest.IndexOf("0 Tr", StringComparison.Ordinal);
        Assert.True(end >= 0, "expected render mode restored to 0");

        var shown = Regex.Matches(rest[..end], @"(Tj|TJ)\b").Count;
        Assert.Equal(1, shown);
    }

    [Fact]
    public void RealBoldFaceRegistered_NoSyntheticStroke()
    {
        var synthetic = CascadeTestSupport.FirstPageContent(Author(registerBoldFace: false));
        var real = CascadeTestSupport.FirstPageContent(Author(registerBoldFace: true));

        Assert.Contains("2 Tr", synthetic, StringComparison.Ordinal);
        Assert.DoesNotContain("2 Tr", real, StringComparison.Ordinal);
    }

    [Fact]
    public void SyntheticBold_TextRemainsExtractable()
    {
        var document = Author(registerBoldFace: false);
        var content = CascadeTestSupport.FirstPageContent(document);
        Assert.Contains("2 Tr", content, StringComparison.Ordinal);

        var text = BuildTestSupport.Reload(document).ExtractText();
        Assert.Contains("Normal", text, StringComparison.Ordinal);
        Assert.Contains("Heavy", text, StringComparison.Ordinal);
    }
}
