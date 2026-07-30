using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;
using static Radzen.Documents.Pdf.Content.ContentEmitter;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class PageContentFinalizer(StructureTreeBuilder structureTree, bool markArtifacts)
{
    private static readonly ContentPhase[] OrderedPhases =
    [
        ContentPhase.BeginArtifacts,
        ContentPhase.Fills,
        ContentPhase.StraightStrokes,
        ContentPhase.RoundedStrokes,
        ContentPhase.Images,
        ContentPhase.Text,
        ContentPhase.EndArtifacts,
        ContentPhase.TaggedContent,
        ContentPhase.Watermark,
        ContentPhase.Resources,
    ];

    internal static IReadOnlyList<PaintPhase> PaintOrder()
    {
        var phases = new List<PaintPhase>();
        foreach (var phase in OrderedPhases)
        {
            var scene = phase switch
            {
                ContentPhase.Fills => PaintPhase.Fill,
                ContentPhase.StraightStrokes or ContentPhase.RoundedStrokes => PaintPhase.Stroke,
                ContentPhase.Images => PaintPhase.Image,
                ContentPhase.Text => PaintPhase.Text,
                ContentPhase.Watermark => PaintPhase.Watermark,
                _ => (PaintPhase?)null,
            };

            if (scene is { } value && (phases.Count == 0 || phases[^1] != value))
            {
                phases.Add(value);
            }
        }

        return phases;
    }

    private readonly List<TaggedDraw> tagged = [];
    private ContentWriter writer = null!;
    private PagePlan plan = null!;
    private int pageIndex;
    private bool wrapArtifacts;
    private GeneratedPage? generatedPage;

    public GeneratedPage Finalize(PagePlan pagePlan, int index)
    {
        using var contentWriter = new ContentWriter();
        writer = contentWriter;
        plan = pagePlan;
        pageIndex = index;
        wrapArtifacts = markArtifacts && HasArtifactContent(plan);
        tagged.Clear();
        generatedPage = null;
        ApplyColorAlpha();

        foreach (var phase in OrderedPhases)
        {
            Emit(phase);
        }

        return generatedPage!;
    }

    private void ApplyColorAlpha()
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
            plan.Texts[i] = WithColorAlpha(plan.Texts[i], 1);
        }
    }

    private TextDraw WithColorAlpha(TextDraw text, double scale)
        => Translucent(text.Color, out var alpha)
            ? text with { ExtGState = plan.ApplyAlpha(text.ExtGState, alpha * scale) }
            : text;

    private static Matrix GradientMatrix(in FillDraw fill)
        => Matrix.FromComponents(1, 0, 0, -1, fill.X, fill.Y + fill.Height);

    private static bool Translucent(Color color, out double alpha)
    {
        alpha = color.A / 255.0;
        return color.A != 255;
    }

    private void Emit(ContentPhase phase)
    {
        switch (phase)
        {
            case ContentPhase.BeginArtifacts:
                BeginArtifacts();
                break;
            case ContentPhase.Fills:
                EmitFills();
                break;
            case ContentPhase.StraightStrokes:
                EmitStraightStrokes();
                break;
            case ContentPhase.RoundedStrokes:
                EmitRoundedStrokes();
                break;
            case ContentPhase.Images:
                EmitImages();
                break;
            case ContentPhase.Text:
                EmitText();
                break;
            case ContentPhase.EndArtifacts:
                EndArtifacts();
                break;
            case ContentPhase.TaggedContent:
                WriteTaggedContent();
                break;
            case ContentPhase.Watermark:
                EmitWatermark();
                break;
            case ContentPhase.Resources:
                PackageResources();
                break;
        }
    }

    private void BeginArtifacts()
    {
        if (wrapArtifacts)
        {
            writer.WriteName("Artifact");
            writer.WriteRaw(" BMC\n");
        }
    }

    private void EmitFills()
    {
        foreach (var fill in plan.Fills)
        {
            if (fill.Element is { } element)
            {
                tagged.Add(new TaggedDraw { Sequence = fill.Sequence, Element = element, Fill = fill });
                continue;
            }

            WriteFill(fill);
        }
    }

    private void WriteFill(in FillDraw fill)
    {
        {
            var grouped = fill.Clip is not null || fill.ExtGState is not null || fill.Gradient is not null;
            if (grouped)
            {
                writer.WriteRaw("q\n");
            }

            if (fill.ExtGState is { } fillState)
            {
                writer.WriteName(fillState);
                writer.WriteRaw(" gs\n");
            }

            if (fill.Clip is { } fillClip)
            {
                WriteClip(writer, fillClip, fill.ClipRadius);
            }

            if (fill.Gradient is { } gradient)
            {
                writer.WriteRaw("/Pattern cs\n");
                writer.WriteName(plan.RegisterPattern(gradient, GradientMatrix(fill)));
                writer.WriteRaw(" scn\n");
            }
            else
            {
                writer.WriteColor(fill.Color, "rg");
            }

            if (fill.Radius > 0)
            {
                WriteRoundedRect(writer, fill.X, fill.Y, fill.Width, fill.Height, fill.Radius);
                writer.WriteRaw("f\n");
            }
            else
            {
                WriteRectangle(writer, fill.X, fill.Y, fill.Width, fill.Height);
                writer.WriteRaw(" f\n");
            }

            if (grouped)
            {
                writer.WriteRaw("Q\n");
            }
        }
    }

    private void EmitStraightStrokes()
    {
        foreach (var edge in plan.Edges)
        {
            writer.WriteRaw("q\n");
            if (edge.ExtGState is { } edgeState)
            {
                writer.WriteName(edgeState);
                writer.WriteRaw(" gs\n");
            }

            if (edge.Clip is { } edgeClip)
            {
                WriteClip(writer, edgeClip, edge.ClipRadius);
            }

            WriteStrokeState(writer, edge.Color, edge.LineWidth, edge.Style);
            writer.WriteNumber(edge.X1);
            writer.WriteRaw(" ");
            writer.WriteNumber(edge.Y1);
            writer.WriteRaw(" m\n");
            writer.WriteNumber(edge.X2);
            writer.WriteRaw(" ");
            writer.WriteNumber(edge.Y2);
            writer.WriteRaw(" l\nS\nQ\n");
        }
    }

    private void EmitRoundedStrokes()
    {
        foreach (var rounded in plan.RoundedStrokes)
        {
            writer.WriteRaw("q\n");
            if (rounded.ExtGState is { } roundedState)
            {
                writer.WriteName(roundedState);
                writer.WriteRaw(" gs\n");
            }

            WriteStrokeState(writer, rounded.Color, rounded.LineWidth, rounded.Style);
            WriteRoundedRect(writer, rounded.X, rounded.Y, rounded.Width, rounded.Height, rounded.Radius);
            writer.WriteRaw("S\nQ\n");
        }
    }

    private void EmitImages()
    {
        foreach (var image in plan.Images)
        {
            if (image.Element is { } element)
            {
                tagged.Add(new TaggedDraw { Sequence = image.Sequence, Element = element, Image = image });
            }
            else
            {
                WriteImageDraw(writer, image);
            }
        }
    }

    private void EmitText()
    {
        foreach (var text in plan.Texts)
        {
            if (text.Element is { } element)
            {
                tagged.Add(new TaggedDraw { Sequence = text.Sequence, Element = element, Text = text });
            }
            else
            {
                WriteTextDraw(writer, text);
            }
        }
    }

    private void WriteTaggedContent()
    {
        foreach (var segment in structureTree.PlanTaggedContent(pageIndex, tagged))
        {
            writer.WriteName(segment.Element.Type);
            writer.WriteRaw(" <</MCID ");
            writer.WriteRaw(segment.Mcid.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteRaw(">> BDC\n");

            foreach (var draw in segment.Content)
            {
                if (draw.Fill is { } fill)
                {
                    WriteFill(fill);
                }
                else if (draw.Image is { } image)
                {
                    WriteImageDraw(writer, image);
                }
                else if (draw.Text is { } text)
                {
                    WriteTextDraw(writer, text);
                }
            }

            writer.WriteRaw("EMC\n");
        }
    }

    private void EndArtifacts()
    {
        if (wrapArtifacts)
        {
            writer.WriteRaw("EMC\n");
        }
    }

    private void EmitWatermark()
    {
        if (plan.Watermark is not { } watermark)
        {
            return;
        }

        if (markArtifacts)
        {
            writer.WriteName("Artifact");
            writer.WriteRaw(" BMC\n");
        }

        WriteWatermark(writer, watermark);

        if (markArtifacts)
        {
            writer.WriteRaw("EMC\n");
        }
    }

    private void PackageResources()
    {
        var usedFonts = new List<GeneratedFont>(plan.UsedFonts);
        var usedImages = new List<GeneratedImage>(plan.UsedImages);
        generatedPage = new GeneratedPage
        {
            Content = writer.ToArray(),
            Fonts = usedFonts,
            Images = usedImages,
            Links = [.. plan.Links],
            ExtGStates = [.. plan.ExtGStates],
            Patterns = [.. plan.Patterns],
        };
    }

    private static void WriteStrokeState(ContentWriter writer, Color color, double lineWidth, BorderStyle style)
    {
        writer.WriteColor(color, "RG");
        writer.WriteNumber(lineWidth);
        writer.WriteRaw(" w\n");
        WriteDashPattern(writer, style, lineWidth);
    }

    private static void WriteDashPattern(ContentWriter writer, BorderStyle style, double lineWidth)
    {
        if (style is not (BorderStyle.Dashed or BorderStyle.Dotted))
        {
            return;
        }

        var on = style == BorderStyle.Dashed ? 3.0 : 1.0;
        writer.WriteRaw("[");
        writer.WriteNumber(on * lineWidth);
        writer.WriteRaw(" ");
        writer.WriteNumber(on * lineWidth);
        writer.WriteRaw("] 0 d\n");
    }

    private static void WriteWatermark(ContentWriter writer, WatermarkDraw watermark)
    {
        writer.WriteRaw("q\n");
        if (watermark.ExtGState is { } state)
        {
            writer.WriteName(state);
            writer.WriteRaw(" gs\n");
        }

        WriteTransform(
            writer,
            WatermarkGeometry.Rotation(
                watermark.Rotation,
                watermark.CenterX,
                watermark.CenterY));
        if (watermark.Image is { } image)
        {
            WriteImageDraw(writer, image);
        }

        foreach (var text in watermark.Texts)
        {
            WriteTextDraw(writer, text);
        }

        writer.WriteRaw("Q\n");
    }

    private static bool HasArtifactContent(PagePlan plan)
    {
        if (plan.Edges.Count > 0 || plan.RoundedStrokes.Count > 0)
        {
            return true;
        }

        foreach (var fill in plan.Fills)
        {
            if (fill.Element is null)
            {
                return true;
            }
        }

        foreach (var image in plan.Images)
        {
            if (image.Element is null)
            {
                return true;
            }
        }

        foreach (var text in plan.Texts)
        {
            if (text.Element is null)
            {
                return true;
            }
        }

        return false;
    }

    private enum ContentPhase
    {
        BeginArtifacts,
        Fills,
        StraightStrokes,
        RoundedStrokes,
        Images,
        Text,
        EndArtifacts,
        TaggedContent,
        Watermark,
        Resources,
    }
}
