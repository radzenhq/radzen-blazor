using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class StructureTreeBuilder(DocumentSemantics semantics, CapturedRendererSettings settings)
{
    private readonly Dictionary<SourceId, StructureElement> elementsBySource = [];
    private readonly Dictionary<SourceId, StructureElement> markerElementsBySource = [];
    private StructureElement documentElement = null!;
    private bool hasUntaggedList;

    public StructureElement DocumentElement => documentElement;

    public bool HasUntaggedList => hasUntaggedList;

    public void Build() => BuildStructureTree();

    private void BuildStructureTree()
    {
        var snapshot = semantics.Structure;
        var materialized = new StructureElement?[snapshot.Nodes.Length];
        documentElement = Materialize(0, snapshot, materialized)!;

        foreach (var association in snapshot.Associations)
        {
            if (materialized[association.Element] is { } element)
            {
                elementsBySource[association.Source] = element;
            }

            if (association.MarkerElement is { } markerIndex
                && materialized[markerIndex] is { } marker)
            {
                markerElementsBySource[association.Source] = marker;
            }
        }

        if (settings.Accessibility == PdfUaConformance.None)
        {
            foreach (var list in snapshot.Lists)
            {
                if (Materializes(list.Tier))
                {
                    hasUntaggedList = true;
                    break;
                }
            }
        }
    }

    private StructureElement? Materialize(
        int index,
        SemanticStructureTree snapshot,
        StructureElement?[] materialized)
    {
        var captured = snapshot.Nodes[index];

        // ISO 14289-1 7.1: content that carries no meaning is real content of no interest to assistive
        // technology and is marked as an artifact rather than tagged as a structure element.
        if (captured.IsDecorative || !Materializes(captured.Tier))
        {
            return null;
        }

        var element = new StructureElement
        {
            Type = StructureType(captured),
            Alt = captured.AlternateText,
            ActualText = captured.ActualText,
            HeaderScope = captured.HeaderScope,
            RowSpan = captured.RowSpan,
            ColumnSpan = captured.ColumnSpan,
        };
        materialized[index] = element;
        foreach (var childIndex in captured.Children)
        {
            if (Materialize(childIndex, snapshot, materialized) is { } child)
            {
                element.Children.Add(child);
                element.Kids.Add(new StructureKid { Child = child });
            }
        }

        return element;
    }

    private string StructureType(in SemanticStructureNode node)
    {
        if (node.ParagraphStyle is not { } styleIndex)
        {
            return StandardType(node.Intent, 0);
        }

        var style = semantics.Styles.Paragraphs[styleIndex];
        return style.CustomRole is { } role && settings.RoleMap.Contains(role)
            ? role
            : StandardType(style.Intent, style.HeadingLevel);
    }

    // ISO 32000-1:2008 Table 333 (standard structure types) and 14.8.4.
    private static string StandardType(SemanticIntent intent, int headingLevel)
        => intent switch
        {
            SemanticIntent.Document => "Document",
            SemanticIntent.Section => "Sect",
            // ISO 32000-1 14.8.4.4: Div is the generic block-level grouping element.
            SemanticIntent.Group => "Div",
            SemanticIntent.Paragraph => "P",
            SemanticIntent.Heading => headingLevel is >= 1 and <= 6
                ? string.Create(CultureInfo.InvariantCulture, $"H{headingLevel}")
                : "H",
            SemanticIntent.List => "L",
            SemanticIntent.ListItem => "LI",
            SemanticIntent.ListLabel => "Lbl",
            SemanticIntent.ListBody => "LBody",
            SemanticIntent.Table => "Table",
            SemanticIntent.TableRow => "TR",
            SemanticIntent.TableHeaderCell => "TH",
            SemanticIntent.TableCell => "TD",
            SemanticIntent.Figure => "Figure",
            SemanticIntent.Navigation => "TOC",
            SemanticIntent.NavigationEntry => "TOCI",
            SemanticIntent.Link => "Link",
            _ => "Reference",
        };

    private bool Materializes(SemanticStructureTier tier)
        => tier switch
        {
            SemanticStructureTier.Always => true,
            SemanticStructureTier.Structural => TaggingActive,
            _ => settings.Accessibility != PdfUaConformance.None,
        };

    public bool TaggingActive
        => settings.Accessibility != PdfUaConformance.None
            || settings.Conformance is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;

    public StructureElement? ElementOf(SourceId source)
        => elementsBySource.TryGetValue(source, out var element) ? element : null;

    public StructureElement? MarkerElementOf(SourceId source)
        => markerElementsBySource.TryGetValue(source, out var element) ? element : null;

    public List<TaggedSegment> PlanTaggedContent(int pageIndex, List<TaggedDraw> draws)
    {
        var segments = new List<TaggedSegment>();
        if (draws.Count == 0)
        {
            return segments;
        }

        draws.Sort(static (a, b) => a.Sequence.CompareTo(b.Sequence));

        var own = new Dictionary<StructureElement, List<TaggedDraw>>();
        foreach (var draw in draws)
        {
            if (!own.TryGetValue(draw.Element, out var list))
            {
                list = [];
                own[draw.Element] = list;
            }

            list.Add(draw);
        }

        var subtreeStart = new Dictionary<StructureElement, int>();
        ComputeSubtreeStart(documentElement, own, subtreeStart);

        var mcid = 0;
        Walk(documentElement, own, subtreeStart, pageIndex, segments, ref mcid);
        return segments;
    }

    private static int ComputeSubtreeStart(
        StructureElement element,
        Dictionary<StructureElement, List<TaggedDraw>> own,
        Dictionary<StructureElement, int> subtreeStart)
    {
        var start = own.TryGetValue(element, out var draws) ? draws[0].Sequence : int.MaxValue;
        foreach (var child in element.Children)
        {
            var childStart = ComputeSubtreeStart(child, own, subtreeStart);
            if (childStart < start)
            {
                start = childStart;
            }
        }

        if (start != int.MaxValue)
        {
            subtreeStart[element] = start;
        }

        return start;
    }

    private static void Walk(
        StructureElement element,
        Dictionary<StructureElement, List<TaggedDraw>> own,
        Dictionary<StructureElement, int> subtreeStart,
        int pageIndex,
        List<TaggedSegment> segments,
        ref int mcid)
    {
        own.TryGetValue(element, out var draws);
        var next = 0;
        foreach (var child in element.Children)
        {
            if (!subtreeStart.TryGetValue(child, out var childStart))
            {
                continue;
            }

            var start = next;
            while (draws is not null && next < draws.Count && draws[next].Sequence < childStart)
            {
                next++;
            }

            if (next > start)
            {
                AddSegment(element, draws!, start, next, pageIndex, segments, ref mcid);
            }

            Walk(child, own, subtreeStart, pageIndex, segments, ref mcid);
            element.AdvancePast(child);
        }

        if (draws is not null && next < draws.Count)
        {
            AddSegment(element, draws, next, draws.Count, pageIndex, segments, ref mcid);
        }
    }

    private static void AddSegment(
        StructureElement element,
        List<TaggedDraw> draws,
        int start,
        int end,
        int pageIndex,
        List<TaggedSegment> segments,
        ref int mcid)
    {
        var content = new List<TaggedDraw>(end - start);
        for (var i = start; i < end; i++)
        {
            content.Add(draws[i]);
        }

        segments.Add(new TaggedSegment(element, mcid, content));
        element.AddMark(pageIndex, mcid);
        mcid++;
    }
}

internal readonly struct TaggedDraw
{
    public required int Sequence { get; init; }

    public required StructureElement Element { get; init; }

    public FillDraw? Fill { get; init; }

    public ImageDraw? Image { get; init; }

    public TextDraw? Text { get; init; }
}

internal sealed record TaggedSegment(StructureElement Element, int Mcid, List<TaggedDraw> Content);
