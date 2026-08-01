using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Output;

namespace Radzen.Documents.Pdf.Render;

internal sealed class StructureTreeBuilder(DocumentSemantics semantics, RenderRequest settings)
{
    private readonly Dictionary<SourceId, StructureElement> elementsBySource = [];
    private readonly Dictionary<SourceId, StructureElement> markerElementsBySource = [];
    private readonly Dictionary<SourceId, StructureElement> linkElementsBySource = [];
    private readonly Dictionary<SourceId, SemanticArtifactKind> artifactsBySource = [];
    private readonly SortedSet<string> unmappedRoles = new(StringComparer.Ordinal);
    private readonly Dictionary<StructureElement, List<PlannedStructureKid>> orderedKids = [];
    private readonly Dictionary<StructureElement, int> kidCursors = [];
    private Dictionary<StructureElement, StructureElement>? parents;
    private Dictionary<StructureElement, int>? childOrder;
    private StructureElement documentElement = null!;
    private int nextElementId;

    public StructureElement DocumentElement => documentElement;

    public ImmutableArray<string> UnmappedRoles => [.. unmappedRoles];

    public StructureElementSnapshot Capture(ImmutableArray<PageOutput> pages)
        => new StructureCapture(pages, orderedKids).Capture(documentElement);

    private sealed class StructureCapture(
        ImmutableArray<PageOutput> pages,
        Dictionary<StructureElement, List<PlannedStructureKid>> orderedKids)
    {
        private readonly Dictionary<StructureElement, StructureElementSnapshot> captured = [];

        public StructureElementSnapshot Capture(StructureElement element)
        {
            if (captured.TryGetValue(element, out var existing))
            {
                return existing;
            }

            var sourceKids = orderedKids[element];
            var kids = ImmutableArray.CreateBuilder<StructureKidSnapshot>(sourceKids.Count);
            foreach (var kid in sourceKids)
            {
                kids.Add(new StructureKidSnapshot(
                    kid.Child is { } child ? Capture(child) : null,
                    kid.Child is null ? pages[kid.PageIndex] : null,
                    kid.Mcid));
            }

            var snapshot = new StructureElementSnapshot(
                element.Id,
                element.Type,
                element.Alt,
                element.ActualText,
                element.Language,
                element.HeaderScope,
                element.RowSpan,
                element.ColumnSpan,
                kids.MoveToImmutable());
            captured.Add(element, snapshot);
            return snapshot;
        }
    }


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

            if (association.MarkerElement is { } markerIndex
                && materialized[markerIndex] is { } marker)
            {
                markerElementsBySource[association.Source] = marker;
            }

            if (association.LinkElement is { } linkIndex
                && materialized[linkIndex] is { } link)
            {
                linkElementsBySource[association.Source] = link;
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
            Id = nextElementId++,
            Type = StructureType(captured),
            Alt = captured.AlternateText,
            Language = captured.Language,
            // ISO 32000-1 14.9.4: the authored replacement text is the element's /ActualText.
            ActualText = captured.ReplacementText,
            HeaderScope = captured.HeaderScope,
            RowSpan = captured.RowSpan,
            ColumnSpan = captured.ColumnSpan,
        };
        orderedKids[element] = [];
        kidCursors[element] = 0;
        materialized[index] = element;
        foreach (var childIndex in captured.Children)
        {
            if (Materialize(childIndex, snapshot, materialized) is { } child)
            {
                element.Children.Add(child);
                orderedKids[element].Add(new PlannedStructureKid { Child = child });
            }
        }

        return element;
    }

    private string StructureType(in SemanticStructureNode node)
    {
        var headingLevel = node.ParagraphStyle is { } styleIndex
            ? semantics.Styles.Paragraphs[styleIndex].HeadingLevel
            : 0;

        if (headingLevel > 0)
        {
            return StandardType(node.Intent, headingLevel);
        }

        if (node.RoleIsDeclared && node.Role is { } declared && !Interpretable(declared))
        {
            unmappedRoles.Add(declared);
        }

        return node.Role is { } role
            && (settings.RoleMap.Contains(role) || (node.RoleIsDeclared && RoleMap.IsStandardType(role)))
            ? role
            : StandardType(node.Intent, headingLevel);
    }

    // ISO 14289-1:2014 7.1: non-standard structure types shall be mapped, in the structure tree root's role
    // map, to the nearest functionally equivalent standard type defined in ISO 32000-1:2008 14.8.4.
    private bool Interpretable(string role)
        => RoleMap.IsStandardType(role) || settings.RoleMap.Contains(role);

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
            SemanticIntent.CrossReference => "Reference",
            SemanticIntent.Link => "Link",
            SemanticIntent.Span => "Span",
            SemanticIntent.Caption => "Caption",
            SemanticIntent.TableHeaderGroup => "THead",
            SemanticIntent.TableBodyGroup => "TBody",
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, null),
        };

    private bool Materializes(SemanticStructureTier tier)
        => tier == SemanticStructureTier.Always || TaggingActive;

    public bool TaggingActive
        => settings.Accessibility != PdfUaConformance.None
            || settings.Conformance is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;

    public StructureElement? ElementOf(SourceId source)
        => elementsBySource.TryGetValue(source, out var element) ? element : null;

    public StructureElement? MarkerElementOf(SourceId source)
        => markerElementsBySource.TryGetValue(source, out var element) ? element : null;

    // ISO 14289-1 7.18.1: the object reference for a link annotation is a kid of the Link element itself.
    public StructureElement? LinkElementOf(SourceId source)
        => linkElementsBySource.TryGetValue(source, out var element) ? element : ElementOf(source);

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

        IndexTree();
        var touched = TouchedChildren(own.Keys);

        var subtreeStart = new Dictionary<StructureElement, int>();
        ComputeSubtreeStart(documentElement, own, touched, subtreeStart);

        Walk(documentElement, own, touched, subtreeStart, pageIndex, marks);
        return marks;
    }

    private void IndexTree()
    {
        if (parents is not null)
        {
            return;
        }

        parents = [];
        childOrder = [];

        var pending = new Stack<StructureElement>();
        pending.Push(documentElement);
        while (pending.Count > 0)
        {
            var element = pending.Pop();
            for (var index = 0; index < element.Children.Count; index++)
            {
                var child = element.Children[index];
                parents[child] = element;
                childOrder[child] = index;
                pending.Push(child);
            }
        }
    }

    private Dictionary<StructureElement, List<StructureElement>> TouchedChildren(
        Dictionary<StructureElement, List<TaggedDraw>>.KeyCollection elements)
    {
        var touched = new Dictionary<StructureElement, List<StructureElement>>();
        var seen = new HashSet<StructureElement>();

        foreach (var element in elements)
        {
            var current = element;
            while (seen.Add(current) && parents!.TryGetValue(current, out var parent))
            {
                if (!touched.TryGetValue(parent, out var children))
                {
                    children = [];
                    touched[parent] = children;
                }

                children.Add(current);
                current = parent;
            }
        }

        foreach (var children in touched.Values)
        {
            children.Sort((left, right) => childOrder![left].CompareTo(childOrder![right]));
        }

        return touched;
    }

    private static int ComputeSubtreeStart(
        StructureElement element,
        Dictionary<StructureElement, List<TaggedDraw>> own,
        Dictionary<StructureElement, List<StructureElement>> touched,
        Dictionary<StructureElement, int> subtreeStart)
    {
        var start = own.TryGetValue(element, out var draws) ? draws[0].Sequence : int.MaxValue;
        if (touched.TryGetValue(element, out var children))
        {
            foreach (var child in children)
            {
                var childStart = ComputeSubtreeStart(child, own, touched, subtreeStart);
                if (childStart < start)
                {
                    start = childStart;
                }
            }
        }

        if (start != int.MaxValue)
        {
            subtreeStart[element] = start;
        }

        return start;
    }

    private void Walk(
        StructureElement element,
        Dictionary<StructureElement, List<TaggedDraw>> own,
        Dictionary<StructureElement, List<StructureElement>> touched,
        Dictionary<StructureElement, int> subtreeStart,
        int pageIndex,
        Dictionary<int, TaggedMark> marks)
    {
        own.TryGetValue(element, out var draws);
        touched.TryGetValue(element, out var children);
        var next = 0;
        foreach (var child in children ?? [])
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

            Walk(child, own, touched, subtreeStart, pageIndex, marks);
            AdvancePast(element, child);
        }

        if (draws is not null && next < draws.Count)
        {
            AddMarks(element, draws, next, draws.Count, pageIndex, marks);
        }
    }

    private void AddMarks(
        StructureElement element,
        List<TaggedDraw> draws,
        int start,
        int end,
        int pageIndex,
        Dictionary<int, TaggedMark> marks)
    {
        for (var i = start; i < end; i++)
        {
            AddMark(element, pageIndex, marks[draws[i].Sequence].Mcid);
        }
    }

    private void AddMark(StructureElement element, int pageIndex, int mcid)
    {
        var kids = orderedKids[element];
        var cursor = kidCursors[element];
        kids.Insert(cursor, new PlannedStructureKid { PageIndex = pageIndex, Mcid = mcid });
        kidCursors[element] = cursor + 1;
    }

    private void AdvancePast(StructureElement element, StructureElement child)
    {
        var kids = orderedKids[element];
        if (Locate(kids, child, kidCursors[element]) is { } ahead)
        {
            kidCursors[element] = ahead + 1;
            return;
        }

        kidCursors[element] = Locate(kids, child, 0)
            ?? throw new InvalidOperationException("A structure child is missing from its parent's ordered kids.");
        kidCursors[element]++;
    }

    private static int? Locate(List<PlannedStructureKid> kids, StructureElement child, int from)
    {
        for (var i = from; i < kids.Count; i++)
        {
            if (ReferenceEquals(kids[i].Child, child))
            {
                return i;
            }
        }

        return null;
    }

}

internal readonly struct PlannedStructureKid
{
    public StructureElement? Child { get; init; }

    public int PageIndex { get; init; }

    public int Mcid { get; init; }
}

internal readonly struct TaggedDraw
{
    public required int Sequence { get; init; }

    public required StructureElement Element { get; init; }

}

internal readonly record struct TaggedMark(StructureElement Element, int Mcid);
