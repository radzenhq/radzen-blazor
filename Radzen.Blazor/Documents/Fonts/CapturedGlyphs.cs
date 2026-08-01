using System;
using System.Collections.Immutable;
using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Fonts;

internal readonly record struct CapturedSfntGlyph(
    ushort GlyphId,
    double Advance,
    double OffsetX,
    double OffsetY,
    double TextAdjustmentPoints,
    int Cluster,
    int Codepoint);

internal readonly record struct CapturedBuiltInGlyph(
    double Advance,
    double TextAdjustmentPoints,
    int Cluster,
    int Codepoint);

internal enum CapturedFontFaceKind
{
    Sfnt,
    BuiltIn,
}

internal readonly record struct CapturedBuiltInFace(
    BuiltInFontFamily Family,
    bool Bold,
    bool Italic,
    BuiltInFaceMetrics Metrics);

internal readonly record struct CapturedFontFace
{
    private readonly SfntFont? sfnt;
    private readonly CapturedBuiltInFace builtIn;

    private CapturedFontFace(
        CapturedFontFaceKind kind,
        SfntFont? sfnt,
        CapturedBuiltInFace builtIn)
    {
        Kind = kind;
        this.sfnt = sfnt;
        this.builtIn = builtIn;
    }

    public CapturedFontFaceKind Kind { get; }

    public SfntFont Sfnt
        => Kind == CapturedFontFaceKind.Sfnt
            ? sfnt!
            : throw new InvalidOperationException("A built-in face has no sfnt font.");

    public CapturedBuiltInFace BuiltIn
        => Kind == CapturedFontFaceKind.BuiltIn
            ? builtIn
            : throw new InvalidOperationException("An sfnt face has no built-in descriptor.");

    public static CapturedFontFace FromSfnt(SfntFont face)
        => new(CapturedFontFaceKind.Sfnt, face, default);

    public static CapturedFontFace FromBuiltIn(CapturedBuiltInFace face)
        => new(CapturedFontFaceKind.BuiltIn, null, face);
}

internal readonly record struct CapturedGlyphSpan(
    CapturedFontFace Face,
    ImmutableArray<CapturedSfntGlyph> SfntGlyphs,
    ImmutableArray<CapturedBuiltInGlyph> BuiltInGlyphs,
    double Advance,
    double XOffset)
{
    public bool IsSfnt => Face.Kind == CapturedFontFaceKind.Sfnt;

    public int GlyphCount => IsSfnt ? SfntGlyphs.Length : BuiltInGlyphs.Length;

    public bool Equals(CapturedGlyphSpan other)
    {
        if (Face != other.Face
            || Advance != other.Advance
            || XOffset != other.XOffset
            || SfntGlyphs.Length != other.SfntGlyphs.Length
            || BuiltInGlyphs.Length != other.BuiltInGlyphs.Length)
        {
            return false;
        }

        for (var i = 0; i < SfntGlyphs.Length; i++)
        {
            if (SfntGlyphs[i] != other.SfntGlyphs[i])
            {
                return false;
            }
        }

        for (var i = 0; i < BuiltInGlyphs.Length; i++)
        {
            if (BuiltInGlyphs[i] != other.BuiltInGlyphs[i])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Face);
        hash.Add(SfntGlyphs.Length);
        for (var i = 0; i < SfntGlyphs.Length; i++)
        {
            hash.Add(SfntGlyphs[i]);
        }

        hash.Add(BuiltInGlyphs.Length);
        for (var i = 0; i < BuiltInGlyphs.Length; i++)
        {
            hash.Add(BuiltInGlyphs[i]);
        }

        hash.Add(Advance);
        hash.Add(XOffset);
        return hash.ToHashCode();
    }

    public int WordSpaceCount
    {
        get
        {
            var count = 0;
            if (IsSfnt)
            {
                foreach (var glyph in SfntGlyphs)
                {
                    if (glyph.Codepoint == ' ')
                    {
                        count++;
                    }
                }
            }
            else
            {
                foreach (var glyph in BuiltInGlyphs)
                {
                    if (glyph.Codepoint == ' ')
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }

}

internal readonly record struct CapturedGlyphRun(
    string Text,
    ImmutableArray<CapturedGlyphSpan> Spans,
    double Advance)
{
    public static CapturedGlyphRun Empty(string text) => new(text, [], 0);
}
