using System;
using System.Collections.Generic;
using System.Globalization;
using static Radzen.Documents.Pdf.Content.ContentEmitter;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf.Emit;

internal sealed class StructureTreeBuilder(DocumentBuilder builder, StyleResolution resolution)
{
    private readonly Dictionary<object, StructureElement> blockElements = [];
    private readonly Dictionary<StructureElement, int> structureOrder = [];
    private StructureElement documentElement = null!;
    private bool hasUntaggedList;

    public StructureElement DocumentElement => documentElement;

    public bool HasUntaggedList => hasUntaggedList;

    public void Build()
    {
        BuildStructureTree();
        IndexStructure();
    }

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

    private bool TaggingActive
        => builder.PdfUA
            || builder.Conformance is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;

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

    // ISO 32000-1 14.8.4.3.3: list structure L -> LI -> {Lbl, LBody}.
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
        if (block is Paragraph paragraph && resolution.BodyElementOf(paragraph) is { } body)
        {
            return body;
        }

        return blockElements.TryGetValue(block, out var element) ? element : null;
    }

    public StructureElement? MarkerElementOf(object block)
        => block is Paragraph paragraph ? resolution.LabelElementOf(paragraph) : null;

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
