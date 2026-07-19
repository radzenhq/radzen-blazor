using System;

namespace Radzen.Documents.Pdf;

internal static class AnnotationCloner
{
    public static Annotation? Clone(Annotation source, int sourcePageOffset, int sourcePageCount, int targetPageOffset)
    {
        if (source is LinkAnnotation { TargetPageIndex: { } targetPageIndex }
            && (targetPageIndex < sourcePageOffset || targetPageIndex >= sourcePageOffset + sourcePageCount))
        {
            return null;
        }

        Annotation clone = source switch
        {
            TextAnnotation value => new TextAnnotation(value.Bounds) { Open = value.Open, Icon = value.Icon },
            HighlightAnnotation value => CopyAreas(new HighlightAnnotation(value.Bounds), value),
            UnderlineAnnotation value => CopyAreas(new UnderlineAnnotation(value.Bounds), value),
            StrikeOutAnnotation value => CopyAreas(new StrikeOutAnnotation(value.Bounds), value),
            SquigglyAnnotation value => CopyAreas(new SquigglyAnnotation(value.Bounds), value),
            LinkAnnotation value => CopyLink(value, sourcePageOffset, targetPageOffset),
            StampAnnotation value => new StampAnnotation(value.Bounds) { Name = value.Name },
            InkAnnotation value => CopyInk(value),
            FreeTextAnnotation value => new FreeTextAnnotation(value.Bounds)
            {
                Font = ContentClone.CopyFont(value.Font),
                TextColor = value.TextColor,
            },
            SquareAnnotation value => CopyShape(new SquareAnnotation(value.Bounds), value),
            CircleAnnotation value => CopyShape(new CircleAnnotation(value.Bounds), value),
            _ => throw new NotSupportedException($"Annotation type '{source.GetType().FullName}' is not supported."),
        };

        clone.Color = source.Color;
        clone.Opacity = source.Opacity;
        clone.Flags = source.Flags;
        clone.Contents = source.Contents;
        clone.Title = source.Title;
        clone.Appearance = CopyAppearance(source.Appearance);
        return clone;
    }

    private static int? Rebase(int? pageIndex, int sourcePageOffset, int targetPageOffset)
        => pageIndex is { } value ? value - sourcePageOffset + targetPageOffset : null;

    private static OutlineTarget? Rebase(OutlineTarget? target, int sourcePageOffset, int targetPageOffset)
        => target is { PageIndex: { } pageIndex }
            ? OutlineTarget.FromLoaded(pageIndex - sourcePageOffset + targetPageOffset, target.Fit, [.. target.FitArguments])
            : target;

    private static LinkAnnotation CopyLink(LinkAnnotation source, int sourcePageOffset, int targetPageOffset)
    {
        var resolvedNamedDestination = source.Destination is not null && source.TargetPageIndex is not null;
        return new LinkAnnotation(source.Bounds)
        {
            Uri = source.Uri,
            Destination = resolvedNamedDestination ? null : source.Destination,
            TargetPageIndex = Rebase(source.TargetPageIndex, sourcePageOffset, targetPageOffset),
            DestinationIsName = resolvedNamedDestination ? false : source.DestinationIsName,
            ResolvedTarget = Rebase(source.ResolvedTarget, sourcePageOffset, targetPageOffset),
        };
    }

    private static AnnotationAppearance? CopyAppearance(AnnotationAppearance? source)
    {
        if (source is null)
        {
            return null;
        }

        var target = new AnnotationAppearance();
        foreach (var element in source.Content)
        {
            target.Content.Add(element.DeepClone());
        }

        return target;
    }

    private static T CopyAreas<T>(T target, MarkupAnnotation source) where T : MarkupAnnotation
    {
        target.Areas.Clear();
        foreach (var area in source.Areas)
        {
            target.Areas.Add(area);
        }

        return target;
    }

    private static InkAnnotation CopyInk(InkAnnotation source)
    {
        var target = new InkAnnotation(source.Bounds) { StrokeWidth = source.StrokeWidth };
        foreach (var sourceStroke in source.Strokes)
        {
            var stroke = new InkStroke();
            foreach (var point in sourceStroke)
            {
                stroke.Add(point);
            }

            target.Strokes.Add(stroke);
        }

        return target;
    }

    private static T CopyShape<T>(T target, ShapeAnnotation source) where T : ShapeAnnotation
    {
        target.BorderWidth = source.BorderWidth;
        target.InteriorColor = source.InteriorColor;
        return target;
    }

}
