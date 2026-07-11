#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// G1(a): when Font.Bold is requested for a registered family that has no separate
// bold face, the run renders synthetic bold: text render mode 2 (fill+stroke,
// "2 Tr") with a small stroke line width (~fontSize*0.03), restored to mode 0
// afterwards. When a real bold face IS registered the styled face is used and no
// synthetic stroke is applied.
public class SyntheticBoldTests
{
    private const string Family = "Liberation Sans";
    private const double Size = 20;

    private static DocumentBuilder Author(bool registerBoldFace)
    {
        var builder = new DocumentBuilder();
        builder.Fonts.Register(Family, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        if (registerBoldFace)
        {
            builder.Fonts.Register(Family, new MemoryStream(
                PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf")), bold: true, italic: false);
        }

        var section = builder.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var lead = paragraph.Inlines.Add("Normal ");
        lead.Font.Name = Family;
        lead.Font.Size = Size;
        var heavy = paragraph.Inlines.Add("Heavy");
        heavy.Font.Name = Family;
        heavy.Font.Size = Size;
        heavy.Font.Bold = true;
        var tail = paragraph.Inlines.Add(" tail");
        tail.Font.Name = Family;
        tail.Font.Size = Size;
        return builder;
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

        // ~Size * 0.03 = 0.6; accept anything in a plausible synthetic-stroke band.
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

        // Exactly one glyph-showing op (the bold run) between mode 2 and the restore.
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
        var builder = Author(registerBoldFace: false);
        var content = CascadeTestSupport.FirstPageContent(builder);
        Assert.Contains("2 Tr", content, StringComparison.Ordinal);

        var text = BuildTestSupport.Reload(builder).ExtractText();
        Assert.Contains("Normal", text, StringComparison.Ordinal);
        Assert.Contains("Heavy", text, StringComparison.Ordinal);
    }
}
