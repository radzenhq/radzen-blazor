#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;

namespace Radzen.Blazor.Pdf.Tests;

public class WatermarkKerningTests
{
    private const string Text = "AVATAR";

    private static DocumentBuilder Builder(bool kerning)
    {
        var builder = new DocumentBuilder { Fonts = { EnableKerning = kerning } };
        builder.Fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        var section = builder.Sections.Add();
        section.Watermark = new Watermark { Text = Text };
        section.Watermark.Font.Name = "Liberation Sans";
        section.Watermark.Font.Size = 60;
        section.Blocks.Add(new Paragraph());
        return builder;
    }

    private static string PageText(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        var (page, _) = BuildTestSupport.PageLeaves(reader)[0];
        return Encoding.ASCII.GetString(BuildTestSupport.Content(reader, page));
    }

    private static FontCollection Fonts(bool kerning)
    {
        var fonts = new FontCollection { EnableKerning = kerning };
        fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return fonts;
    }

    [Fact]
    public void KernedAndUnkernedWidthsDiffer()
    {
        var font = new Font { Name = "Liberation Sans", Size = 60 };

        Assert.True(Fonts(kerning: true).MeasureText(Text, font) < Fonts(kerning: false).MeasureText(Text, font));
    }

    private static double TjAdjustments(string content)
    {
        var open = content.IndexOf('[');
        var close = content.IndexOf("] TJ", StringComparison.Ordinal);
        Assert.InRange(open, 0, close);

        double total = 0;
        var number = new StringBuilder();
        var inString = false;
        for (var i = open + 1; i < close; i++)
        {
            var c = content[i];
            if (inString)
            {
                if (c == '\\')
                {
                    i++;
                }
                else if (c == ')')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '(')
            {
                inString = true;
            }
            else if (char.IsDigit(c) || c is '-' or '.')
            {
                number.Append(c);
                continue;
            }

            if (number.Length > 0)
            {
                total += double.Parse(number.ToString(), CultureInfo.InvariantCulture);
                number.Clear();
            }
        }

        if (number.Length > 0)
        {
            total += double.Parse(number.ToString(), CultureInfo.InvariantCulture);
        }

        return total;
    }

    [Fact]
    public void Kerned_DrawnWidthEqualsTheMeasuredWidthTheMarkIsCentredFrom()
    {
        var font = new Font { Name = "Liberation Sans", Size = 60 };
        var measured = Fonts(kerning: true).MeasureText(Text, font);
        var unkerned = Fonts(kerning: false).MeasureText(Text, font);

        var displacement = -TjAdjustments(PageText(Builder(kerning: true))) * font.Size / 1000.0;

        Assert.Equal(measured, unkerned + displacement, 2);
    }

    [Fact]
    public void KernedSpaceContainingRun_MeasuredWidthEqualsDrawnAdvance()
    {
        const string text = "AV AV";
        var fonts = Fonts(kerning: true);
        var font = new Font { Name = "Liberation Sans", Size = 60 };
        var builder = new SfntRunBuilder(fonts, new GeneratorFontResolver(PdfAConformance.None));

        var drawnAdvance = builder.Build(text, font, font.Size).Sum(run => run.Advance);

        Assert.Equal(fonts.MeasureText(text, font), drawnAdvance, 10);
        Assert.Contains(builder.Build(text, font, font.Size), run => run.Kerns is not null);
    }

    [Fact]
    public void Unkerned_WatermarkShowsWithoutTj()
    {
        var content = PageText(Builder(kerning: false));

        Assert.Contains("Tj", content);
        Assert.DoesNotContain("TJ", content);
    }
}
