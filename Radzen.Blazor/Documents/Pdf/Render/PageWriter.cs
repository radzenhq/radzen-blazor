using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Geometry;
using static Radzen.Documents.Pdf.Content.ContentEmitter;
using static Radzen.Documents.Pdf.Render.DrawWriter;

namespace Radzen.Documents.Pdf.Render;

internal sealed class PageWriter(
    StructureTreeBuilder structureTree,
    bool markArtifacts,
    PagePlan plan,
    int pageIndex,
    IReadOnlyDictionary<EmittedFont, PlannedFont> fontPlans) : IDisposable
{
    private static readonly ContentPhase[] OrderedPhases =
    [
        ContentPhase.Fills,
        ContentPhase.StraightStrokes,
        ContentPhase.RoundedStrokes,
        ContentPhase.Images,
        ContentPhase.Text,
        ContentPhase.Watermark,
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
    private Dictionary<int, TaggedMark> taggedMarks = [];
    private readonly ContentWriter writer = new();
    private readonly IReadOnlyDictionary<EmittedFont, PlannedFont> plannedFonts = fontPlans;

    public PageOutput Finalize()
    {
        PageDrawTransformer.ApplyColorAlpha(plan);
        PlanMarkedContent();

        foreach (var phase in OrderedPhases)
        {
            Emit(phase);
        }

        return PackageResources();
    }

    private static Matrix GradientMatrix(in FillDraw fill)
        => BottomUpSpace.TopDownAt(fill.X, fill.Y, fill.Height);

    private void Emit(ContentPhase phase)
    {
        switch (phase)
        {
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
            case ContentPhase.Watermark:
                EmitWatermark();
                break;
        }
    }

    private void PlanMarkedContent()
    {
        foreach (var fill in plan.Fills)
        {
            if (fill.Element is { } element)
            {
                tagged.Add(new TaggedDraw { Sequence = fill.Sequence, Element = element });
            }
        }

        foreach (var rounded in plan.RoundedStrokes)
        {
            if (rounded.Element is { } element)
            {
                tagged.Add(new TaggedDraw { Sequence = rounded.Sequence, Element = element });
            }
        }

        foreach (var image in plan.Images)
        {
            if (image.Element is { } element)
            {
                tagged.Add(new TaggedDraw { Sequence = image.Sequence, Element = element });
            }
        }

        foreach (var text in plan.Texts)
        {
            if (text.Element is { } element)
            {
                tagged.Add(new TaggedDraw { Sequence = text.Sequence, Element = element });
            }
        }

        taggedMarks = structureTree.PlanTaggedContent(pageIndex, tagged);
    }

    private void EmitFills()
    {
        foreach (var fill in plan.Fills)
        {
            BeginMarkedDraw(fill.Element, fill.Artifact, fill.Sequence);
            WriteFill(fill);
            EndMarkedDraw(fill.Element, fill.Artifact);
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
            BeginArtifactDraw(edge.Artifact);
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
            EndArtifact();
        }
    }

    private void EmitRoundedStrokes()
    {
        foreach (var rounded in plan.RoundedStrokes)
        {
            BeginMarkedDraw(rounded.Element, rounded.Artifact, rounded.Sequence);
            writer.WriteRaw("q\n");
            if (rounded.ExtGState is { } roundedState)
            {
                writer.WriteName(roundedState);
                writer.WriteRaw(" gs\n");
            }

            WriteStrokeState(writer, rounded.Color, rounded.LineWidth, rounded.Style);
            WriteRoundedRect(writer, rounded.X, rounded.Y, rounded.Width, rounded.Height, rounded.Radius);
            writer.WriteRaw("S\nQ\n");
            EndMarkedDraw(rounded.Element, rounded.Artifact);
        }
    }

    private void EmitImages()
    {
        foreach (var image in plan.Images)
        {
            BeginMarkedDraw(image.Element, image.Artifact, image.Sequence);
            WriteImageDraw(writer, image);
            EndMarkedDraw(image.Element, image.Artifact);
        }
    }

    private void EmitText()
    {
        foreach (var text in plan.Texts)
        {
            BeginMarkedDraw(text.Element, text.Artifact, text.Sequence);
            WriteTextDraw(writer, text, plannedFonts[text.Font]);
            EndMarkedDraw(text.Element, text.Artifact);
        }
    }

    private void BeginMarkedDraw(
        StructureElement? element,
        SemanticArtifactKind? artifact,
        int sequence)
    {
        if (element is { } taggedElement)
        {
            if (artifact is not null)
            {
                throw new InvalidOperationException(
                    "A page draw cannot be both structure content and an artifact.");
            }

            if (!taggedMarks.TryGetValue(sequence, out var mark)
                || !ReferenceEquals(mark.Element, taggedElement))
            {
                throw new InvalidOperationException(
                    "A structured page draw has no planned marked-content reference.");
            }

            writer.WriteName(taggedElement.Type);
            writer.WriteRaw(" <</MCID ");
            writer.WriteRaw(mark.Mcid.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteRaw(">> BDC\n");
            return;
        }

        if (artifact is { } artifactKind)
        {
            BeginArtifact(artifactKind);
            return;
        }

        if (markArtifacts)
        {
            throw new InvalidOperationException(
                "Tagged page content must resolve to a structure element or an explicit artifact classification.");
        }
    }

    private void EndMarkedDraw(StructureElement? element, SemanticArtifactKind? artifact)
    {
        if (element is not null || (artifact is not null && markArtifacts))
        {
            writer.WriteRaw("EMC\n");
        }
    }

    private void BeginArtifact(SemanticArtifactKind artifact)
    {
        if (!markArtifacts)
        {
            return;
        }

        writer.WriteName("Artifact");
        if (artifact == SemanticArtifactKind.Pagination)
        {
            // ISO 32000-1 14.8.2.2: headers and footers are pagination artifacts.
            writer.WriteRaw(" <</Type /Pagination>> BDC\n");
        }
        else
        {
            writer.WriteRaw(" BMC\n");
        }
    }

    private void BeginArtifactDraw(SemanticArtifactKind? artifact)
    {
        if (artifact is { } kind)
        {
            BeginArtifact(kind);
        }
        else if (markArtifacts)
        {
            throw new InvalidOperationException(
                "Tagged page content must resolve to a structure element or an explicit artifact classification.");
        }
    }

    private void EndArtifact()
    {
        if (markArtifacts)
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
            BeginArtifact(watermark.Artifact);
        }

        WriteWatermark(writer, watermark);

        if (markArtifacts)
        {
            EndArtifact();
        }
    }

    private PageOutput PackageResources()
    {
        var fonts = ImmutableArray.CreateBuilder<OutputFont>(plan.UsedFonts.Count);
        foreach (var font in plan.UsedFonts)
        {
            fonts.Add(plannedFonts[font].Output);
        }

        return new PageOutput(
            writer.ToArray(),
            fonts.MoveToImmutable(),
            [.. plan.UsedImages],
            [.. plan.Links],
            [.. plan.ExtGStates],
            [.. plan.Patterns]);
    }

    private void WriteWatermark(ContentWriter writer, WatermarkDraw watermark)
    {
        var transform = WatermarkGeometry.Rotation(
            watermark.Rotation,
            watermark.CenterX,
            watermark.CenterY);
        ContentEmitter.WriteWatermark(
            writer,
            watermark.ExtGState,
            transform,
            output =>
            {
                if (watermark.Image is { } image)
                {
                    WriteImageDraw(output, image);
                }
            },
            output =>
            {
                foreach (var text in watermark.Texts)
                {
                    WriteTextDraw(output, text, plannedFonts[text.Font]);
                }
            });
    }

    private enum ContentPhase
    {
        Fills,
        StraightStrokes,
        RoundedStrokes,
        Images,
        Text,
        Watermark,
    }

    public void Dispose() => writer.Dispose();
}
