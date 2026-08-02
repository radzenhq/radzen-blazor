using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Core;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Scene;

namespace Radzen.Documents.Layout;

internal readonly record struct SceneGeometry(int PageIndex, Rect Bounds, SourceId Source);

internal static class SceneHitTest
{
    public static SourceId? At(LaidOutPage page, double x, double y)
    {
        var deepest = -1;
        SourceId? hit = null;
        foreach (var item in SceneGeometryCollector.Collect(page))
        {
            if (item.Depth >= deepest && Contains(item.Bounds, x, y))
            {
                deepest = item.Depth;
                hit = item.Source;
            }
        }

        return hit;
    }

    public static ImmutableArray<SceneGeometry> Geometry(LaidOutDocument document, SourceId source)
    {
        ArgumentNullException.ThrowIfNull(document);

        var geometry = ImmutableArray.CreateBuilder<SceneGeometry>();
        for (var index = 0; index < document.Pages.Length; index++)
        {
            foreach (var item in SceneGeometryCollector.Collect(document.Pages[index]))
            {
                if (item.Source == source)
                {
                    geometry.Add(new SceneGeometry(index, item.Bounds, source));
                }
            }
        }

        return geometry.ToImmutable();
    }

    private static bool Contains(in Rect bounds, double x, double y)
        => bounds.Width > 0
            && bounds.Height > 0
            && x >= bounds.X
            && x <= bounds.X + bounds.Width
            && y >= bounds.Y
            && y <= bounds.Y + bounds.Height;
}

internal sealed class SceneGeometryCollector : ISceneVisitor
{
    internal readonly record struct Item(SourceId Source, Rect Bounds, int Depth);

    private readonly record struct Container(Matrix? Transform, Rect? Clip);

    private readonly List<Item> items = [];
    private readonly List<Container> containers = [];
    private double top;

    public static IReadOnlyList<Item> Collect(LaidOutPage page)
    {
        var collector = new SceneGeometryCollector();
        SceneWalk.Page(page, collector);
        return collector.items;
    }

    void ISceneVisitor.EnterLayer(SceneLayerKind kind, double layerTop)
    {
        containers.Clear();
        top = layerTop;
    }

    void ISceneVisitor.Line(in LaidOutLine line, in SceneFrame frame)
    {
        var left = frame.Left + line.X;
        var lineTop = top + line.Y + frame.Delta;
        var height = line.Line.Height;
        Add(line.Source, new Rect(left, lineTop, line.Line.Width, height));

        foreach (var fragment in line.Line.Fragments)
        {
            Add(
                fragment.Source,
                new Rect(left + fragment.XOffset, lineTop, fragment.Advance, height),
                containers.Count + 1);
        }
    }

    void ISceneVisitor.Image(in LaidOutImage image, in SceneFrame frame)
        => Add(
            image.Source,
            new Rect(frame.Left + image.X, top + image.Y + frame.Delta, image.Width, image.Height));

    void ISceneVisitor.CodeSymbol(in LaidOutCodeSymbol codeSymbol, in SceneFrame frame)
        => Add(
            codeSymbol.Source,
            new Rect(
                frame.Left + codeSymbol.X,
                top + codeSymbol.Y + frame.Delta,
                codeSymbol.Width,
                codeSymbol.Height));

    void ISceneVisitor.EnterBox(LaidOutBox box, in SceneFrame frame, in SceneClip clip)
    {
        var rect = Local(frame, box.Bounds);
        Add(box.Source, rect);
        containers.Add(new Container(box.Transform ?? Current.Transform, rect));
    }

    void ISceneVisitor.LeaveBox(LaidOutBox box, in SceneFrame frame) => Pop();

    void ISceneVisitor.EnterFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
    {
        Add(
            fragment.Source,
            new Rect(
                frame.Left,
                top + fragment.Bounds.Y + frame.Delta,
                fragment.Bounds.Width,
                fragment.Bounds.Height));
        containers.Add(Current);
    }

    void ISceneVisitor.LeaveFragment(in LaidOutTableFragment fragment, in SceneFrame frame) => Pop();

    void ISceneVisitor.EnterTable(in LaidOutTablePlacement table, in SceneFrame frame)
    {
        Add(
            table.Layout.Source,
            new Rect(frame.Left, top + frame.Delta, table.Layout.Width, table.Layout.Height));
        containers.Add(Current);
    }

    void ISceneVisitor.LeaveTable(in LaidOutTablePlacement table, in SceneFrame frame) => Pop();

    void ISceneVisitor.EnterCell(LaidOutCell cell, in SceneFrame frame, in SceneClip clip)
    {
        var rect = Local(frame, cell.Bounds);
        Add(cell.Source, rect);
        containers.Add(new Container(Current.Transform, rect));
    }

    void ISceneVisitor.LeaveCell(LaidOutCell cell, in SceneFrame frame) => Pop();

    private Container Current => containers.Count > 0 ? containers[^1] : default;

    private void Pop() => containers.RemoveAt(containers.Count - 1);

    private Rect Local(in SceneFrame frame, in Rect bounds)
        => new(frame.Left + bounds.X, top + bounds.Y + frame.Delta, bounds.Width, bounds.Height);

    private void Add(SourceId source, in Rect bounds) => Add(source, bounds, containers.Count);

    private void Add(SourceId source, in Rect bounds, int depth)
        => items.Add(new Item(source, Place(bounds), depth));

    private Rect Place(in Rect bounds)
    {
        var left = bounds.X;
        var upper = bounds.Y;
        var right = bounds.X + bounds.Width;
        var bottom = bounds.Y + bounds.Height;
        var current = Current;

        if (current.Clip is { } clip)
        {
            left = Math.Clamp(left, clip.X, clip.X + clip.Width);
            right = Math.Clamp(right, clip.X, clip.X + clip.Width);
            upper = Math.Clamp(upper, clip.Y, clip.Y + clip.Height);
            bottom = Math.Clamp(bottom, clip.Y, clip.Y + clip.Height);
        }

        if (current.Transform is { } matrix)
        {
            var (ax, ay) = matrix.Transform(left, upper);
            var (bx, by) = matrix.Transform(right, upper);
            var (cx, cy) = matrix.Transform(right, bottom);
            var (dx, dy) = matrix.Transform(left, bottom);
            left = Math.Min(Math.Min(ax, bx), Math.Min(cx, dx));
            right = Math.Max(Math.Max(ax, bx), Math.Max(cx, dx));
            upper = Math.Min(Math.Min(ay, by), Math.Min(cy, dy));
            bottom = Math.Max(Math.Max(ay, by), Math.Max(cy, dy));
        }

        return new Rect(left, upper, right - left, bottom - upper);
    }
}
