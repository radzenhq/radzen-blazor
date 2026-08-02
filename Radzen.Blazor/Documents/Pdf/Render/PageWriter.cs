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
    PagePlan plan,
    int pageIndex,
    IReadOnlyDictionary<EmittedFont, PlannedFont> fontPlans) : IDisposable
{
    private readonly bool markArtifacts = structureTree.TaggingActive;
    private readonly List<TaggedDraw> tagged = [];
    private Dictionary<int, TaggedMark> taggedMarks = [];
    private readonly ContentWriter writer = new();
    private readonly IReadOnlyDictionary<EmittedFont, PlannedFont> plannedFonts = fontPlans;

    public PageOutput Finalize()
    {
        PageDrawTransformer.ApplyColorAlpha(plan);
        PlanMarkedContent();

        foreach (var draw in OrderedDraws())
        {
            Emit(draw);
        }

        return PackageResources();
    }

    private static Matrix GradientMatrix(in FillDraw fill)
        => BottomUpSpace.TopDownAt(fill.X, fill.Y, fill.Height);

    private void Emit(in PageDrawReference draw)
    {
        switch (draw.Phase)
        {
            case PaintPhase.Fills:
                EmitFill(plan.Fills[draw.Index]);
                break;
            case PaintPhase.StraightStrokes:
                EmitStraightStroke(plan.Edges[draw.Index]);
                break;
            case PaintPhase.RoundedStrokes:
                EmitRoundedStroke(plan.RoundedStrokes[draw.Index]);
                break;
            case PaintPhase.Images:
                EmitImage(plan.Images[draw.Index]);
                break;
            case PaintPhase.Text:
                EmitText(plan.Texts[draw.Index]);
                break;
            case PaintPhase.Watermark:
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

        // ISO 32000-1 14.7.4.3: a widget annotation joins the structure tree in its reading-order
        // position through an object reference rather than through marked content of its own.
        for (var widget = 0; widget < plan.Widgets.Count; widget++)
        {
            if (plan.Widgets[widget].Element is { } element)
            {
                tagged.Add(new TaggedDraw
                {
                    Sequence = plan.Widgets[widget].Sequence,
                    Element = element,
                    Annotation = widget,
                });
            }
        }

        taggedMarks = structureTree.PlanTaggedContent(pageIndex, [.. tagged]);
    }

    private void EmitFill(in FillDraw fill)
    {
        BeginMarkedDraw(fill.Element, fill.Artifact, fill.Sequence);
        WriteFill(fill);
        EndMarkedDraw(fill.Element, fill.Artifact);
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
                writer.WriteName(plan.Resources.RegisterPattern(gradient, GradientMatrix(fill)));
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

    private void EmitStraightStroke(in EdgeDraw edge)
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

    private void EmitRoundedStroke(in RoundedStrokeDraw rounded)
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

    private void EmitImage(in ImageDraw image)
    {
        BeginMarkedDraw(image.Element, image.Artifact, image.Sequence);
        WriteImageDraw(writer, image);
        EndMarkedDraw(image.Element, image.Artifact);
    }

    private void EmitText(in TextDraw text)
    {
        BeginMarkedDraw(text.Element, text.Artifact, text.Sequence);
        WriteTextDraw(writer, text, plannedFonts[text.Font]);
        EndMarkedDraw(text.Element, text.Artifact);
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
        var fonts = ImmutableArray.CreateBuilder<OutputFont>(plan.Resources.UsedFonts.Count);
        foreach (var font in plan.Resources.UsedFonts)
        {
            fonts.Add(plannedFonts[font].Output);
        }

        var widgets = ImmutableArray.CreateBuilder<OutputWidget>(plan.Widgets.Count);
        foreach (var widget in plan.Widgets)
        {
            widgets.Add(new OutputWidget(
                widget.X,
                widget.Bottom,
                widget.Field,
                widget.Font,
                widget.Element?.Id));
        }

        return new PageOutput(
            writer.ToArray(),
            fonts.MoveToImmutable(),
            [.. plan.Resources.UsedImages],
            [.. plan.Links],
            [.. plan.Resources.ExtGStates],
            [.. plan.Resources.Patterns],
            widgets.MoveToImmutable());
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

    private List<PageDrawReference> OrderedDraws()
    {
        var draws = AllDraws();
        draws.Sort(CompareDraws);
        return draws;
    }

    private List<PageDrawReference> AllDraws()
    {
        var draws = new List<PageDrawReference>(
            plan.Fills.Count + plan.Edges.Count + plan.RoundedStrokes.Count
            + plan.Images.Count + plan.Texts.Count + (plan.Watermark is null ? 0 : 1));
        foreach (var phase in PdfPaintOrder.Phases)
        {
            var count = phase switch
            {
                PaintPhase.Fills => plan.Fills.Count,
                PaintPhase.StraightStrokes => plan.Edges.Count,
                PaintPhase.RoundedStrokes => plan.RoundedStrokes.Count,
                PaintPhase.Images => plan.Images.Count,
                PaintPhase.Text => plan.Texts.Count,
                PaintPhase.Watermark => plan.Watermark is null ? 0 : 1,
                _ => 0,
            };
            for (var index = 0; index < count; index++)
            {
                draws.Add(new PageDrawReference(phase, index, StackOf(phase, index)));
            }
        }

        return draws;
    }

    private static int CompareDraws(PageDrawReference left, PageDrawReference right)
    {
        if (left.Stack is not { } leftStack)
        {
            return right.Stack is null ? CompareWithinStack(left, right) : 1;
        }

        if (right.Stack is not { } rightStack)
        {
            return -1;
        }

        var layer = leftStack.Layer.CompareTo(rightStack.Layer);
        if (layer != 0)
        {
            return layer;
        }

        var order = leftStack.Order.CompareTo(rightStack.Order);
        return order != 0 ? order : CompareWithinStack(left, right);
    }

    private static int CompareWithinStack(PageDrawReference left, PageDrawReference right)
    {
        var phase = left.Phase.CompareTo(right.Phase);
        return phase != 0 ? phase : left.Index.CompareTo(right.Index);
    }

    private PaintStack? StackOf(PaintPhase phase, int index)
        => phase switch
        {
            PaintPhase.Fills => plan.Fills[index].Stack,
            PaintPhase.StraightStrokes => plan.Edges[index].Stack,
            PaintPhase.RoundedStrokes => plan.RoundedStrokes[index].Stack,
            PaintPhase.Images => plan.Images[index].Stack,
            PaintPhase.Text => plan.Texts[index].Stack,
            _ => null,
        };

    private readonly record struct PageDrawReference(PaintPhase Phase, int Index, PaintStack? Stack);

    public void Dispose() => writer.Dispose();
}
