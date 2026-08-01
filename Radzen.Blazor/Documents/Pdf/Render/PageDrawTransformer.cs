using System;
using System.Collections.Generic;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Pdf.Render;

internal static class PageDrawTransformer
{
    public static void ApplyColorAlpha(PagePlan plan)
    {
        for (var i = 0; i < plan.Fills.Count; i++)
        {
            var fill = plan.Fills[i];
            if (fill.Gradient is null && Translucent(fill.Color, out var alpha))
            {
                plan.Fills[i] = fill with { ExtGState = plan.ApplyAlpha(fill.ExtGState, alpha) };
            }
        }

        for (var i = 0; i < plan.Edges.Count; i++)
        {
            var edge = plan.Edges[i];
            if (Translucent(edge.Color, out var alpha))
            {
                plan.Edges[i] = edge with { ExtGState = plan.ApplyAlpha(edge.ExtGState, alpha) };
            }
        }

        for (var i = 0; i < plan.RoundedStrokes.Count; i++)
        {
            var rounded = plan.RoundedStrokes[i];
            if (Translucent(rounded.Color, out var alpha))
            {
                plan.RoundedStrokes[i] = rounded with { ExtGState = plan.ApplyAlpha(rounded.ExtGState, alpha) };
            }
        }

        for (var i = 0; i < plan.Texts.Count; i++)
        {
            var text = plan.Texts[i];
            if (Translucent(text.Color, out var alpha))
            {
                plan.Texts[i] = text with { ExtGState = plan.ApplyAlpha(text.ExtGState, alpha) };
            }
        }
    }

    private static bool Translucent(Color color, out double alpha)
    {
        alpha = color.A / 255.0;
        return color.A != 255;
    }

    public static void ApplyRoundedClip(PagePlan plan, PdfRect bounds, double radius, PlanMarks mark)
    {
        ApplyClip(plan.Fills, mark.Fills, fill => fill.Clip is null
            ? fill with { Clip = bounds, ClipRadius = radius }
            : fill);
        ApplyClip(plan.Edges, mark.Edges, edge => edge.Clip is null
            ? edge with { Clip = bounds, ClipRadius = radius }
            : edge);
        ApplyClip(plan.Images, mark.Images, image => image.Clip is null
            ? image with { Clip = bounds, ClipRadius = radius }
            : image);
        ApplyClip(plan.Texts, mark.Texts, text => text.Clip is null
            ? text with { Clip = bounds, ClipRadius = radius }
            : text);
    }

    private static void ApplyClip<T>(List<T> items, int start, Func<T, T> clip)
    {
        for (var i = start; i < items.Count; i++)
        {
            items[i] = clip(items[i]);
        }
    }

    public static void ApplyTransform(PagePlan plan, Matrix transform, PlanMarks mark)
    {
        ValidateTransform(plan, mark);
        TransformEdges(plan, transform, mark);
        ConvertFills(plan, transform, mark);
        ConvertRoundedStrokes(plan, transform, mark);

        for (var i = mark.Images; i < plan.Images.Count; i++)
        {
            plan.Images[i] = plan.Images[i] with { Transform = transform };
        }

        for (var i = mark.Texts; i < plan.Texts.Count; i++)
        {
            plan.Texts[i] = plan.Texts[i] with { Transform = transform };
        }
    }

    private static void ValidateTransform(PagePlan plan, PlanMarks mark)
    {
        for (var i = mark.Fills; i < plan.Fills.Count; i++)
        {
            var fill = plan.Fills[i];
            if (fill.Radius > 0 || (fill.Clip is not null && fill.ClipRadius > 0))
            {
                throw new NotSupportedException(
                    "A rotated box cannot preserve rounded corners or a rounded clip; remove the corner radius or the rotation.");
            }

            if (fill.Gradient is not null)
            {
                throw new NotSupportedException(
                    "A rotated box cannot preserve a gradient background; remove the gradient or the rotation.");
            }
        }

        for (var i = mark.Rounded; i < plan.RoundedStrokes.Count; i++)
        {
            if (plan.RoundedStrokes[i].Radius > 0)
            {
                throw new NotSupportedException(
                    "A rotated box cannot preserve a rounded border; remove the corner radius or the rotation.");
            }
        }

        for (var i = mark.Edges; i < plan.Edges.Count; i++)
        {
            if (plan.Edges[i].Clip is not null && plan.Edges[i].ClipRadius > 0)
            {
                throw new NotSupportedException(
                    "A rotated box cannot preserve a rounded clip on a border edge; remove the corner radius or the rotation.");
            }
        }
    }

    private static void TransformEdges(PagePlan plan, Matrix transform, PlanMarks mark)
    {
        for (var i = mark.Edges; i < plan.Edges.Count; i++)
        {
            var edge = plan.Edges[i];
            var (x1, y1) = transform.Transform(edge.X1, edge.Y1);
            var (x2, y2) = transform.Transform(edge.X2, edge.Y2);
            plan.Edges[i] = edge with { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Clip = null, ClipRadius = 0 };
        }
    }

    private static void ConvertFills(PagePlan plan, Matrix transform, PlanMarks mark)
    {
        if (plan.Fills.Count <= mark.Fills)
        {
            return;
        }

        var converted = new List<EdgeDraw>(plan.Fills.Count - mark.Fills);
        for (var i = mark.Fills; i < plan.Fills.Count; i++)
        {
            var fill = plan.Fills[i];
            if (fill.Width <= 0 || fill.Height <= 0)
            {
                continue;
            }

            var midY = fill.Y + fill.Height / 2;
            var (x1, y1) = transform.Transform(fill.X, midY);
            var (x2, y2) = transform.Transform(fill.X + fill.Width, midY);
            converted.Add(new EdgeDraw
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                LineWidth = fill.Height,
                Color = fill.Color,
                Style = BorderStyle.Solid,
                Artifact = SemanticArtifacts.ForDecoration(fill.Artifact),
                ExtGState = fill.ExtGState,
                Stack = fill.Stack,
            });
        }

        plan.Fills.RemoveRange(mark.Fills, plan.Fills.Count - mark.Fills);
        plan.Edges.InsertRange(mark.Edges, converted);
    }

    private static void ConvertRoundedStrokes(PagePlan plan, Matrix transform, PlanMarks mark)
    {
        if (plan.RoundedStrokes.Count <= mark.Rounded)
        {
            return;
        }

        for (var i = mark.Rounded; i < plan.RoundedStrokes.Count; i++)
        {
            var rounded = plan.RoundedStrokes[i];
            var left = rounded.X;
            var bottom = rounded.Y;
            var right = rounded.X + rounded.Width;
            var top = rounded.Y + rounded.Height;
            AddTransformedEdge(plan, transform, left, top, right, top, rounded);
            AddTransformedEdge(plan, transform, right, bottom, right, top, rounded);
            AddTransformedEdge(plan, transform, left, bottom, right, bottom, rounded);
            AddTransformedEdge(plan, transform, left, bottom, left, top, rounded);
        }

        plan.RoundedStrokes.RemoveRange(mark.Rounded, plan.RoundedStrokes.Count - mark.Rounded);
    }

    private static void AddTransformedEdge(
        PagePlan plan,
        Matrix transform,
        double x1,
        double y1,
        double x2,
        double y2,
        RoundedStrokeDraw rounded)
    {
        var (tx1, ty1) = transform.Transform(x1, y1);
        var (tx2, ty2) = transform.Transform(x2, y2);
        plan.Edges.Add(new EdgeDraw
        {
            X1 = tx1,
            Y1 = ty1,
            X2 = tx2,
            Y2 = ty2,
            LineWidth = rounded.LineWidth,
            Color = rounded.Color,
            Style = rounded.Style,
            Artifact = rounded.Artifact,
            ExtGState = rounded.ExtGState,
            Stack = rounded.Stack,
        });
    }
}
