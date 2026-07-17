using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

internal static class AnnotationAppearanceBuilder
{
    public static IReadOnlyList<ContentElement> Build(Annotation annotation)
    {
        if (annotation.Appearance is { } custom)
        {
            return [.. custom.Content];
        }

        var width = annotation.Bounds.Width;
        var height = annotation.Bounds.Height;
        return annotation switch
        {
            HighlightAnnotation highlight => Markup(highlight, static (area, color) =>
                Rectangle(area.Left, area.Bottom, area.Width, area.Height, color, fill: true, stroke: false, 0)),
            UnderlineAnnotation underline => Markup(underline, static (area, color) =>
                Line(area.Left, area.Bottom + 1, area.Right, area.Bottom + 1, color, 1)),
            StrikeOutAnnotation strikeOut => Markup(strikeOut, static (area, color) =>
                Line(area.Left, area.Bottom + area.Height / 2, area.Right, area.Bottom + area.Height / 2, color, 1)),
            SquigglyAnnotation squiggly => Markup(squiggly, static (area, color) => Squiggle(area, color)),
            TextAnnotation => [Note(width, height, annotation.Color)],
            StampAnnotation stamp => Stamp(width, height, annotation.Color, stamp.Name),
            InkAnnotation ink => Ink(ink),
            FreeTextAnnotation freeText => FreeText(freeText),
            SquareAnnotation square => Shape(width, height, square, circle: false),
            CircleAnnotation circle => Shape(width, height, circle, circle: true),
            _ => [],
        };
    }

    // One primitive per markup area so the appearance agrees with the /QuadPoints the
    // emitter writes: a markup spanning wrapped lines must not paint the gaps between them.
    private static IReadOnlyList<ContentElement> Markup(MarkupAnnotation annotation, Func<PdfRect, Color, ContentElement> build)
    {
        var result = new List<ContentElement>(annotation.Areas.Count);
        foreach (var area in annotation.Areas)
        {
            result.Add(build(
                PdfRect.FromSize(area.Left - annotation.Bounds.Left, area.Bottom - annotation.Bounds.Bottom, area.Width, area.Height),
                annotation.Color));
        }

        return result;
    }

    private static IReadOnlyList<ContentElement> Stamp(double width, double height, Color color, string name)
    {
        var border = Rectangle(width, height, color, fill: false, stroke: true, 2);
        var text = new TextContent(name, Unit.FromPoint(3), Unit.FromPoint(Math.Max(2, height / 2 - 4)))
        {
            Color = color,
            Font = new Font { Size = Math.Max(6, Math.Min(12, height / 2)) },
        };
        return [border, text];
    }

    private static IReadOnlyList<ContentElement> Ink(InkAnnotation annotation)
    {
        var result = new List<ContentElement>();
        foreach (var stroke in annotation.Strokes)
        {
            if (stroke.Count == 0)
            {
                continue;
            }

            var path = new PathContent { Stroke = true, StrokeColor = annotation.Color, Thickness = annotation.StrokeWidth };
            path.MoveTo(stroke[0].X - annotation.Bounds.Left, stroke[0].Y - annotation.Bounds.Bottom);
            for (var i = 1; i < stroke.Count; i++)
            {
                path.LineTo(stroke[i].X - annotation.Bounds.Left, stroke[i].Y - annotation.Bounds.Bottom);
            }

            result.Add(path);
        }

        return result;
    }

    private static IReadOnlyList<ContentElement> FreeText(FreeTextAnnotation annotation)
    {
        if (string.IsNullOrEmpty(annotation.Contents))
        {
            return [];
        }

        return
        [
            new TextContent(annotation.Contents, Unit.FromPoint(2), Unit.FromPoint(Math.Max(2, annotation.Bounds.Height - annotation.Font.Size - 2)))
            {
                Color = annotation.TextColor,
                Font = annotation.Font,
            },
        ];
    }

    private static IReadOnlyList<ContentElement> Shape(double width, double height, ShapeAnnotation annotation, bool circle)
    {
        var path = circle ? Ellipse(width, height) : RectanglePath(width, height);
        path.Stroke = annotation.BorderWidth > 0;
        path.Thickness = annotation.BorderWidth;
        path.StrokeColor = annotation.Color;
        path.Fill = annotation.InteriorColor is not null;
        path.FillColor = annotation.InteriorColor ?? Color.Transparent;
        return [path];
    }

    private static PathContent Note(double width, double height, Color color)
    {
        var path = RectanglePath(width, height);
        path.Stroke = true;
        path.Fill = true;
        path.StrokeColor = Color.Black;
        path.FillColor = color;
        return path;
    }

    private static PathContent Squiggle(PdfRect area, Color color)
    {
        var path = new PathContent { Stroke = true, StrokeColor = color, Thickness = 1 };
        path.MoveTo(area.Left, area.Bottom + 1);
        var x = 0.0;
        while (x < area.Width)
        {
            path.LineTo(area.Left + Math.Min(area.Width, x + 2), area.Bottom + 3);
            path.LineTo(area.Left + Math.Min(area.Width, x + 4), area.Bottom + 1);
            x += 4;
        }

        return path;
    }

    private static PathContent Line(double x0, double y0, double x1, double y1, Color color, double thickness)
    {
        var path = new PathContent { Stroke = true, StrokeColor = color, Thickness = thickness };
        path.MoveTo(x0, y0);
        path.LineTo(x1, y1);
        return path;
    }

    private static PathContent Rectangle(double width, double height, Color color, bool fill, bool stroke, double thickness)
        => Rectangle(0, 0, width, height, color, fill, stroke, thickness);

    private static PathContent Rectangle(double x, double y, double width, double height, Color color, bool fill, bool stroke, double thickness)
    {
        var path = RectanglePath(x, y, width, height);
        path.Fill = fill;
        path.Stroke = stroke;
        path.FillColor = color;
        path.StrokeColor = color;
        path.Thickness = thickness;
        return path;
    }

    private static PathContent RectanglePath(double width, double height) => RectanglePath(0, 0, width, height);

    private static PathContent RectanglePath(double x, double y, double width, double height)
    {
        var path = new PathContent();
        path.MoveTo(x, y);
        path.LineTo(x + width, y);
        path.LineTo(x + width, y + height);
        path.LineTo(x, y + height);
        path.Close();
        return path;
    }

    private static PathContent Ellipse(double width, double height)
    {
        const double kappa = 0.552284749831;
        var rx = width / 2;
        var ry = height / 2;
        var path = new PathContent();
        path.MoveTo(width, ry);
        path.CurveTo(width, ry + ry * kappa, rx + rx * kappa, height, rx, height);
        path.CurveTo(rx - rx * kappa, height, 0, ry + ry * kappa, 0, ry);
        path.CurveTo(0, ry - ry * kappa, rx - rx * kappa, 0, rx, 0);
        path.CurveTo(rx + rx * kappa, 0, width, ry - ry * kappa, width, ry);
        path.Close();
        return path;
    }
}
