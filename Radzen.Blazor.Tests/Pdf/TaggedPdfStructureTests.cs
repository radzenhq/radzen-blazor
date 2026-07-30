#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedPdfStructureTests
{
    private sealed class Node
    {
        public string RawType = "";
        public DictionaryObject Dict = null!;
        public List<Node> Children { get; } = [];
        public List<int> Mcids { get; } = [];
        public List<object> Kids { get; } = [];
    }

    private static Document AuthorInvoice()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();

        var heading = BuildTestSupport.AddText(section, "Invoice", BuildTestSupport.Latin, 18);
        heading.StyleName = "Heading1";

        BuildTestSupport.AddText(section, "Billed to Acme Corp", BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, "Payment due in 30 days", BuildTestSupport.Latin);

        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();

        var header = table.Rows.Add();
        header.IsHeaderRow = true;
        TableLayoutSupport.Fill(header.Cells[0], "Item");
        TableLayoutSupport.Fill(header.Cells[1], "Price");

        var apple = table.Rows.Add();
        TableLayoutSupport.Fill(apple.Cells[0], "Apple");
        TableLayoutSupport.Fill(apple.Cells[1], "111");

        var bread = table.Rows.Add();
        TableLayoutSupport.Fill(bread.Cells[0], "Bread");
        TableLayoutSupport.Fill(bread.Cells[1], "222");

        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg")).AlternateText = "A red square";

        return document;
    }

    private static DictionaryObject Catalog(DocumentReader reader)
    {
        Assert.True(reader.Trailer.TryGetValue("Root", out var rootObject), "trailer has /Root");
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(rootObject!));
        return catalog;
    }

    private static DictionaryObject StructTreeRoot(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("StructTreeRoot", out var rootObject), "catalog has /StructTreeRoot");
        var root = Assert.IsType<DictionaryObject>(reader.Resolve(rootObject!));
        Assert.Equal("StructTreeRoot", BuildTestSupport.Name(reader, root, "Type"));
        return root;
    }

    private static string Effective(DocumentReader reader, DictionaryObject structRoot, string rawType)
    {
        if (structRoot.TryGetValue("RoleMap", out var mapObject)
            && reader.Resolve(mapObject!) is DictionaryObject roleMap
            && roleMap.TryGetValue(rawType, out var mapped)
            && reader.Resolve(mapped!) is NameObject standard)
        {
            return standard.Value;
        }

        return rawType;
    }

    private static List<DocumentObject> KidValues(DocumentReader reader, DictionaryObject parent)
    {
        var values = new List<DocumentObject>();
        if (!parent.TryGetValue("K", out var k))
        {
            return values;
        }

        var resolved = reader.Resolve(k!);
        if (resolved is ArrayObject array)
        {
            foreach (var item in array)
            {
                values.Add(reader.Resolve(item));
            }
        }
        else
        {
            values.Add(resolved);
        }

        return values;
    }

    private static List<Node> KidsOf(DocumentReader reader, DictionaryObject parent)
    {
        var nodes = new List<Node>();
        foreach (var kid in KidValues(reader, parent))
        {
            if (kid is DictionaryObject dict
                && dict.TryGetValue("S", out var s)
                && reader.Resolve(s!) is NameObject type)
            {
                var element = new Node { RawType = type.Value, Dict = dict };
                element.Children.AddRange(KidsOf(reader, dict));
                element.Mcids.AddRange(DirectMcids(reader, dict));
                element.Kids.AddRange(OrderedKids(reader, dict));
                nodes.Add(element);
            }
        }

        return nodes;
    }

    private static IEnumerable<int> DirectMcids(DocumentReader reader, DictionaryObject element)
    {
        foreach (var kid in KidValues(reader, element))
        {
            if (kid is NumberObject mcid)
            {
                yield return mcid.IntValue;
            }
            else if (kid is DictionaryObject mcr
                && !mcr.ContainsKey("S")
                && mcr.TryGetValue("MCID", out var value)
                && reader.Resolve(value!) is NumberObject number)
            {
                yield return number.IntValue;
            }
        }
    }

    private static Node DocumentElement(DocumentReader reader, DictionaryObject structRoot)
    {
        var kids = KidsOf(reader, structRoot);
        var document = Assert.Single(kids);
        Assert.Equal("Document", Effective(reader, structRoot, document.RawType));
        return document;
    }

    private static Node SectionElement(DocumentReader reader, DictionaryObject structRoot)
    {
        var document = DocumentElement(reader, structRoot);
        var sect = Assert.Single(document.Children);
        Assert.Equal("Sect", Effective(reader, structRoot, sect.RawType));
        return sect;
    }

    private static List<(string Tag, int Mcid)> MarkedContentInOrder(DocumentReader reader, DictionaryObject page)
    {
        var result = new List<(string, int)>();
        foreach (var operation in ContentStreamTokenizer.Parse(BuildTestSupport.Content(reader, page)))
        {
            if (operation.Operator != "BDC")
            {
                continue;
            }

            Assert.True(operation.Operands.Count >= 2, "BDC has tag and properties operands");
            Assert.Equal(ContentTokenKind.Name, operation.Operands[0].Kind);
            var tag = operation.Operands[0].Text;

            int? mcid = null;
            for (var i = 1; i < operation.Operands.Count - 1; i++)
            {
                if (operation.Operands[i].Kind == ContentTokenKind.Name
                    && operation.Operands[i].Text == "MCID"
                    && operation.Operands[i + 1].Kind == ContentTokenKind.Number)
                {
                    mcid = (int)operation.Operands[i + 1].Number;
                    break;
                }
            }

            if (mcid is { } value)
            {
                result.Add((tag, value));
            }
        }

        return result;
    }

    private static IEnumerable<object> OrderedKids(DocumentReader reader, DictionaryObject element)
    {
        var children = 0;
        var built = KidsOf(reader, element);
        foreach (var kid in KidValues(reader, element))
        {
            if (kid is NumberObject mcid)
            {
                yield return mcid.IntValue;
            }
            else if (kid is DictionaryObject dict && dict.ContainsKey("S"))
            {
                yield return built[children++];
            }
            else if (kid is DictionaryObject mcr
                && mcr.TryGetValue("MCID", out var value)
                && reader.Resolve(value!) is NumberObject number)
            {
                yield return number.IntValue;
            }
        }
    }

    private static void CollectMcidsInStructureOrder(Node node, List<int> acc)
    {
        foreach (var kid in node.Kids)
        {
            if (kid is int mcid)
            {
                acc.Add(mcid);
            }
            else if (kid is Node child)
            {
                CollectMcidsInStructureOrder(child, acc);
            }
        }
    }

    private static Node? OwnerOfMcid(Node node, int mcid)
    {
        if (node.Mcids.Contains(mcid))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            if (OwnerOfMcid(child, mcid) is { } owner)
            {
                return owner;
            }
        }

        return null;
    }

    [Fact]
    public void Build_EmitsMarkInfoAndStructTreeRoot()
    {
        var reader = BuildTestSupport.Read(AuthorInvoice());
        var catalog = Catalog(reader);

        Assert.True(catalog.TryGetValue("MarkInfo", out var markInfoObject), "catalog has /MarkInfo");
        var markInfo = Assert.IsType<DictionaryObject>(reader.Resolve(markInfoObject!));
        Assert.True(markInfo.TryGetValue("Marked", out var marked), "MarkInfo has /Marked");
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(marked!)).Value, "/Marked is true");

        var structRoot = StructTreeRoot(reader);
        Assert.True(structRoot.ContainsKey("K"), "StructTreeRoot has kids");
        Assert.True(structRoot.ContainsKey("ParentTree"), "StructTreeRoot has /ParentTree");
    }

    [Fact]
    public void Build_StructureNestingMatchesAuthoringOrder()
    {
        var reader = BuildTestSupport.Read(AuthorInvoice());
        var structRoot = StructTreeRoot(reader);
        var sect = SectionElement(reader, structRoot);

        var kinds = sect.Children.Select(c => Effective(reader, structRoot, c.RawType)).ToList();
        Assert.Equal(new[] { "H1", "P", "P", "Table", "Figure" }, kinds);
    }

    [Fact]
    public void Build_TableRowsUseThForHeaderAndTdForBody()
    {
        var reader = BuildTestSupport.Read(AuthorInvoice());
        var structRoot = StructTreeRoot(reader);
        var sect = SectionElement(reader, structRoot);

        var table = sect.Children.Single(c => Effective(reader, structRoot, c.RawType) == "Table");
        Assert.Equal(3, table.Children.Count);
        Assert.All(table.Children, row => Assert.Equal("TR", Effective(reader, structRoot, row.RawType)));

        var headerCells = table.Children[0].Children;
        Assert.Equal(2, headerCells.Count);
        Assert.All(headerCells, cell => Assert.Equal("TH", Effective(reader, structRoot, cell.RawType)));

        foreach (var body in table.Children.Skip(1))
        {
            Assert.Equal(2, body.Children.Count);
            Assert.All(body.Children, cell => Assert.Equal("TD", Effective(reader, structRoot, cell.RawType)));
        }

        Assert.All(
            table.Children.SelectMany(row => row.Children),
            cell => Assert.Empty(cell.Children));
    }

    [Fact]
    public void Build_TaggedCellsNestTheirParagraphsInsideTheCell()
    {
        var reader = BuildTestSupport.Read(
            AuthorInvoice(),
            new DocumentRenderer { Conformance = PdfAConformance.PdfA2A });
        var structRoot = StructTreeRoot(reader);
        var sect = SectionElement(reader, structRoot);

        var table = sect.Children.Single(c => Effective(reader, structRoot, c.RawType) == "Table");
        Assert.All(table.Children.SelectMany(row => row.Children), cell =>
        {
            Assert.Empty(cell.Mcids);
            var paragraph = Assert.Single(cell.Children);
            Assert.Equal("P", Effective(reader, structRoot, paragraph.RawType));
            Assert.NotEmpty(paragraph.Mcids);
        });
    }

    [Fact]
    public void Build_Heading1StyleMapsToH1WithPageAndMcid()
    {
        var reader = BuildTestSupport.Read(AuthorInvoice());
        var structRoot = StructTreeRoot(reader);
        var sect = SectionElement(reader, structRoot);

        var heading = sect.Children[0];
        Assert.Equal("H1", Effective(reader, structRoot, heading.RawType));
        Assert.True(heading.Dict.ContainsKey("Pg"), "heading element carries /Pg");
        Assert.NotEmpty(heading.Mcids);
    }

    [Fact]
    public void Build_ContentMcidsMatchStructureTreeInReadingOrder()
    {
        var reader = BuildTestSupport.Read(AuthorInvoice());
        var structRoot = StructTreeRoot(reader);
        var document = DocumentElement(reader, structRoot);

        var pages = BuildTestSupport.PageLeaves(reader);
        var page = Assert.Single(pages).Page;

        var marked = MarkedContentInOrder(reader, page);
        Assert.NotEmpty(marked);

        var treeOrder = new List<int>();
        CollectMcidsInStructureOrder(document, treeOrder);

        Assert.Equal(treeOrder, marked.Select(m => m.Mcid).ToList());
    }

    [Fact]
    public void Build_ParentTreeResolvesPageMcidsToOwningElements()
    {
        var reader = BuildTestSupport.Read(AuthorInvoice());
        var structRoot = StructTreeRoot(reader);
        var document = DocumentElement(reader, structRoot);

        var page = Assert.Single(BuildTestSupport.PageLeaves(reader)).Page;
        Assert.True(page.TryGetValue("StructParents", out var keyObject), "page has /StructParents");
        var key = Assert.IsType<NumberObject>(reader.Resolve(keyObject!)).IntValue;

        var parentTree = Assert.IsType<DictionaryObject>(reader.Resolve(structRoot["ParentTree"]));
        var entry = NumberTreeLookup(reader, parentTree, key);
        var parents = Assert.IsType<ArrayObject>(entry);

        foreach (var (_, mcid) in MarkedContentInOrder(reader, page))
        {
            Assert.True(mcid < parents.Count, $"ParentTree entry covers MCID {mcid}");
            var resolved = Assert.IsType<DictionaryObject>(reader.Resolve(parents[mcid]));
            var owner = OwnerOfMcid(document, mcid);
            Assert.NotNull(owner);
            Assert.Same(owner!.Dict, resolved);
        }
    }

    private static DocumentObject? NumberTreeLookup(DocumentReader reader, DictionaryObject node, int key)
    {
        if (node.TryGetValue("Nums", out var numsObject)
            && reader.Resolve(numsObject!) is ArrayObject nums)
        {
            for (var i = 0; i + 1 < nums.Count; i += 2)
            {
                if (reader.Resolve(nums[i]) is NumberObject number && number.IntValue == key)
                {
                    return reader.Resolve(nums[i + 1]);
                }
            }
        }

        if (node.TryGetValue("Kids", out var kidsObject)
            && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject child
                    && NumberTreeLookup(reader, child, key) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }
}
