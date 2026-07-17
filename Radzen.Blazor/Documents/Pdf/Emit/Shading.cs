using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emit;

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
            ["ShadingType"] = new NumberObject(brush.ShadingType),
            ["ColorSpace"] = new NameObject("DeviceRGB"),
            ["Coords"] = brush.BuildCoords(),
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
            ["Bounds"] = BoundsWithinDomain(bounds),
            ["Encode"] = encode,
        };
    }

    // ISO 32000-1 7.10.4: Type 3 /Bounds must lie strictly inside the domain. A CSS hard stop
    // lands a bound on or past the boundary, so nudge those by an epsilon below emitted (0.001)
    // precision, leaving valid interior gradients unaffected.
    private static ArrayObject BoundsWithinDomain(List<double> bounds)
    {
        const double epsilon = 1e-6;
        var values = new double[bounds.Count];
        var lower = 0.0;
        for (var i = 0; i < bounds.Count; i++)
        {
            lower = bounds[i] <= lower ? lower + epsilon : bounds[i];
            values[i] = lower;
        }

        var upper = 1.0;
        for (var i = values.Length - 1; i >= 0; i--)
        {
            upper = values[i] >= upper ? upper - epsilon : values[i];
            values[i] = upper;
        }

        ArrayObject result = [];
        foreach (var value in values)
        {
            result.Add(new NumberObject(value));
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
        ["C0"] = PdfColorArray.Rgb(from),
        ["C1"] = PdfColorArray.Rgb(to),
        ["N"] = new NumberObject(1),
    };

    private static ArrayObject Domain() => [new NumberObject(0), new NumberObject(1)];
}
