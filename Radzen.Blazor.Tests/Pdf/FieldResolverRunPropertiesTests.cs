#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class FieldResolverRunPropertiesTests
{
    [Fact]
    public void PageNumberFieldKeepsEveryRunPropertyWhenResolved()
    {
        var paragraph = new Paragraph();
        var field = paragraph.Inlines.Add(new PageNumberField()) as Run;
        Assert.NotNull(field);
        field!.Link = "https://example.test";
        field.LinkToAnchor = "target";
        field.Anchor = "source";
        field.LetterSpacing = Unit.FromPoint(1.5);
        field.WordSpacing = Unit.FromPoint(2.5);
        field.HorizontalScale = 80;
        field.VerticalAlign = RunVerticalAlign.Superscript;
        field.VerticalAlignScale = 0.6;
        field.Opacity = 0.4;
        field.Invisible = true;
        field.SetFillGray(0.25);

        var resolver = new FieldResolver(new FontCollection(), new StyleResolution());
        var line = Assert.Single(resolver.ResolveFields(paragraph, 500, 7, 10, null, 1));
        var resolved = Assert.Single(line.Fragments).Run;

        Assert.Equal(field.Link, resolved.Link);
        Assert.Equal(field.LinkToAnchor, resolved.LinkToAnchor);
        Assert.Equal(field.Anchor, resolved.Anchor);
        Assert.Equal(field.LetterSpacing, resolved.LetterSpacing);
        Assert.Equal(field.WordSpacing, resolved.WordSpacing);
        Assert.Equal(field.HorizontalScale, resolved.HorizontalScale);
        Assert.Equal(field.VerticalAlign, resolved.VerticalAlign);
        Assert.Equal(field.VerticalAlignScale, resolved.VerticalAlignScale);
        Assert.Equal(field.Opacity, resolved.Opacity);
        Assert.Equal(field.Invisible, resolved.Invisible);
        Assert.Equal(field.FillPaint, resolved.FillPaint);
    }

    [Fact]
    public void FieldRunsWithDifferentSpacingDoNotMerge()
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Page ");
        var field = paragraph.Inlines.Add(new PageNumberField());
        field.LetterSpacing = Unit.FromPoint(2);

        var resolver = new FieldResolver(new FontCollection(), new StyleResolution());
        var line = Assert.Single(resolver.ResolveFields(paragraph, 500, 7, 10, null, 1));

        Assert.Equal(2, line.Fragments.Select(fragment => fragment.Run).Distinct().Count());
        Assert.Equal(Unit.FromPoint(2), line.Fragments[^1].Run.LetterSpacing);
    }
}
