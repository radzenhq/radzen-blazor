using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class StructureTreeBuilder(DocumentSemantics semantics, RenderRequest settings)
{
    private readonly Dictionary<SourceId, StructureElement> elementsBySource = [];
    private readonly Dictionary<SourceId, StructureElement> markerElementsBySource = [];
    private readonly Dictionary<SourceId, SemanticArtifactKind> artifactsBySource = [];
    private StructureElement documentElement = null!;

    public StructureElement DocumentElement => documentElement;


    public void Build() => BuildStructureTree();

    private void BuildStructureTree()
    {
        var snapshot = semantics.Structure;
        var materialized = new StructureElement?[snapshot.Nodes.Length];
        documentElement = Materialize(0, snapshot, materialized)!;

        foreach (var artifact in snapshot.Artifacts)
        {
            artifactsBySource[artifact.Source] = artifact.Kind;
        }

        foreach (var association in snapshot.Associations)
        {
            if (materialized[association.Element] is { } element)
            {
                elementsBySource[association.Source] = element;
            }
            else if (snapshot.Nodes[association.Element].IsDecorative)
            {
                artifactsBySource[association.Source] = SemanticArtifactKind.Decorative;
            }

            if (association.MarkerElement is { } markerIndex
                && materialized[markerIndex] is { } marker)
            {
                markerElementsBySource[association.Source] = marker;
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

    public SemanticArtifactKind? ArtifactOf(SourceId source)
        => artifactsBySource.TryGetValue(source, out var kind) ? kind : null;

    public Dictionary<int, TaggedMark> PlanTaggedContent(int pageIndex, List<TaggedDraw> draws)
    {
        var marks = new Dictionary<int, TaggedMark>();
        if (draws.Count == 0)
        {
            return marks;
        }

        for (var mcid = 0; mcid < draws.Count; mcid++)
        {
            var draw = draws[mcid];
            marks.Add(draw.Sequence, new TaggedMark(draw.Element, mcid));
        }

        // ISO 32000-1 14.7.4.4: structure kids may reference MCIDs in reading order
        // independently of the marked-content sequence order in the page stream.
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

        Walk(documentElement, own, subtreeStart, pageIndex, marks);
        return marks;
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
        Dictionary<int, TaggedMark> marks)
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
                AddMarks(element, draws!, start, next, pageIndex, marks);
            }

            Walk(child, own, subtreeStart, pageIndex, marks);
            element.AdvancePast(child);
        }

        if (draws is not null && next < draws.Count)
        {
            AddMarks(element, draws, next, draws.Count, pageIndex, marks);
        }
    }

    private static void AddMarks(
        StructureElement element,
        List<TaggedDraw> draws,
        int start,
        int end,
        int pageIndex,
        Dictionary<int, TaggedMark> marks)
    {
        for (var i = start; i < end; i++)
        {
            element.AddMark(pageIndex, marks[draws[i].Sequence].Mcid);
        }
    }

}

internal readonly struct TaggedDraw
{
    public required int Sequence { get; init; }

    public required StructureElement Element { get; init; }

}

internal readonly record struct TaggedMark(StructureElement Element, int Mcid);
