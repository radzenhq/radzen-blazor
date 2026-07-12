using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

// Turns a public GradientBrush into the PDF dictionaries a shading fill needs: the
// axial/radial /Shading dict (ISO 32000-1 8.7.4.5.2/8.7.4.5.3), its colour /Function
// (type 2 exponential for two stops, type 3 stitching for more) and the shading
// /Pattern (PatternType 2) that a content stream selects with /Pattern cs + scn.
internal static class ShadingBuilder
{
    public static DictionaryObject BuildShading(GradientBrush brush)
    {
        ArgumentNullException.ThrowIfNull(brush);
        ArrayObject extend = [new BooleanObject(brush.ExtendStart), new BooleanObject(brush.ExtendEnd)];
        return new DictionaryObject
        {
            ["ShadingType"] = new NumberObject(brush is RadialGradient ? 3 : 2),
            ["ColorSpace"] = new NameObject("DeviceRGB"),
            ["Coords"] = Coords(brush),
            ["Function"] = BuildFunction(brush.Stops),
            ["Extend"] = extend,
        };
    }

    public static DictionaryObject BuildPattern(GradientBrush brush) => new()
    {
        ["Type"] = new NameObject("Pattern"),
        ["PatternType"] = new NumberObject(2),
        ["Shading"] = BuildShading(brush),
    };

    // A single stop is a constant colour; two stops interpolate directly; more stops
    // stitch one exponential subfunction per adjacent pair over the [0 1] domain.
    public static DictionaryObject BuildFunction(IReadOnlyList<GradientStop> stops)
    {
        if (stops.Count <= 2)
        {
            return Exponential(stops[0].Color, stops[stops.Count - 1].Color);
        }

        ArrayObject functions = [];
        ArrayObject encode = [];
        for (var i = 0; i < stops.Count - 1; i++)
        {
            functions.Add(Exponential(stops[i].Color, stops[i + 1].Color));
            encode.Add(new NumberObject(0));
            encode.Add(new NumberObject(1));
        }

        ArrayObject bounds = [];
        for (var i = 1; i < stops.Count - 1; i++)
        {
            bounds.Add(new NumberObject(stops[i].Offset));
        }

        return new DictionaryObject
        {
            ["FunctionType"] = new NumberObject(3),
            ["Domain"] = Domain(),
            ["Functions"] = functions,
            ["Bounds"] = bounds,
            ["Encode"] = encode,
        };
    }

    private static DictionaryObject Exponential(Color from, Color to) => new()
    {
        ["FunctionType"] = new NumberObject(2),
        ["Domain"] = Domain(),
        ["C0"] = Rgb(from),
        ["C1"] = Rgb(to),
        ["N"] = new NumberObject(1),
    };

    private static ArrayObject Coords(GradientBrush brush) => brush switch
    {
        RadialGradient r =>
        [
            new NumberObject(r.X0), new NumberObject(r.Y0), new NumberObject(r.R0),
            new NumberObject(r.X1), new NumberObject(r.Y1), new NumberObject(r.R1),
        ],
        LinearGradient l =>
        [
            new NumberObject(l.X0), new NumberObject(l.Y0),
            new NumberObject(l.X1), new NumberObject(l.Y1),
        ],
        _ => throw new NotSupportedException($"Unsupported gradient brush '{brush.GetType().Name}'."),
    };

    private static ArrayObject Rgb(Color color) =>
        [new NumberObject(color.R / 255.0), new NumberObject(color.G / 255.0), new NumberObject(color.B / 255.0)];

    private static ArrayObject Domain() => [new NumberObject(0), new NumberObject(1)];
}
