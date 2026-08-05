#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class SyntheticItalicTests
{
    private const string Family = "Liberation Sans";
    private const double Size = 20;

    private static Document Author(bool registerItalicFace, bool bold = false)
    {
        var document = new Document();
        document.Fonts.Register(Family, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        if (registerItalicFace)
        {
            document.Fonts.Register(Family, new MemoryStream(
                PdfTestResources.ReadAllBytes("Fonts/LiberationSans-BoldItalic.ttf")), bold: false, italic: true);
        }

        var section = document.Sections.Add();
        var paragraph = section.Blocks.Add(new Paragraph());
        var lead = paragraph.Inlines.Add("Upright ");
        lead.Font.Family = Family;
        lead.Font.Size = Size;
        var slanted = paragraph.Inlines.Add("Slanted");
        slanted.Font.Family = Family;
        slanted.Font.Size = Size;
        slanted.Font.Italic = true;
        slanted.Font.Bold = bold;
        var tail = paragraph.Inlines.Add(" tail");
        tail.Font.Family = Family;
        tail.Font.Size = Size;
        return document;
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
        var document = Author(registerItalicFace: false);
        var content = CascadeTestSupport.FirstPageContent(document);
        Assert.Contains(TextMatrices(content), IsSheared);

        var text = BuildTestSupport.Reload(document).ExtractText();
        Assert.Contains("Upright", text, StringComparison.Ordinal);
        Assert.Contains("Slanted", text, StringComparison.Ordinal);
    }
}
