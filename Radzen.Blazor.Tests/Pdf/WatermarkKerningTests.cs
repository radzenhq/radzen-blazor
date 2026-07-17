#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// A watermark in a registered (embedded) font is centred from the kerned MeasureText width,
// so the drawn run must carry the same kerning: without the TJ adjustments the mark is
// painted at its unkerned width while positioned for its kerned one, and sits off-centre.
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

    // AVATAR has kerned pairs in Liberation Sans, so the two widths must differ - otherwise
    // the drift this test guards could not be observed at all.
    [Fact]
    public void KernedAndUnkernedWidthsDiffer()
    {
        var font = new Font { Name = "Liberation Sans", Size = 60 };

        Assert.True(Fonts(kerning: true).MeasureText(Text, font) < Fonts(kerning: false).MeasureText(Text, font));
    }

    // Sums the numeric adjustments of the page's single TJ array, in 1/1000 em. Glyph codes sit
    // in (...) literal strings whose octal escapes are digits too, so those are skipped wholesale.
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

    // The invariant the bug broke: the width the run is drawn at must equal the width it was
    // centred from. Drawn width is the unkerned advance sum plus the TJ displacement
    // (-adjustment * size / 1000 per entry), and that must land on the kerned MeasureText width.
    [Fact]
    public void Kerned_DrawnWidthEqualsTheMeasuredWidthTheMarkIsCentredFrom()
    {
        var font = new Font { Name = "Liberation Sans", Size = 60 };
        var measured = Fonts(kerning: true).MeasureText(Text, font);
        var unkerned = Fonts(kerning: false).MeasureText(Text, font);

        var displacement = -TjAdjustments(PageText(Builder(kerning: true))) * font.Size / 1000.0;

        // 2 dp: the TJ numbers are written to 3 decimals, far below the ~8.9 pt drift of the bug.
        Assert.Equal(measured, unkerned + displacement, 2);
    }

    // The default (unkerned) watermark emits a plain Tj show, unchanged.
    [Fact]
    public void Unkerned_WatermarkShowsWithoutTj()
    {
        var content = PageText(Builder(kerning: false));

        Assert.Contains("Tj", content);
        Assert.DoesNotContain("TJ", content);
    }
}
