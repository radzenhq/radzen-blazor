#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Q1(a): when Font.Italic is requested for a registered family that has no italic
// face, the run renders synthetic italic: a sheared text matrix (Tm with a non-zero
// c shear term, ~tan(12deg) = 0.21), restored after the run. When a real italic
// face IS registered the styled face is used and no shear is applied. Synthetic
// italic combines with synthetic bold ("2 Tr") when both are requested.
public class SyntheticItalicTests
{
    private const string Family = "Liberation Sans";
    private const double Size = 20;

    private static DocumentBuilder Author(bool registerItalicFace, bool bold = false)
    {
        var builder = new DocumentBuilder();
        builder.Fonts.Register(Family, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        if (registerItalicFace)
        {
            builder.Fonts.Register(Family, new MemoryStream(
                PdfTestResources.ReadAllBytes("Fonts/LiberationSans-BoldItalic.ttf")), bold: false, italic: true);
        }

        var section = builder.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var lead = paragraph.Inlines.Add("Upright ");
        lead.Font.Name = Family;
        lead.Font.Size = Size;
        var slanted = paragraph.Inlines.Add("Slanted");
        slanted.Font.Name = Family;
        slanted.Font.Size = Size;
        slanted.Font.Italic = true;
        slanted.Font.Bold = bold;
        var tail = paragraph.Inlines.Add(" tail");
        tail.Font.Name = Family;
        tail.Font.Size = Size;
        return builder;
    }

    private static List<double[]> TextMatrices(string content)
    {
        var matrices = new List<double[]>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) Tm"))
        {
            var values = new double[6];
            for (var i = 0; i < 6; i++)
            {
                values[i] = double.Parse(m.Groups[i + 1].Value, CultureInfo.InvariantCulture);
            }

            matrices.Add(values);
        }

        return matrices;
    }

    private static bool IsSheared(double[] tm) => Math.Abs(tm[2]) >= 0.1 && Math.Abs(tm[2]) <= 0.4;

    [Fact]
    public void ItalicWithoutItalicFace_EmitsShearedTextMatrix()
    {
        var content = CascadeTestSupport.FirstPageContent(Author(registerItalicFace: false));

        Assert.Contains(TextMatrices(content), IsSheared);
    }

    [Fact]
    public void ItalicWithoutItalicFace_ShearAppliesToItalicRunOnly()
    {
        var content = CascadeTestSupport.FirstPageContent(Author(registerItalicFace: false));

        var sheared = Regex.Match(content, @"(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) Tm");
        while (sheared.Success)
        {
            var c = double.Parse(sheared.Groups[3].Value, CultureInfo.InvariantCulture);
            if (Math.Abs(c) >= 0.1)
            {
                break;
            }

            sheared = sheared.NextMatch();
        }

        Assert.True(sheared.Success, "expected a sheared Tm for the italic run");

        // After the sheared run the matrix must be restored (an unsheared Tm or a
        // new text object) before any further glyphs are shown; exactly one
        // glyph-showing op renders under the shear.
        var rest = content[(sheared.Index + sheared.Length)..];
        var end = rest.Length;
        foreach (Match m in Regex.Matches(rest, @"(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) Tm"))
        {
            if (Math.Abs(double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)) < 0.05)
            {
                end = Math.Min(end, m.Index);
                break;
            }
        }

        var bt = rest.IndexOf("BT", StringComparison.Ordinal);
        if (bt >= 0)
        {
            end = Math.Min(end, bt);
        }

        Assert.True(end < rest.Length, "expected the text matrix to be restored after the italic run");
        Assert.Equal(1, Regex.Matches(rest[..end], @"(Tj|TJ)\b").Count);
    }

    [Fact]
    public void RealItalicFaceRegistered_NoShear()
    {
        var synthetic = CascadeTestSupport.FirstPageContent(Author(registerItalicFace: false));
        var real = CascadeTestSupport.FirstPageContent(Author(registerItalicFace: true));

        Assert.Contains(TextMatrices(synthetic), IsSheared);
        Assert.DoesNotContain(TextMatrices(real), IsSheared);
    }

    [Fact]
    public void SyntheticItalic_CombinesWithSyntheticBold()
    {
        var content = CascadeTestSupport.FirstPageContent(Author(registerItalicFace: false, bold: true));

        Assert.Contains("2 Tr", content, StringComparison.Ordinal);
        Assert.Contains(TextMatrices(content), IsSheared);
    }

    [Fact]
    public void SyntheticItalic_TextRemainsExtractable()
    {
        var builder = Author(registerItalicFace: false);
        var content = CascadeTestSupport.FirstPageContent(builder);
        Assert.Contains(TextMatrices(content), IsSheared);

        var text = BuildTestSupport.Reload(builder).ExtractText();
        Assert.Contains("Upright", text, StringComparison.Ordinal);
        Assert.Contains("Slanted", text, StringComparison.Ordinal);
    }
}
