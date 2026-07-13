#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// #48: a header/footer band reserves lines by laying the field paragraph out once with the
// single-digit placeholder. If a wider resolved page number wraps to more lines than were
// reserved it would overprint the content below, so ResolveFields fails loud instead.
public class FieldBandOverflowTests
{
    private static void Sized(Run run)
    {
        run.Font.Name = LineLayoutSupport.Family;
        run.Font.Size = 12;
    }

    private static Paragraph FieldParagraph()
    {
        var paragraph = new Paragraph();
        Sized(paragraph.Inlines.Add("ending on page "));
        Sized(paragraph.Inlines.Add(new PageNumberField())); // placeholder "0"
        return paragraph;
    }

    [Fact]
    public void ResolveFields_ResolvedNumberWrapsBeyondReserved_Throws()
    {
        var fonts = LineLayoutSupport.Fonts();
        var resolver = new FieldResolver(fonts, new StyleResolution());

        // Width fits "ending on page 0" (the placeholder) on one line but not "ending on page
        // 1000", which wraps its number to a second line.
        var width = fonts.MeasureText("ending on page 0", LineLayoutSupport.FontAt(12)) + 1;
        var reserved = LineBreaker.Break(FieldParagraph(), width, fonts).Count;
        Assert.Equal(1, reserved);

        var ex = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveFields(FieldParagraph(), width, 1000, 1000, null, reserved));
        Assert.Contains("reserved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveFields_SingleDigitNumberFitsReserved_DoesNotThrow()
    {
        var fonts = LineLayoutSupport.Fonts();
        var resolver = new FieldResolver(fonts, new StyleResolution());
        var width = fonts.MeasureText("ending on page 0", LineLayoutSupport.FontAt(12)) + 1;
        var reserved = LineBreaker.Break(FieldParagraph(), width, fonts).Count;

        var lines = resolver.ResolveFields(FieldParagraph(), width, 7, 9, null, reserved);

        Assert.Equal(reserved, lines.Count);
    }
}
