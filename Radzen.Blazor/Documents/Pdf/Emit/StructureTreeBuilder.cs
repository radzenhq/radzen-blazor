using System;
using System.Collections.Generic;
using System.Globalization;
using static Radzen.Documents.Pdf.Content.ContentEmitter;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf.Emit;

// Builds the logical structure tree that mirrors the authoring DOM, assigns each element
// its DFS pre-order rank, and emits the per-page tagged content (BDC/EMC) in that order.
internal sealed class StructureTreeBuilder(DocumentBuilder builder, StyleResolution resolution)
{
    private readonly Dictionary<object, StructureElement> blockElements = [];
    private readonly Dictionary<StructureElement, int> structureOrder = [];
    private StructureElement documentElement = null!;
    private bool hasUntaggedList;

    public StructureElement DocumentElement => documentElement;

    // True when the authoring DOM has a List the tree left untagged (lists are only mapped
    // for PDF/UA). PDF/A Level-A rejects such untagged content, so the conformance writer
    // fails loud instead of advertising a conformance it does not meet.
    public bool HasUntaggedList => hasUntaggedList;

    public void Build()
    {
        BuildStructureTree();
        IndexStructure();
    }

    // The logical structure tree mirrors the authoring DOM in authoring order:
    // Document -> Sect per Section -> P (or H1..H6 by StyleName), Table -> TR ->
    // TH/TD, Figure per image. Header and footer content stays unmapped (untagged).
    private void BuildStructureTree()
    {
        documentElement = new StructureElement { Type = "Document" };
        foreach (var section in builder.Sections)
        {
            var sect = new StructureElement { Type = "Sect" };
            documentElement.Children.Add(sect);
            foreach (var block in section.Blocks)
            {
                MapBlock(block, sect);
            }
        }
    }

    // Assign each structure element its DFS pre-order rank once so per-page tagged emission can
    // order that page's content-bearing elements without re-walking the whole tree per page.
    private void IndexStructure()
    {
        var index = 0;
        var stack = new Stack<StructureElement>();
        stack.Push(documentElement);
        while (stack.Count > 0)
        {
            var element = stack.Pop();
            structureOrder[element] = index++;
            for (var c = element.Children.Count - 1; c >= 0; c--)
            {
                stack.Push(element.Children[c]);
            }
        }
    }

    private void MapBlock(Block block, StructureElement parent) => block.Accept(mapper, parent);

    private Mapper? cachedMapper;

    private Mapper mapper => cachedMapper ??= new Mapper(this, builder);

    // True when the output is fully tagged (PDF/UA or PDF/A Level-A). Real content that
    // would otherwise be emitted as /Artifact (a Container's children) must be tagged then,
    // so it is not hidden from assistive technology while the file claims conformance.
    private bool TaggingActive
        => builder.PdfUA
            || builder.Conformance is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;

    // Maps each block into the tagged structure tree. The types that carry no logical
    // Blocks with no structure of their own are mapped explicitly (not by Default) so an
    // unmapped block type fails loud rather than vanishing from accessible output. A Stack
    // Container has no element itself but its children are descended into and mapped in place.
    private sealed class Mapper(StructureTreeBuilder tree, DocumentBuilder builder) : BlockVisitor<StructureElement, Nothing>
    {
        protected override Nothing Default(Block block, StructureElement parent)
            => throw new NotSupportedException(
                $"Block type '{block.GetType().FullName}' is not mapped into the tagged structure tree. "
                + "Add a Visit overload for it to this block visitor so it cannot silently vanish from accessible output.");

        public override Nothing Visit(Paragraph paragraph, StructureElement parent)
        {
            var p = new StructureElement { Type = tree.StructureTypeFor(paragraph.StyleName) };
            parent.Children.Add(p);
            tree.blockElements[paragraph] = p;
            return default;
        }

        public override Nothing Visit(Table table, StructureElement parent)
        {
            var element = new StructureElement { Type = "Table" };
            parent.Children.Add(element);
            foreach (var row in table.Rows)
            {
                var tr = new StructureElement { Type = "TR" };
                element.Children.Add(tr);
                foreach (var cell in row.Cells)
                {
                    var td = new StructureElement { Type = row.IsHeader ? "TH" : "TD" };
                    tr.Children.Add(td);
                    tree.blockElements[cell] = td;
                }
            }

            return default;
        }

        public override Nothing Visit(Image image, StructureElement parent)
        {
            var figure = new StructureElement
            {
                Type = "Figure",
                Alt = image.AlternateText,
                ActualText = image.ActualText,
            };
            parent.Children.Add(figure);
            tree.blockElements[image] = figure;
            return default;
        }

        public override Nothing Visit(List list, StructureElement parent)
        {
            if (builder.PdfUA)
            {
                tree.MapList(list, parent);
            }
            else
            {
                tree.hasUntaggedList = true;
            }

            return default;
        }

        public override Nothing Visit(PageBreak block, StructureElement parent) => default;

        public override Nothing Visit(Container block, StructureElement parent)
        {
            // Overlay (and rotated) containers are lowered onto a different emit path that
            // does not resolve per-child structure elements, so only Stack containers - whose
            // children flow through the tagged box-content path - are descended into here.
            if (tree.TaggingActive && block.Layout == ContainerLayout.Stack)
            {
                foreach (var child in block.Blocks)
                {
                    tree.MapBlock(child, parent);
                }
            }

            return default;
        }

        public override Nothing Visit(Barcode block, StructureElement parent) => default;

        public override Nothing Visit(QrCode block, StructureElement parent) => default;

        public override Nothing Visit(TableOfContents block, StructureElement parent) => default;
    }

    // PDF/UA list structure (ISO 32000-1 14.8.4.3.3): L -> LI -> {Lbl, LBody}. Each item's
    // Lbl and LBody are stashed on the ListItem so the paginator's synthesized marker
    // paragraph can tag its marker into Lbl and its content into LBody. A nested list is
    // built inside its parent item's LBody. Only built for tagged (PDF/UA) output.
    private void MapList(List list, StructureElement parent)
    {
        var l = new StructureElement { Type = "L" };
        parent.Children.Add(l);
        foreach (var item in list.Items)
        {
            var li = new StructureElement { Type = "LI" };
            l.Children.Add(li);
            var lbl = new StructureElement { Type = "Lbl" };
            var lbody = new StructureElement { Type = "LBody" };
            li.Children.Add(lbl);
            li.Children.Add(lbody);
            resolution.SetListItemElements(item, lbl, lbody);
            if (item.NestedList is { } nested)
            {
                MapList(nested, lbody);
            }
        }
    }

    // A recognized heading style keeps its H1..H6 mapping. Otherwise a style name declared
    // as a custom role becomes that role verbatim (emitted with a /RoleMap to its standard
    // type); everything else stays P. With no declared roles this is exactly HeadingType, so
    // default output is unchanged.
    private string StructureTypeFor(string? styleName)
    {
        var type = HeadingType(styleName);
        if (type == "P" && styleName is not null && builder.RoleMap.Contains(styleName))
        {
            return styleName;
        }

        return type;
    }

    private static string HeadingType(string? styleName)
    {
        if (styleName is null)
        {
            return "P";
        }

        if (styleName.Length == 8
            && styleName.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
            && styleName[7] is >= '1' and <= '6')
        {
            return Heading(styleName[7]);
        }

        if (styleName.Length == 2
            && (styleName[0] == 'H' || styleName[0] == 'h')
            && styleName[1] is >= '1' and <= '6')
        {
            return Heading(styleName[1]);
        }

        return "P";
    }

    private static string Heading(char level) => level switch
    {
        '1' => "H1",
        '2' => "H2",
        '3' => "H3",
        '4' => "H4",
        '5' => "H5",
        _ => "H6",
    };

    public StructureElement? ElementOf(object block)
    {
        // A synthesized list-item paragraph tags its content into its LBody element (carried in
        // the per-save StyleResolution, keyed by that synthesized paragraph).
        if (block is Paragraph paragraph && resolution.BodyElementOf(paragraph) is { } body)
        {
            return body;
        }

        return blockElements.TryGetValue(block, out var element) ? element : null;
    }

    // The Lbl element a list-item paragraph's marker fragment tags into, or null.
    public StructureElement? MarkerElementOf(object block)
        => block is Paragraph paragraph ? resolution.LabelElementOf(paragraph) : null;

    // Emits only the elements that carry content on this page, ordered by their DFS pre-order rank so
    // the byte output matches a full-tree pre-order walk without the per-page O(elements) recursion.
    public void WriteTaggedContent(
        ContentWriter writer,
        int pageIndex,
        Dictionary<StructureElement, List<ImageDraw>> taggedImages,
        Dictionary<StructureElement, List<TextDraw>> taggedTexts)
    {
        var elements = new List<StructureElement>(taggedImages.Count + taggedTexts.Count);
        foreach (var element in taggedImages.Keys)
        {
            elements.Add(element);
        }

        foreach (var element in taggedTexts.Keys)
        {
            if (!taggedImages.ContainsKey(element))
            {
                elements.Add(element);
            }
        }

        elements.Sort((a, b) => structureOrder[a].CompareTo(structureOrder[b]));

        var mcid = 0;
        foreach (var element in elements)
        {
            var hasImages = taggedImages.TryGetValue(element, out var elementImages);
            var hasTexts = taggedTexts.TryGetValue(element, out var elementTexts);
            writer.WriteName(element.Type);
            writer.WriteRaw(" <</MCID ");
            writer.WriteRaw(mcid.ToString(CultureInfo.InvariantCulture));
            writer.WriteRaw(">> BDC\n");

            if (hasImages)
            {
                foreach (var image in elementImages!)
                {
                    WriteImageDraw(writer, image);
                }
            }

            if (hasTexts)
            {
                foreach (var text in elementTexts!)
                {
                    WriteTextDraw(writer, text);
                }
            }

            writer.WriteRaw("EMC\n");
            element.Marks.Add((pageIndex, mcid));
            mcid++;
        }
    }
}
