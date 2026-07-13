using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;
using static Radzen.Documents.Pdf.Content.ContentEmitter;

namespace Radzen.Documents.Pdf.Emit;

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

    private readonly Dictionary<StructureElement, List<ImageDraw>> taggedImages = [];
    private readonly Dictionary<StructureElement, List<TextDraw>> taggedTexts = [];
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
        taggedImages.Clear();
        taggedTexts.Clear();
        generatedPage = null;

        foreach (var phase in OrderedPhases)
        {
            Emit(phase);
        }

        return generatedPage!;
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
                structureTree.WriteTaggedContent(writer, pageIndex, taggedImages, taggedTexts);
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
            writer.WriteRaw(" BDC\n");
        }
    }

    private void EmitFills()
    {
        foreach (var fill in plan.Fills)
        {
            // A pattern colour space persists in the graphics state, so a gradient fill is
            // always scoped by q..Q to keep it from leaking onto the fills that follow.
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
                writer.WriteName(plan.RegisterPattern(gradient));
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
                writer.WriteNumber(fill.X);
                writer.WriteRaw(" ");
                writer.WriteNumber(fill.Y);
                writer.WriteRaw(" ");
                writer.WriteNumber(fill.Width);
                writer.WriteRaw(" ");
                writer.WriteNumber(fill.Height);
                writer.WriteRaw(" re f\n");
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
                Accumulate(taggedImages, element, image);
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
                Accumulate(taggedTexts, element, text);
            }
            else
            {
                WriteTextDraw(writer, text);
            }
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
            writer.WriteRaw(" BDC\n");
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

    // Stroke colour + line width, then any dash pattern. Edges and rounded strokes must
    // emit this byte-for-byte identically, so both go through here.
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

        var radians = watermark.Rotation * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var negSin = sin == 0 ? 0 : -sin;
        writer.WriteNumber(cos);
        writer.WriteRaw(" ");
        writer.WriteNumber(sin);
        writer.WriteRaw(" ");
        writer.WriteNumber(negSin);
        writer.WriteRaw(" ");
        writer.WriteNumber(cos);
        writer.WriteRaw(" ");
        writer.WriteNumber(watermark.CenterX);
        writer.WriteRaw(" ");
        writer.WriteNumber(watermark.CenterY);
        writer.WriteRaw(" cm\n");
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

    // Whether the page carries any non-structure (pagination/decorative) content that a
    // fully tagged document must wrap in /Artifact: backgrounds, borders, or header/footer
    // images and texts (the draws left untagged because they have no structure element).
    private static bool HasArtifactContent(PagePlan plan)
    {
        if (plan.Fills.Count > 0 || plan.Edges.Count > 0 || plan.RoundedStrokes.Count > 0)
        {
            return true;
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

    private static void Accumulate<T>(Dictionary<StructureElement, List<T>> map, StructureElement element, T draw)
    {
        if (!map.TryGetValue(element, out var list))
        {
            list = [];
            map[element] = list;
        }

        list.Add(draw);
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
