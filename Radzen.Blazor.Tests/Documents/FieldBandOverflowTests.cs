#nullable enable
using System;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;
using Radzen.Blazor.Tests.Isolated;

public class FieldBandOverflowTests
{
    private static void Sized(Inline inline)
    {
        var run = (TextInline)inline;
        run.Font.Family = LineLayoutSupport.Family;
        run.Font.Size = 12;
    }

    private static Paragraph FieldParagraph()
    {
        var paragraph = new Paragraph();
        Sized(paragraph.Inlines.Add("ending on page "));
        Sized(paragraph.Inlines.Add(new PageNumberField()));
        return paragraph;
    }

    private static FieldResolver Resolver(FontCollection fonts)
        => new(fonts,
            LoweringResult.CreateForDocument(StyleResolution.Empty),
            new LayoutCaptureContext(ImageProbes.None));

    [Fact]
    public void ResolveFields_ResolvedNumberWrapsBeyondReserved_Throws()
    {
        var fonts = LineLayoutSupport.Fonts();
        var width = fonts.MeasureText("0000", LineLayoutSupport.FontAt(12)) + 1;
        var reserved = IsolatedLineBreaker.Break(FieldParagraph(), width, fonts).Count;

        var ex = Assert.Throws<InvalidOperationException>(
            () => Resolver(fonts).ResolveFields(FieldParagraph(), width, int.MaxValue, int.MaxValue, null, reserved));
        Assert.Contains("reserved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveFields_FourDigitNumberFitsReserved_DoesNotThrow()
    {
        var fonts = LineLayoutSupport.Fonts();
        var width = fonts.MeasureText("ending on page 0", LineLayoutSupport.FontAt(12)) + 1;
        var reserved = IsolatedLineBreaker.Break(FieldParagraph(), width, fonts).Count;

        var lines = Resolver(fonts).ResolveFields(FieldParagraph(), width, 1000, 1000, null, reserved);

        Assert.True(lines.Count <= reserved);
    }

    [Fact]
    public void ResolveFields_SingleDigitNumberFitsReserved_DoesNotThrow()
    {
        var fonts = LineLayoutSupport.Fonts();
        var width = fonts.MeasureText("ending on page 0", LineLayoutSupport.FontAt(12)) + 1;
        var reserved = IsolatedLineBreaker.Break(FieldParagraph(), width, fonts).Count;

        var lines = Resolver(fonts).ResolveFields(FieldParagraph(), width, 7, 9, null, reserved);

        Assert.True(lines.Count <= reserved);
    }
}
