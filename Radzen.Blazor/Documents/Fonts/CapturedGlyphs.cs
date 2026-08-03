using System;
using System.Collections.Immutable;
using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Fonts;

internal readonly record struct CapturedGlyph(
    double Advance,
    double Kerning,
    int Cluster,
    int Codepoint);

internal enum CapturedFontFaceKind
{
    Sfnt,
    BuiltIn,
}

internal readonly record struct CapturedFaceMetrics(
    double Ascent,
    double Descent,
    double UnitsPerEm);

internal readonly record struct CapturedBuiltInFace(
    BuiltInFontFamily Family,
    bool Bold,
    bool Italic,
    BuiltInFaceMetrics Metrics);

internal readonly record struct CapturedFontFace : IFontProgramSource
{
    private readonly SfntFont? program;
    private readonly CapturedBuiltInFace builtIn;

    private CapturedFontFace(
        CapturedFontFaceKind kind,
        string family,
        bool bold,
        bool italic,
        CapturedFaceMetrics metrics,
        SfntFont? program,
        CapturedBuiltInFace builtIn)
    {
        Kind = kind;
        Family = family;
        Bold = bold;
        Italic = italic;
        Metrics = metrics;
        this.program = program;
        this.builtIn = builtIn;
    }

    public CapturedFontFaceKind Kind { get; }

    public string Family { get; }

    public bool Bold { get; }

    public bool Italic { get; }

    public CapturedFaceMetrics Metrics { get; }

    public CapturedBuiltInFace BuiltIn
        => Kind == CapturedFontFaceKind.BuiltIn
            ? builtIn
            : throw new InvalidOperationException("An sfnt face has no built-in descriptor.");

    SfntFont IFontProgramSource.Program
        => program ?? throw new InvalidOperationException("A built-in face has no sfnt font.");

    public static CapturedFontFace FromSfnt(SfntFont face)
        => new(
            CapturedFontFaceKind.Sfnt,
            face.FamilyName,
            face.Bold,
            face.Italic,
            new CapturedFaceMetrics(face.Ascent, face.Descent, face.UnitsPerEm),
            face,
            default);

    public static CapturedFontFace FromBuiltIn(CapturedBuiltInFace face)
        => new(
            CapturedFontFaceKind.BuiltIn,
            GenericFamily(face.Family),
            face.Bold,
            face.Italic,
            new CapturedFaceMetrics(
                face.Metrics.Ascender,
                face.Metrics.Descender,
                face.Metrics.DesignUnitsPerEm),
            null,
            face);

    private static string GenericFamily(BuiltInFontFamily family)
        => family switch
        {
            BuiltInFontFamily.Serif => "serif",
            BuiltInFontFamily.Monospace => "monospace",
            BuiltInFontFamily.Symbol => "Symbol",
            BuiltInFontFamily.ZapfDingbats => "ZapfDingbats",
            _ => "sans-serif",
        };
}

internal readonly record struct CapturedGlyphSpan(
    CapturedFontFace Face,
    ImmutableArray<CapturedGlyph> Glyphs,
    double Advance,
    double XOffset)
{
    public int GlyphCount => Glyphs.Length;

    public int WordSpaceCount
    {
        get
        {
            var count = 0;
            foreach (var glyph in Glyphs)
            {
                if (glyph.Codepoint == ' ')
                {
                    count++;
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
