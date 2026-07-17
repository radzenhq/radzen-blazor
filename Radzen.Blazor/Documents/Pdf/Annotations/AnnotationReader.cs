using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf;

internal static class AnnotationReader
{
    public static void Read(
        Page page,
        DocumentReader reader,
        DictionaryObject pageDictionary,
        IReadOnlyList<Page> pages,
        IReadOnlyDictionary<Page, DictionaryObject> pageDictionaries)
    {
        if (reader.GetArray(pageDictionary, "Annots") is not { } annotations)
        {
            return;
        }

        foreach (var original in annotations)
        {
            if (reader.AsDictionary(original) is not { } dictionary)
            {
                page.Annotations.Load(null, reader, original, null);
                continue;
            }

            page.Annotations.Load(Create(reader, dictionary, pages, pageDictionaries), reader, original, dictionary);
        }
    }

    private static Annotation? Create(
        DocumentReader reader,
        DictionaryObject dictionary,
        IReadOnlyList<Page> pages,
        IReadOnlyDictionary<Page, DictionaryObject> pageDictionaries)
    {
        var subtype = reader.GetName(dictionary, "Subtype");
        if (subtype is not ("Text" or "Highlight" or "Underline" or "StrikeOut" or "Squiggly"
            or "Link" or "Stamp" or "Ink" or "FreeText" or "Square" or "Circle"))
        {
            return null;
        }

        var bounds = Bounds(reader, reader.GetArray(dictionary, "Rect"));
        Annotation? annotation = subtype switch
        {
            "Text" => new TextAnnotation(bounds)
            {
                Open = reader.GetBool(dictionary, "Open") ?? false,
                Icon = reader.GetName(dictionary, "Name") ?? "Note",
            },
            "Highlight" => Markup(new HighlightAnnotation(bounds), reader, reader.GetArray(dictionary, "QuadPoints")),
            "Underline" => Markup(new UnderlineAnnotation(bounds), reader, reader.GetArray(dictionary, "QuadPoints")),
            "StrikeOut" => Markup(new StrikeOutAnnotation(bounds), reader, reader.GetArray(dictionary, "QuadPoints")),
            "Squiggly" => Markup(new SquigglyAnnotation(bounds), reader, reader.GetArray(dictionary, "QuadPoints")),
            "Link" => ReadLink(new LinkAnnotation(bounds), reader, dictionary, pages, pageDictionaries),
            "Stamp" => new StampAnnotation(bounds) { Name = reader.GetName(dictionary, "Name") ?? "Draft" },
            "Ink" => ReadInk(new InkAnnotation(bounds), reader, dictionary),
            "FreeText" => ReadFreeText(new FreeTextAnnotation(bounds), reader, dictionary),
            "Square" => ReadShape(new SquareAnnotation(bounds), reader, dictionary),
            "Circle" => ReadShape(new CircleAnnotation(bounds), reader, dictionary),
            _ => null,
        };

        if (annotation is not null)
        {
            annotation.Color = ReadColor(reader, reader.GetArray(dictionary, "C")) ?? annotation.Color;
            annotation.Opacity = reader.GetNumber(dictionary, "CA") ?? 1;
            annotation.Flags = (AnnotationFlags)(reader.GetInt(dictionary, "F") ?? 0);
            annotation.Contents = Text(reader.GetString(dictionary, "Contents"));
            annotation.Title = Text(reader.GetString(dictionary, "T"));
        }

        return annotation;
    }

    private static T Markup<T>(T annotation, DocumentReader reader, ArrayObject? quadPoints) where T : MarkupAnnotation
    {
        if (quadPoints is null || quadPoints.Count == 0)
        {
            return annotation;
        }

        if (quadPoints.Count % 8 != 0)
        {
            throw new DocumentParseException("An annotation /QuadPoints array must contain groups of eight numbers.", -1);
        }

        annotation.Areas.Clear();
        for (var i = 0; i < quadPoints.Count; i += 8)
        {
            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            for (var point = 0; point < 8; point += 2)
            {
                var x = Number(reader, quadPoints[i + point]);
                var y = Number(reader, quadPoints[i + point + 1]);
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }

            annotation.Areas.Add(new Rect(minX, minY, maxX - minX, maxY - minY));
        }

        return annotation;
    }

    private static LinkAnnotation ReadLink(
        LinkAnnotation annotation,
        DocumentReader reader,
        DictionaryObject dictionary,
        IReadOnlyList<Page> pages,
        IReadOnlyDictionary<Page, DictionaryObject> pageDictionaries)
    {
        if (reader.GetDictionary(dictionary, "A") is { } action)
        {
            var kind = reader.GetName(action, "S");
            if (kind == "URI" && reader.GetString(action, "URI") is { } uri)
            {
                annotation.Uri = new Uri(uri, UriKind.RelativeOrAbsolute);
            }
            else if (kind == "GoTo" && action.TryGetValue("D", out var destination))
            {
                ReadDestination(annotation, reader, destination!, pages, pageDictionaries);
            }
        }
        else if (dictionary.TryGetValue("Dest", out var destination))
        {
            ReadDestination(annotation, reader, destination!, pages, pageDictionaries);
        }

        return annotation;
    }

    private static void ReadDestination(
        LinkAnnotation annotation,
        DocumentReader reader,
        DocumentObject destination,
        IReadOnlyList<Page> pages,
        IReadOnlyDictionary<Page, DictionaryObject> pageDictionaries)
    {
        var resolved = reader.Resolve(destination);
        if (resolved is StringObject text)
        {
            annotation.Destination = Text(text.Value);
            return;
        }

        if (resolved is NameObject name)
        {
            annotation.Destination = name.Value;
            annotation.DestinationIsName = true;
            return;
        }

        if (resolved is not ArrayObject { Count: > 0 } array || reader.AsDictionary(array[0]) is not { } target)
        {
            throw new DocumentParseException("A link annotation has an unsupported destination.", -1);
        }

        for (var i = 0; i < pages.Count; i++)
        {
            if (ReferenceEquals(pageDictionaries[pages[i]], target))
            {
                annotation.TargetPageIndex = i;
                return;
            }
        }

        throw new DocumentParseException("A link annotation targets a page outside the document.", -1);
    }

    private static InkAnnotation ReadInk(InkAnnotation annotation, DocumentReader reader, DictionaryObject dictionary)
    {
        if (reader.GetArray(dictionary, "InkList") is not { } strokes)
        {
            return annotation;
        }

        foreach (var value in strokes)
        {
            if (reader.AsArray(value) is not { } points || points.Count % 2 != 0)
            {
                throw new DocumentParseException("An annotation /InkList stroke must contain coordinate pairs.", -1);
            }

            var stroke = new InkStroke();
            for (var i = 0; i < points.Count; i += 2)
            {
                stroke.Add(new AnnotationPoint(Number(reader, points[i]), Number(reader, points[i + 1])));
            }

            annotation.Strokes.Add(stroke);
        }

        if (reader.GetDictionary(dictionary, "BS") is { } border)
        {
            annotation.StrokeWidth = reader.GetNumber(border, "W") ?? annotation.StrokeWidth;
        }

        return annotation;
    }

    // /BS and /DA are rebuilt from the model whenever an annotation is re-emitted, so what
    // the source stated has to land on the model or the edit silently reverts it.
    private static FreeTextAnnotation ReadFreeText(FreeTextAnnotation annotation, DocumentReader reader, DictionaryObject dictionary)
    {
        if (reader.GetString(dictionary, "DA") is not { } da)
        {
            return annotation;
        }

        var (font, size) = FieldAppearances.ParseDefaultAppearance(da);
        if (font is not null && size > 0)
        {
            annotation.Font = FieldAppearances.AppearanceFont(font, size);
        }

        annotation.TextColor = DefaultAppearanceColor(da) ?? annotation.TextColor;
        return annotation;
    }

    // The colour of a /DA: the operand of its last non-stroking colour operator (ISO 32000-1
    // 12.7.3.3). A /Pattern or /CS-based colour has no direct model equivalent and is ignored.
    private static Color? DefaultAppearanceColor(string da)
    {
        var tokens = da.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        Color? color = null;
        for (var i = 0; i < tokens.Length; i++)
        {
            var operands = tokens[i] switch
            {
                "g" => 1,
                "rg" => 3,
                "k" => 4,
                _ => 0,
            };

            if (operands == 0 || i < operands)
            {
                continue;
            }

            var values = new double[operands];
            var parsed = true;
            for (var operand = 0; operand < operands; operand++)
            {
                parsed &= double.TryParse(
                    tokens[i - operands + operand], NumberStyles.Float, CultureInfo.InvariantCulture, out values[operand]);
            }

            if (!parsed)
            {
                continue;
            }

            color = operands switch
            {
                1 => Color.FromRgb(Channel(values[0]), Channel(values[0]), Channel(values[0])),
                3 => Color.FromRgb(Channel(values[0]), Channel(values[1]), Channel(values[2])),
                _ => Color.FromRgb(
                    Channel((1 - values[0]) * (1 - values[3])),
                    Channel((1 - values[1]) * (1 - values[3])),
                    Channel((1 - values[2]) * (1 - values[3]))),
            };
        }

        return color;
    }

    private static T ReadShape<T>(T annotation, DocumentReader reader, DictionaryObject dictionary) where T : ShapeAnnotation
    {
        annotation.InteriorColor = ReadColor(reader, reader.GetArray(dictionary, "IC"));
        if (reader.GetDictionary(dictionary, "BS") is { } border)
        {
            annotation.BorderWidth = reader.GetNumber(border, "W") ?? 1;
        }

        return annotation;
    }

    // Annotation.Bounds is a Rect but holds PDF user space, so the read crosses conventions here.
    private static Rect Bounds(DocumentReader reader, ArrayObject? value)
    {
        var bounds = PdfRects.Read(reader, value, RectPolicy.Strict(
            "A modeled annotation requires a four-number /Rect array.",
            "An annotation coordinate is not numeric."));
        return new Rect(bounds.Left, bounds.Bottom, bounds.Width, bounds.Height);
    }

    private static Color? ReadColor(DocumentReader reader, ArrayObject? value)
    {
        if (value is null)
        {
            return null;
        }

        // ISO 32000-1 Table 164: 0 numbers means no colour, 1 grayscale, 3 RGB, 4 CMYK.
        switch (value.Count)
        {
            case 0:
                return null;
            case 1:
                var gray = Channel(Number(reader, value[0]));
                return Color.FromRgb(gray, gray, gray);
            case 3:
                return Color.FromRgb(Channel(Number(reader, value[0])), Channel(Number(reader, value[1])), Channel(Number(reader, value[2])));
            case 4:
                var black = Number(reader, value[3]);
                return Color.FromRgb(
                    Channel((1 - Number(reader, value[0])) * (1 - black)),
                    Channel((1 - Number(reader, value[1])) * (1 - black)),
                    Channel((1 - Number(reader, value[2])) * (1 - black)));
            default:
                throw new DocumentParseException("An annotation colour array must contain zero, one, three or four numbers.", -1);
        }
    }

    private static byte Channel(double value) => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);

    private static double Number(DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is NumberObject number
            ? number.DoubleValue
            : throw new DocumentParseException("An annotation coordinate is not numeric.", -1);

    private static string? Text(string? value) => value is null ? null : FormField.DecodeTextString(value);
}
