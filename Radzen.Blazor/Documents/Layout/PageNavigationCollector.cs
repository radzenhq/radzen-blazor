using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal sealed class PageNavigationCollector
{
    private readonly List<LaidOutLink> links = [];
    private readonly List<LaidOutAnchor> anchors = [];
    private readonly HashSet<string> seen = new(StringComparer.Ordinal);

    public static LaidOutPage Collect(LaidOutPage page)
    {
        var collector = new PageNavigationCollector();
        var left = page.ContentBox.X;

        collector.Layer(page.Body, left, page.ContentBox.Y);
        collector.Layer(page.HeaderLayer, left, page.HeaderTop);
        collector.Layer(page.FooterLayer, left, page.FooterTop);

        return page with
        {
            Links = [.. collector.links],
            Anchors = [.. collector.anchors],
        };
    }

    private void Layer(LaidOutLayer layer, double left, double top)
    {
        foreach (var line in layer.Lines)
        {
            Line(line.Line, left, top + line.Y, transform: null, clip: null);
        }

        var cursor = OrderedMerge.ByOrder(layer.Tables, static t => t.Order, layer.Boxes, static b => b.Order);
        while (cursor.MoveNext())
        {
            if (cursor.IsTable)
            {
                Fragment(layer.Tables[cursor.TableIndex], left, top, transform: null);
            }
            else
            {
                Box(layer.Boxes[cursor.BoxIndex], left, top);
            }
        }
    }

    private void Box(in LaidOutBox box, double left, double top)
        => Content(
            box.Content,
            left,
            top,
            box.Bounds.Y,
            box.Transform,
            Clip(left, top, box.Bounds, 0));

    private void Fragment(in LaidOutTableFragment positioned, double left, double top, Matrix? transform)
    {
        var x = left + positioned.Layout.Decoration.LeftIndent;
        foreach (var row in positioned.Rows)
        {
            foreach (var placed in row.Cells)
            {
                Cell(placed.Cell, x, top, placed.Delta, transform);
            }
        }
    }

    private void Cell(LaidOutCell cell, double left, double top, double delta, Matrix? transform)
        => Content(
            new LaidOutBoxContent
            {
                Height = 0,
                Lines = cell.Lines,
                Images = cell.Images,
                CodeSymbols = cell.CodeSymbols,
                Tables = cell.Tables,
                Boxes = cell.Boxes,
            },
            left,
            top,
            delta,
            transform,
            Clip(left, top, cell.Bounds, delta));

    private void Content(in LaidOutBoxContent content, double left, double top, double delta, Matrix? transform, Clipping? clip)
    {
        foreach (var line in content.Lines)
        {
            Line(line.Line, left + line.X, top + line.Y + delta, transform, clip);
        }

        var cursor = OrderedMerge.ByOrder(content.Tables, static t => t.Order, content.Boxes, static b => b.Order);
        while (cursor.MoveNext())
        {
            if (cursor.IsTable)
            {
                var nested = content.Tables[cursor.TableIndex];
                foreach (var cell in nested.Layout.Cells)
                {
                    Cell(
                        cell,
                        left + nested.X + nested.Layout.Decoration.LeftIndent,
                        top,
                        delta + nested.Y,
                        transform);
                }
            }
            else
            {
                var nested = content.Boxes[cursor.BoxIndex];
                Content(
                    nested.Content,
                    left + nested.Bounds.X,
                    top,
                    delta + nested.Bounds.Y,
                    transform,
                    Clip(left, top, nested.Bounds, delta));
            }
        }
    }

    private static Clipping Clip(double left, double top, in Rect bounds, double delta)
        => new(
            left + bounds.X,
            top + bounds.Y + delta,
            left + bounds.X + bounds.Width,
            top + bounds.Y + delta + bounds.Height);

    private readonly record struct Clipping(double Left, double Top, double Right, double Bottom);

    private void Line(LineBox line, double originX, double lineTop, Matrix? transform, Clipping? clip)
    {
        foreach (var fragment in line.Fragments)
        {
            if (fragment.Paint.Anchor is { Length: > 0 } anchor && seen.Add(anchor))
            {
                anchors.Add(new LaidOutAnchor
                {
                    Name = anchor,
                    Top = transform is { } matrix
                        ? matrix.Transform(originX, lineTop).Y
                        : lineTop,
                });
            }
        }

        var y = lineTop + line.Baseline;
        var fragments = line.Fragments;
        var i = 0;
        while (i < fragments.Length)
        {
            var first = fragments[i];
            if (!IsLink(first))
            {
                i++;
                continue;
            }

            var start = first.XOffset;
            var end = start + first.Advance;
            var j = i + 1;
            while (j < fragments.Length && fragments[j].Source == first.Source)
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            var size = first.Paint.Font.Size;
            links.Add(Link(
                originX + start,
                y - (size * 0.9),
                originX + end,
                y + (size * 0.3),
                first.Paint,
                first.Source,
                transform,
                clip));
            i = j;
        }
    }

    private LaidOutLink Link(
        double left, double top, double right, double bottom, in FragmentPaint paint, SourceId source, Matrix? transform, Clipping? clip)
    {
        if (clip is { } bounds)
        {
            left = Math.Clamp(left, bounds.Left, bounds.Right);
            right = Math.Clamp(right, bounds.Left, bounds.Right);
            top = Math.Clamp(top, bounds.Top, bounds.Bottom);
            bottom = Math.Clamp(bottom, bounds.Top, bounds.Bottom);
        }

        if (transform is { } matrix)
        {
            var (ax, ay) = matrix.Transform(left, top);
            var (bx, by) = matrix.Transform(right, top);
            var (cx, cy) = matrix.Transform(right, bottom);
            var (dx, dy) = matrix.Transform(left, bottom);
            left = Math.Min(Math.Min(ax, bx), Math.Min(cx, dx));
            right = Math.Max(Math.Max(ax, bx), Math.Max(cx, dx));
            top = Math.Min(Math.Min(ay, by), Math.Min(cy, dy));
            bottom = Math.Max(Math.Max(ay, by), Math.Max(cy, dy));
        }

        return new LaidOutLink
        {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
            Uri = paint.LinkTarget,
            Anchor = paint.AnchorTarget,
            Source = source,
        };
    }

    private static bool IsLink(in LineFragment fragment)
        => (fragment.Paint.LinkTarget is not null || fragment.Paint.AnchorTarget is not null)
            && fragment.Text.Length > 0;
}
