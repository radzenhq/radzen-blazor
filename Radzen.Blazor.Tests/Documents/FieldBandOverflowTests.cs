#nullable enable
using System;
using Xunit;

using Radzen.Documents;
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

    [Fact]
    public void ResolveFields_ResolvedNumberWrapsBeyondReserved_Throws()
    {
        var fonts = LineLayoutSupport.Fonts();
        var resolver = new FieldResolver(
            fonts,
            LoweringContext.CreateForDocument(StyleResolution.Empty),
            new LayoutCaptureContext());

        var width = fonts.MeasureText("ending on page 0", LineLayoutSupport.FontAt(12)) + 1;
        var reserved = IsolatedLineBreaker.Break(FieldParagraph(), width, fonts).Count;
        Assert.Equal(1, reserved);

        var ex = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveFields(FieldParagraph(), width, 1000, 1000, null, reserved));
        Assert.Contains("reserved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveFields_SingleDigitNumberFitsReserved_DoesNotThrow()
    {
        var fonts = LineLayoutSupport.Fonts();
        var resolver = new FieldResolver(
            fonts,
            LoweringContext.CreateForDocument(StyleResolution.Empty),
            new LayoutCaptureContext());
        var width = fonts.MeasureText("ending on page 0", LineLayoutSupport.FontAt(12)) + 1;
        var reserved = IsolatedLineBreaker.Break(FieldParagraph(), width, fonts).Count;

        var lines = resolver.ResolveFields(FieldParagraph(), width, 7, 9, null, reserved);

        Assert.Equal(reserved, lines.Count);
    }
}
