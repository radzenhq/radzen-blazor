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

    // A single stop is a constant colour. Stops that span the full [0 1] domain interpolate
    // directly (two stops) or stitch one exponential per adjacent pair; stops that start after
    // 0 or end before 1 hold the endpoint colour constant over the leading/trailing sub-range,
    // so the stop offsets are honoured instead of being stretched across the whole axis.
    public static DictionaryObject BuildFunction(IReadOnlyList<GradientStop> stops)
    {
        if (stops.Count == 1)
        {
            return Exponential(stops[0].Color, stops[0].Color);
        }

        var leading = stops[0].Offset > 0;
        var trailing = stops[stops.Count - 1].Offset < 1;
        if (stops.Count == 2 && !leading && !trailing)
        {
            return Exponential(stops[0].Color, stops[stops.Count - 1].Color);
        }

        ArrayObject functions = [];
        var bounds = new List<double>();
        ArrayObject encode = [];

        if (leading)
        {
            functions.Add(Exponential(stops[0].Color, stops[0].Color));
            AddUnitEncode(encode);
            bounds.Add(stops[0].Offset);
        }

        for (var i = 0; i < stops.Count - 1; i++)
        {
            functions.Add(Exponential(stops[i].Color, stops[i + 1].Color));
            AddUnitEncode(encode);
            if (i < stops.Count - 2)
            {
                bounds.Add(stops[i + 1].Offset);
            }
        }

        if (trailing)
        {
            bounds.Add(stops[stops.Count - 1].Offset);
            functions.Add(Exponential(stops[stops.Count - 1].Color, stops[stops.Count - 1].Color));
            AddUnitEncode(encode);
        }

        return new DictionaryObject
        {
            ["FunctionType"] = new NumberObject(3),
            ["Domain"] = Domain(),
            ["Functions"] = functions,
            ["Bounds"] = StrictlyIncreasing(bounds),
            ["Encode"] = encode,
        };
    }

    // Type 3 /Bounds must be strictly increasing (ISO 32000-1 7.10.4). CSS hard stops produce
    // equal adjacent offsets; nudge each colliding bound just past its predecessor so Acrobat
    // accepts the function instead of rendering the fill blank. The epsilon is below content
    // stream (0.001) precision, so a valid strictly-increasing gradient is unaffected.
    private static ArrayObject StrictlyIncreasing(List<double> bounds)
    {
        const double epsilon = 1e-6;
        ArrayObject result = [];
        var previous = double.NegativeInfinity;
        foreach (var value in bounds)
        {
            var bound = value <= previous ? previous + epsilon : value;
            result.Add(new NumberObject(bound));
            previous = bound;
        }

        return result;
    }

    private static void AddUnitEncode(ArrayObject encode)
    {
        encode.Add(new NumberObject(0));
        encode.Add(new NumberObject(1));
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
