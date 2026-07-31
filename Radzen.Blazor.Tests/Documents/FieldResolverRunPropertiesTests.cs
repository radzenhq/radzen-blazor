#nullable enable
using System.Linq;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;

namespace Radzen.Blazor.Documents.Tests;

public class FieldResolverRunPropertiesTests
{
    [Fact]
    public void PageNumberFieldKeepsEveryRunPropertyWhenResolved()
    {
        var paragraph = new Paragraph();
        var field = new PageNumberField();
        paragraph.Inlines.Add(field);
        field.Link = "https://example.test";
        field.LinkToAnchor = "target";
        field.Anchor = "source";
        field.LetterSpacing = Unit.FromPoint(1.5);
        field.WordSpacing = Unit.FromPoint(2.5);
        field.HorizontalScale = 0.8;
        field.VerticalAlignment = RunVerticalAlignment.Superscript;
        field.VerticalAlignmentScale = 0.6;
        field.Opacity = 0.4;
        field.Invisible = true;

        var resolver = new FieldResolver(
            new FontCollection(),
            LoweringResult.CreateForDocument(StyleResolution.Empty),
            new LayoutCaptureContext());
        var line = Assert.Single(resolver.ResolveFields(paragraph, 500, 7, 10, null, 1));
        var resolved = Assert.Single(line.Fragments).Paint;

        Assert.Equal(field.Link, resolved.LinkTarget);
        Assert.Equal(field.LinkToAnchor, resolved.LinkToAnchor);
        Assert.Equal(field.Anchor, resolved.Anchor);
        Assert.Equal(field.LetterSpacing.Point, resolved.LetterSpacing);
        Assert.Equal(field.WordSpacing.Point, resolved.WordSpacing);
        Assert.Equal(field.HorizontalScale, resolved.HorizontalScale);
        Assert.True(resolved.IsScript);
        Assert.Equal(field.VerticalAlignmentScale, resolved.ScriptScale);
        Assert.Equal(field.Opacity, resolved.Opacity);
        Assert.Equal(field.Invisible, resolved.Invisible);
    }

    [Fact]
    public void FieldRunsWithDifferentSpacingDoNotMerge()
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Page ");
        var field = new PageNumberField();
        paragraph.Inlines.Add(field);
        field.LetterSpacing = Unit.FromPoint(2);

        var resolver = new FieldResolver(
            new FontCollection(),
            LoweringResult.CreateForDocument(StyleResolution.Empty),
            new LayoutCaptureContext());
        var line = Assert.Single(resolver.ResolveFields(paragraph, 500, 7, 10, null, 1));

        Assert.Equal(2, line.Fragments.Select(fragment => fragment.Source).Distinct().Count());
        Assert.Equal(2, line.Fragments[^1].Paint.LetterSpacing);
    }
}
