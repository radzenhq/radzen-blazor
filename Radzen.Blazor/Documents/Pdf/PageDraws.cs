using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

internal struct TextDraw
{
    public required double X { get; init; }
    public required double Baseline { get; init; }
    public required double Size { get; init; }
    public required Color Color { get; init; }
    public required GeneratedFont Font { get; init; }
    public required byte[] Bytes { get; init; }
    public double StrokeWidth { get; init; }
    public double Shear { get; init; }
    public double CharSpacing { get; init; }
    public double Rise { get; init; }
    public StructureElement? Element { get; init; }
    public Rect? Clip { get; set; }
    public string? ExtGState { get; init; }
}

internal struct ImageDraw
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required GeneratedImage Image { get; init; }
    public StructureElement? Element { get; init; }
    public Rect? Clip { get; set; }
    public string? ExtGState { get; init; }
}

internal struct FillDraw
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required Color Color { get; init; }
    public Rect? Clip { get; set; }
    public string? ExtGState { get; init; }
}

internal readonly struct EdgeDraw
{
    public required double X1 { get; init; }
    public required double Y1 { get; init; }
    public required double X2 { get; init; }
    public required double Y2 { get; init; }
    public required double LineWidth { get; init; }
    public required Color Color { get; init; }
    public required BorderStyle Style { get; init; }
    public string? ExtGState { get; init; }
}

// A watermark overlay serialized after all page content: text segments and/or an
// image drawn in a coordinate system rotated around the page center, made
// semi-transparent through the page's ExtGState resource.
internal sealed class WatermarkDraw
{
    public required double CenterX { get; init; }
    public required double CenterY { get; init; }
    public required double Rotation { get; init; }
    public string? ExtGState { get; init; }
    public List<TextDraw> Texts { get; } = [];
    public ImageDraw? Image { get; set; }
}

internal sealed class PagePlan
{
    public required PageSize Size { get; init; }
    public List<FillDraw> Fills { get; } = [];
    public List<EdgeDraw> Edges { get; } = [];
    public List<ImageDraw> Images { get; } = [];
    public List<TextDraw> Texts { get; } = [];
    public List<GeneratedLink> Links { get; } = [];
    public List<GeneratedExtGState> ExtGStates { get; } = [];
    public WatermarkDraw? Watermark { get; set; }
    public HashSet<GeneratedFont> UsedFonts { get; } = [];
    public HashSet<GeneratedImage> UsedImages { get; } = [];

    // One ExtGState per distinct (fill, stroke) alpha pair, keyed GS0, GS1, ...
    public string RegisterExtGState(double fillAlpha, double strokeAlpha)
    {
        fillAlpha = System.Math.Clamp(fillAlpha, 0, 1);
        strokeAlpha = System.Math.Clamp(strokeAlpha, 0, 1);
        foreach (var state in ExtGStates)
        {
            if (state.FillAlpha == fillAlpha && state.StrokeAlpha == strokeAlpha)
            {
                return state.Key;
            }
        }

        var key = "GS" + ExtGStates.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ExtGStates.Add(new GeneratedExtGState { Key = key, FillAlpha = fillAlpha, StrokeAlpha = strokeAlpha });
        return key;
    }
}
