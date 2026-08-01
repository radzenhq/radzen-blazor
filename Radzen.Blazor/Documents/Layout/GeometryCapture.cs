using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal static class GeometryCapture
{
    public static FontPaint Font(Font font)
        => FontPaintCapture.Capture(font);

    public static FragmentPaint Fragment(Inline inline, Font font, LayoutCaptureContext capture)
        => Fragment(inline, Font(font), capture);

    public static FragmentPaint Fragment(
        Inline inline,
        in FontPaint font,
        LayoutCaptureContext capture)
        => inline is TextInline text
            ? TextFragment(text, font)
            : ImageFragment((InlineImage)inline, font, capture);

    private static FragmentPaint TextFragment(TextInline run, in FontPaint font) => new()
    {
        Font = font,
        Opacity = run.Opacity,
        LetterSpacing = run.LetterSpacing.Point,
        WordSpacing = run.WordSpacing.Point,
        HorizontalScale = run.HorizontalScale,
        ScriptScale = run.ScriptScale,
        Rise = run.ScriptRise(font.Size),
        IsScript = run.VerticalAlignment != RunVerticalAlignment.None,
        Invisible = run.Invisible,
        Link = run.Link,
        LinkToAnchor = run.LinkToAnchor,
        Anchor = run.Anchor,
    };

    private static FragmentPaint ImageFragment(
        InlineImage image,
        in FontPaint font,
        LayoutCaptureContext capture) => new()
    {
        Font = font,
        Opacity = image.Opacity,
        LetterSpacing = 0,
        WordSpacing = 0,
        HorizontalScale = 1,
        ScriptScale = 1,
        Rise = 0,
        IsScript = false,
        Invisible = false,
        Link = image.Link,
        LinkToAnchor = image.LinkToAnchor,
        Anchor = image.Anchor,
        InlineImage = ImagePaint(image, capture),
    };

    public static ImagePaint Image(Image image, LayoutCaptureContext capture) => new()
    {
        Data = capture.Image(image.Data),
        Opacity = image.Opacity,
        Interpolate = image.Interpolate,
    };

    public static TableDecoration Table(Table table, double additionalLeftIndent = 0)
    {
        var backgrounds = ImmutableArray.CreateBuilder<Color?>(table.Rows.Count);
        for (var i = 0; i < table.Rows.Count; i++)
        {
            backgrounds.Add(table.Rows[i].Background);
        }

        var borders = table.Borders;
        return new TableDecoration
        {
            LeftIndent = additionalLeftIndent + table.LeftIndent.Point,
            CornerRadius = table.CornerRadius.Point,
            Frame = new BoxStyle
            {
                Top = Edge(borders.Top),
                Right = Edge(borders.Right),
                Bottom = Edge(borders.Bottom),
                Left = Edge(borders.Left),
            },
            RowBackgrounds = backgrounds.MoveToImmutable(),
        };
    }

    public static GradientPaint? Gradient(
        GradientBrush? brush,
        GradientReference reference,
        LayoutCaptureContext capture)
    {
        if (brush is null)
        {
            return null;
        }

        var stops = ImmutableArray.CreateBuilder<GradientStopPaint>(brush.Stops.Count);
        foreach (var stop in brush.Stops)
        {
            stops.Add(new GradientStopPaint(stop.Offset, stop.Color));
        }

        var identity = capture.Paint(brush);
        return brush switch
        {
            LinearGradient linear => new GradientPaint(
                identity,
                GradientPaintKind.Linear,
                reference.X(linear.X0), reference.Y(linear.Y0), 0,
                reference.X(linear.X1), reference.Y(linear.Y1), 0,
                stops.MoveToImmutable()),
            RadialGradient radial => new GradientPaint(
                identity,
                GradientPaintKind.Radial,
                reference.X(radial.X0), reference.Y(radial.Y0), reference.Radius(radial.R0),
                reference.X(radial.X1), reference.Y(radial.Y1), reference.Radius(radial.R1),
                stops.MoveToImmutable()),
            _ => throw new NotSupportedException($"Unsupported gradient kind '{brush.GetType()}'."),
        };
    }

    public static BoxShadowPaint? Shadow(BoxShadow? shadow)
        => shadow is null
            ? null
            : new BoxShadowPaint(
                shadow.Color,
                shadow.BlurRadius.Point,
                shadow.OffsetX.Point,
                shadow.OffsetY.Point,
                shadow.Spread.Point);

    public static BoxStyle Box(
        Container container,
        double width,
        double height,
        LayoutCaptureContext capture) => new()
    {
        Background = container.Background,
        BackgroundGradient = Gradient(
            container.BackgroundGradient,
            GradientReference.Box(width, height),
            capture),
        Top = Edge(container.Borders.Top),
        Right = Edge(container.Borders.Right),
        Bottom = Edge(container.Borders.Bottom),
        Left = Edge(container.Borders.Left),
        CornerRadius = container.CornerRadius.Point,
        Shadow = Shadow(container.Shadow),
        Blend = container.BlendMode,
    };

    public static ResolvedEdge? Edge(Border edge)
    {
        // Capture policy: setting a width opts an otherwise unstyled edge into a solid border,
        // while selecting a style with zero width uses the historical hairline width.
        var style = edge.Style;
        if (style == BorderStyle.None && edge.Width.Point > 0)
        {
            style = BorderStyle.Solid;
        }

        if (style == BorderStyle.None)
        {
            return null;
        }

        var width = edge.Width.Point;
        return new ResolvedEdge(edge.Color, width > 0 ? width : 0.5, style);
    }

    public static LaidOutDocumentInfo DocumentInfo(DocumentInfo info) => new()
    {
        Title = info.Title,
        Author = info.Author,
        Subject = info.Subject,
        Keywords = info.Keywords,
        Creator = info.Creator,
        CreationDate = info.CreationDate,
        ModificationDate = info.ModificationDate,
    };

    public static CapturedGlyphRun PositionSpans(in CapturedGlyphRun run, in FragmentPaint paint)
    {
        if (run.Spans.IsDefaultOrEmpty)
        {
            return run;
        }

        var spans = ImmutableArray.CreateBuilder<CapturedGlyphSpan>(run.Spans.Length);
        var x = 0.0;
        foreach (var span in run.Spans)
        {
            spans.Add(span with { XOffset = x });
            x += RunTextAdvance.Calculate(
                span.Advance,
                span.GlyphCount,
                span.WordSpaceCount,
                paint,
                trailingCharacterSpacing: true);
        }

        return run with { Spans = spans.MoveToImmutable() };
    }

    private static InlineImagePaint ImagePaint(InlineImage image, LayoutCaptureContext capture)
    {
        var (width, height) = capture.Probes.Measure(image);
        return new InlineImagePaint
        {
            Key = capture.Source(image),
            Data = capture.Image(image.Data),
            Width = width,
            Height = height,
        };
    }
}
