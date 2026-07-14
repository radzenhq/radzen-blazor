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
            HighlightAnnotation => [Rectangle(width, height, annotation.Color, fill: true, stroke: false, 0)],
            UnderlineAnnotation => [Line(0, 1, width, 1, annotation.Color, 1)],
            StrikeOutAnnotation => [Line(0, height / 2, width, height / 2, annotation.Color, 1)],
            SquigglyAnnotation => [Squiggle(width, annotation.Color)],
            TextAnnotation => [Note(width, height, annotation.Color)],
            StampAnnotation stamp => Stamp(width, height, annotation.Color, stamp.Name),
            InkAnnotation ink => Ink(ink),
            FreeTextAnnotation freeText => FreeText(freeText),
            SquareAnnotation square => Shape(width, height, square, circle: false),
            CircleAnnotation circle => Shape(width, height, circle, circle: true),
            _ => [],
        };
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
            path.MoveTo(stroke[0].X - annotation.Bounds.X, stroke[0].Y - annotation.Bounds.Y);
            for (var i = 1; i < stroke.Count; i++)
            {
                path.LineTo(stroke[i].X - annotation.Bounds.X, stroke[i].Y - annotation.Bounds.Y);
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

    private static PathContent Squiggle(double width, Color color)
    {
        var path = new PathContent { Stroke = true, StrokeColor = color, Thickness = 1 };
        path.MoveTo(0, 1);
        var x = 0.0;
        while (x < width)
        {
            path.LineTo(Math.Min(width, x + 2), 3);
            path.LineTo(Math.Min(width, x + 4), 1);
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
    {
        var path = RectanglePath(width, height);
        path.Fill = fill;
        path.Stroke = stroke;
        path.FillColor = color;
        path.StrokeColor = color;
        path.Thickness = thickness;
        return path;
    }

    private static PathContent RectanglePath(double width, double height)
    {
        var path = new PathContent();
        path.MoveTo(0, 0);
        path.LineTo(width, 0);
        path.LineTo(width, height);
        path.LineTo(0, height);
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
