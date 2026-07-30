#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Pdf.Tests;

public class MicroTypographyTests
{
    private const string Family = "Liberation Sans";
    private const double Size = 20;

    private static Document Author(Action<Run> configure)
    {
        var document = new Document();
        document.Fonts.Register(Family, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        var lead = paragraph.Inlines.Add("Base ");
        lead.Font.Family = Family;
        lead.Font.Size = Size;
        var styled = paragraph.Inlines.Add("Styled");
        styled.Font.Family = Family;
        styled.Font.Size = Size;
        configure(styled);
        var tail = paragraph.Inlines.Add(" tail");
        tail.Font.Family = Family;
        tail.Font.Size = Size;
        return document;
    }

    private static List<double> Operands(string content, string op)
    {
        var values = new List<double>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+) " + op + @"\b"))
        {
            values.Add(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
        }

        return values;
    }

    [Fact]
    public void LetterSpacing_EmitsTcAndResetsAfterShow()
    {
        var content = CascadeTestSupport.FirstPageContent(
            Author(run => run.LetterSpacing = Unit.FromPoint(2)));

        var spacings = Operands(content, "Tc");
        Assert.Equal(2, spacings.Count);
        Assert.Equal(2.0, spacings[0], 6);
        Assert.Equal(0.0, spacings[1], 6);
        Assert.True(content.IndexOf("2 Tc", StringComparison.Ordinal) < content.IndexOf("0 Tc", StringComparison.Ordinal));
    }

    [Fact]
    public void LetterSpacing_WidensMeasuredAdvanceBySpacingPerGap()
    {
        var fonts = LineLayoutSupport.Fonts();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Styled");
        run.Font.Family = Family;
        run.Font.Size = Size;
        run.LetterSpacing = Unit.FromPoint(2);

        var lines = LineBreaker.Break(paragraph, 500, fonts);

        var plain = fonts.MeasureText("Styled", LineLayoutSupport.FontAt(Size));
        var fragment = Assert.Single(Assert.Single(lines).Fragments);
        Assert.Equal(plain + (2.0 * ("Styled".Length - 1)), fragment.Advance, 6);
    }

    [Fact]
    public void Superscript_EmitsPositiveTsAndReducedTfAboveBaseline()
    {
        var content = CascadeTestSupport.FirstPageContent(
            Author(run => run.VerticalAlignment = RunVerticalAlignment.Superscript));

        var rises = Operands(content, "Ts");
        Assert.Equal(2, rises.Count);
        Assert.True(rises[0] > 0);
        Assert.Equal(0.0, rises[1], 6);
        Assert.Contains(CascadeTestSupport.TfSizes(content), size => Math.Abs(size - (Size * 0.583)) < 0.01);
    }

    [Fact]
    public void Subscript_EmitsNegativeTs()
    {
        var content = CascadeTestSupport.FirstPageContent(
            Author(run => run.VerticalAlignment = RunVerticalAlignment.Subscript));

        var rises = Operands(content, "Ts");
        Assert.Equal(2, rises.Count);
        Assert.True(rises[0] < 0);
        Assert.Equal(0.0, rises[1], 6);
    }

    [Fact]
    public void Superscript_MeasuresAtReducedSizeAndRaisesAscent()
    {
        var fonts = LineLayoutSupport.Fonts();
        var plainLine = Assert.Single(LineBreaker.Break(LineLayoutSupport.SingleRun("Styled", Size), 500, fonts));

        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Styled");
        run.Font.Family = Family;
        run.Font.Size = Size;
        run.VerticalAlignment = RunVerticalAlignment.Superscript;
        var line = Assert.Single(LineBreaker.Break(paragraph, 500, fonts));

        var fragment = Assert.Single(line.Fragments);
        Assert.Equal(0.583 * Assert.Single(plainLine.Fragments).Advance, fragment.Advance, 6);
        Assert.True(line.Baseline > 0.583 * plainLine.Baseline);
        Assert.True(line.Height > 0.583 * plainLine.Height);
    }

    [Fact]
    public void DefaultRun_EmitsNoTcOrTsAndContentUnchanged()
    {
        var baseline = CascadeTestSupport.FirstPageContent(Author(_ => { }));
        var explicitDefaults = CascadeTestSupport.FirstPageContent(Author(run =>
        {
            run.LetterSpacing = Unit.FromPoint(0);
            run.VerticalAlignment = RunVerticalAlignment.None;
        }));

        Assert.Equal(baseline, explicitDefaults);
        Assert.Empty(Operands(baseline, "Tc"));
        Assert.Empty(Operands(baseline, "Ts"));
    }
}
