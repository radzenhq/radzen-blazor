using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Scene;
using Radzen.Documents.Core;

namespace Radzen.Documents.Layout;

internal sealed class PageNavigationCollector : ISceneVisitor
{
    private readonly List<LaidOutLink> links = [];
    private readonly List<LaidOutAnchor> anchors = [];
    private readonly IDictionary<string, SourceId> seen;
    private readonly List<Container> containers = [];
    private double top;

    private PageNavigationCollector(IDictionary<string, SourceId> seen) => this.seen = seen;

    public static LaidOutPage Collect(LaidOutPage page, IDictionary<string, SourceId> seen)
    {
        var collector = Walk(page, seen);

        return page with
        {
            Links = [.. collector.links],
            Anchors = [.. collector.anchors],
        };
    }

    public static ImmutableArray<LaidOutAnchor> Anchors(LaidOutPage page, IDictionary<string, SourceId> seen)
        => [.. Walk(page, seen).anchors];

    private static PageNavigationCollector Walk(LaidOutPage page, IDictionary<string, SourceId> seen)
    {
        var collector = new PageNavigationCollector(seen);
        SceneWalk.Page(page, collector);
        return collector;
    }

    void ISceneVisitor.EnterLayer(SceneLayerKind kind, double layerTop) => top = layerTop;

    void ISceneVisitor.Line(in LaidOutLine line, in SceneFrame frame)
    {
        var current = Current;
        Line(line.Line, frame.Left + line.X, top + line.Y + frame.Delta, current.Transform, current.Clip);
    }

    void ISceneVisitor.EnterBox(LaidOutBox box, in SceneFrame frame, in SceneClip clip)
        => containers.Add(new Container(
            box.Transform ?? Current.Transform,
            Clip(frame, box.Bounds)));

    void ISceneVisitor.LeaveBox(LaidOutBox box, in SceneFrame frame) => Pop();

    void ISceneVisitor.EnterCell(LaidOutCell cell, in SceneFrame frame, in SceneClip clip)
        => containers.Add(new Container(Current.Transform, Clip(frame, cell.Bounds)));

    void ISceneVisitor.LeaveCell(LaidOutCell cell, in SceneFrame frame) => Pop();

    private Container Current => containers.Count > 0 ? containers[^1] : default;

    private void Pop() => containers.RemoveAt(containers.Count - 1);

    private Clipping Clip(in SceneFrame frame, in Rect bounds)
        => new(
            frame.Left + bounds.X,
            top + bounds.Y + frame.Delta,
            frame.Left + bounds.X + bounds.Width,
            top + bounds.Y + frame.Delta + bounds.Height);

    private readonly record struct Container(Matrix? Transform, Clipping? Clip);

    private readonly record struct Clipping(double Left, double Top, double Right, double Bottom);

    private void Line(LineBox line, double originX, double lineTop, Matrix? transform, Clipping? clip)
    {
        foreach (var fragment in line.Fragments)
        {
            if (fragment.Paint.Anchor is not { Length: > 0 } anchor)
            {
                continue;
            }

            if (seen.TryGetValue(anchor, out var source))
            {
                if (source == fragment.Source)
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"Duplicate anchor name '{anchor}'; anchor names must be unique within a document.");
            }

            seen.Add(anchor, fragment.Source);
            anchors.Add(new LaidOutAnchor
            {
                Name = anchor,
                Top = transform is { } matrix
                    ? matrix.Transform(originX, lineTop).Y
                    : lineTop,
            });
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

            var (above, below) = Extent(first, line);
            links.Add(Link(
                originX + start,
                y - above,
                originX + end,
                y + below,
                first.Paint,
                first.Source,
                transform,
                clip));
            i = j;
        }
    }

    private static (double Above, double Below) Extent(in LineFragment fragment, LineBox line)
    {
        if (fragment.Paint.InlineImage is { } image)
        {
            return (image.Height, 0.0);
        }

        if (fragment.Paint.FormField is { } field)
        {
            return (field.Ascent, field.Height - field.Ascent);
        }

        var size = fragment.Paint.Font.Size;
        foreach (var span in fragment.GlyphRun.Spans)
        {
            var metrics = span.Face.Metrics;
            return (
                metrics.Ascent * size / metrics.UnitsPerEm,
                -metrics.Descent * size / metrics.UnitsPerEm);
        }

        return (line.Baseline, line.Height - line.Baseline);
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
            && (fragment.Text.Length > 0
                || fragment.Paint.InlineImage is not null
                || fragment.Paint.FormField is not null);
}
