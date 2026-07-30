using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf;

// ISO 32000-1 8.7.4.5.2/8.7.4.5.3: axial/radial /Shading dictionaries.
internal static class ShadingBuilder
{
    // ISO 32000-1 Table 78: /Extend defaults to [false false].
    private static ArrayObject BothEndsExtended() => [new BooleanObject(true), new BooleanObject(true)];

    public static DictionaryObject BuildShading(GradientBrush brush)
    {
        return new DictionaryObject
        {
            ["ShadingType"] = new NumberObject(ShadingType(brush)),
            ["ColorSpace"] = new NameObject("DeviceRGB"),
            ["Coords"] = Coords(brush),
            ["Function"] = BuildFunction(brush.Stops),
            ["Extend"] = BothEndsExtended(),
        };
    }

    public static DictionaryObject BuildShading(in GradientPaint brush)
    {
        return new DictionaryObject
        {
            ["ShadingType"] = new NumberObject(ShadingType(brush)),
            ["ColorSpace"] = new NameObject("DeviceRGB"),
            ["Coords"] = Coords(brush),
            ["Function"] = BuildFunction(brush.Stops),
            ["Extend"] = BothEndsExtended(),
        };
    }

    private static int ShadingType(in GradientPaint brush) => brush.Kind switch
    {
        GradientPaintKind.Linear => 2,
        GradientPaintKind.Radial => 3,
        _ => throw new NotSupportedException($"Unsupported gradient kind '{brush.Kind}'."),
    };

    private static int ShadingType(GradientBrush brush) => brush switch
    {
        LinearGradient => 2,
        RadialGradient => 3,
        _ => throw new NotSupportedException($"Unsupported gradient kind '{brush.GetType()}'."),
    };

    private static ArrayObject Coords(in GradientPaint brush) => brush.Kind switch
    {
        GradientPaintKind.Linear =>
        [
            new NumberObject(brush.X0), new NumberObject(brush.Y0),
            new NumberObject(brush.X1), new NumberObject(brush.Y1),
        ],
        GradientPaintKind.Radial =>
        [
            new NumberObject(brush.X0), new NumberObject(brush.Y0), new NumberObject(brush.R0),
            new NumberObject(brush.X1), new NumberObject(brush.Y1), new NumberObject(brush.R1),
        ],
        _ => throw new NotSupportedException($"Unsupported gradient kind '{brush.Kind}'."),
    };

    private static ArrayObject Coords(GradientBrush brush) => brush switch
    {
        LinearGradient linear =>
        [
            new NumberObject(linear.X0.Point), new NumberObject(linear.Y0.Point),
            new NumberObject(linear.X1.Point), new NumberObject(linear.Y1.Point),
        ],
        RadialGradient radial =>
        [
            new NumberObject(radial.X0.Point), new NumberObject(radial.Y0.Point), new NumberObject(radial.R0.Point),
            new NumberObject(radial.X1.Point), new NumberObject(radial.Y1.Point), new NumberObject(radial.R1.Point),
        ],
        _ => throw new NotSupportedException($"Unsupported gradient kind '{brush.GetType()}'."),
    };

    public static DictionaryObject BuildPattern(GradientBrush brush) => new()
    {
        ["Type"] = new NameObject("Pattern"),
        ["PatternType"] = new NumberObject(2),
        ["Shading"] = BuildShading(brush),
    };

    public static DictionaryObject BuildPattern(in GradientPaint brush) => new()
    {
        ["Type"] = new NameObject("Pattern"),
        ["PatternType"] = new NumberObject(2),
        ["Shading"] = BuildShading(brush),
    };

    public static DictionaryObject BuildPattern(GradientBrush brush, Matrix matrix)
    {
        var pattern = BuildPattern(brush);
        pattern["Matrix"] = new ArrayObject
        {
            new NumberObject(matrix.A),
            new NumberObject(matrix.B),
            new NumberObject(matrix.C),
            new NumberObject(matrix.D),
            new NumberObject(matrix.E),
            new NumberObject(matrix.F),
        };
        return pattern;
    }

    public static DictionaryObject BuildPattern(in GradientPaint brush, Matrix matrix)
    {
        var pattern = BuildPattern(brush);
        pattern["Matrix"] = new ArrayObject
        {
            new NumberObject(matrix.A),
            new NumberObject(matrix.B),
            new NumberObject(matrix.C),
            new NumberObject(matrix.D),
            new NumberObject(matrix.E),
            new NumberObject(matrix.F),
        };
        return pattern;
    }

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

    private static DictionaryObject BuildFunction(ImmutableArray<GradientStopPaint> stops)
    {
        if (stops.Length == 1)
        {
            return Exponential(stops[0].Color, stops[0].Color);
        }

        var leading = stops[0].Offset > 0;
        var trailing = stops[^1].Offset < 1;
        if (stops.Length == 2 && !leading && !trailing)
        {
            return Exponential(stops[0].Color, stops[^1].Color);
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

        for (var i = 0; i < stops.Length - 1; i++)
        {
            functions.Add(Exponential(stops[i].Color, stops[i + 1].Color));
            AddUnitEncode(encode);
            if (i < stops.Length - 2)
            {
                bounds.Add(stops[i + 1].Offset);
            }
        }

        if (trailing)
        {
            bounds.Add(stops[^1].Offset);
            functions.Add(Exponential(stops[^1].Color, stops[^1].Color));
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

    // ISO 32000-1 7.10.4: Type 3 /Bounds must lie strictly inside the domain.
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
