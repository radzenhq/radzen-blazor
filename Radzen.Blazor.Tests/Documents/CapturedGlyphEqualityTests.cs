#nullable enable
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class CapturedGlyphEqualityTests
{
    [Fact]
    public void CapturedGlyphSpan_GlyphArraysHaveStructuralValueEquality()
    {
        var face = CapturedFontFace.FromBuiltIn(new CapturedBuiltInFace(
            BuiltInFontFamily.Sans,
            Bold: false,
            Italic: false,
            Metrics: default));
        var glyphs = ImmutableArray.Create(new CapturedBuiltInGlyph(
            Advance: 6,
            TextAdjustmentPoints: 0,
            Cluster: 0,
            Codepoint: 'A'));
        var first = new CapturedGlyphSpan(face, [], glyphs, Advance: 6, XOffset: 2);
        var second = new CapturedGlyphSpan(face, [], [.. glyphs], Advance: 6, XOffset: 2);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void SemanticStructureNode_DoesNotAdvertiseRecordEqualityOperators()
    {
        Assert.Null(typeof(SemanticStructureNode).GetMethod("op_Equality"));
        Assert.Null(typeof(SemanticStructureNode).GetMethod("op_Inequality"));
    }
}
