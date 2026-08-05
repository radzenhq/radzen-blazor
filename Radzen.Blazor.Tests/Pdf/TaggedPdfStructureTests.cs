#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

        var table = section.Blocks.Add(new Table());
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

        section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg"))).AlternateText = "A red square";

        return document;
    }

    private static DocumentRenderer TaggedRenderer() => new() { Conformance = PdfAConformance.PdfA2A };

    private static DocumentReader ReadTagged(Document document)
        => BuildTestSupport.Read(document, TaggedRenderer());

    private static string RawType(string element)
        => Shaped("structure element", @"/S /(\w+) /P ", element).Groups[1].Value;

    private static string EffectiveType(string structureRoot, string rawType)
    {
        var roleMap = Regex.Match(structureRoot, @"/RoleMap << ([^>]*)>>");
        if (roleMap.Success)
        {
            var mapped = Regex.Match(roleMap.Groups[1].Value, $@"/{Regex.Escape(rawType)} /(\w+)");
            if (mapped.Success)
            {
                return mapped.Groups[1].Value;
            }
        }

        return rawType;
    }

    private static string[] Pages(string emission)
        => [.. Regex.Matches(emission, @"\d+ 0 obj\n<< /Type /Page [^\n]*").Select(match => match.Value)];

    private static (string Root, string Sect) SectionElement(string emission)
    {
        var structureRoot = StructureRoot(emission);
        var documentElement = IndirectObject(
            emission,
            Shaped("structure root", @"/K (\d+) 0 R", structureRoot).Groups[1].Value);
        Assert.Equal("Document", EffectiveType(structureRoot, RawType(documentElement)));

        var sect = IndirectObject(
            emission,
            Assert.Single(ChildElements(StructureKids("document element", documentElement))));
        Assert.Equal("Sect", EffectiveType(structureRoot, RawType(sect)));

        return (structureRoot, sect);
    }

    private static string TableElement(string emission, string structureRoot, string sect)
        => ChildElements(StructureKids("section element", sect))
            .Select(number => IndirectObject(emission, number))
            .Single(element => EffectiveType(structureRoot, RawType(element)) == "Table");

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
                element.Mcids.AddRange(ReaderMcids(reader, dict));
                element.Kids.AddRange(OrderedKids(reader, dict));
                nodes.Add(element);
            }
        }

        return nodes;
    }

    private static IEnumerable<int> ReaderMcids(DocumentReader reader, DictionaryObject element)
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
        var emission = Emit(AuthorInvoice(), TaggedRenderer());

        Carries("catalog", "/MarkInfo << /Marked true >>", Line(emission, "/Type /Catalog"));

        var structureRoot = StructureRoot(emission);
        Carries("structure root", "/Type /StructTreeRoot", structureRoot);
        Carries("structure root", "/K ", structureRoot);
        Carries("structure root", "/ParentTree ", structureRoot);
    }

    [Fact]
    public void Build_WithoutAccessibilityOrConformance_EmitsNoMarkInfoAndNoStructTreeRoot()
    {
        var catalog = Line(Emit(AuthorInvoice(), new DocumentRenderer()), "/Type /Catalog");

        Lacks("catalog", "/MarkInfo", catalog);
        Lacks("catalog", "/StructTreeRoot", catalog);
    }

    [Fact]
    public void Build_WithoutAccessibilityOrConformance_EmitsNoStructParentsOnAnyPage()
    {
        var emission = Emit(AuthorInvoice(), new DocumentRenderer());

        Assert.All(Pages(emission), page =>
        {
            Lacks("page", "/StructParents", page);
            Lacks("page", "/StructParent", page);
        });
    }

    [Fact]
    public void Build_WithoutAccessibilityOrConformance_EmitsNoMarkedContentInThePageStream()
    {
        var reader = BuildTestSupport.Read(AuthorInvoice());

        foreach (var leaf in BuildTestSupport.PageLeaves(reader))
        {
            var operators = ContentStreamTokenizer
                .Parse(BuildTestSupport.Content(reader, leaf.Page))
                .Select(operation => operation.Operator)
                .ToList();

            Assert.DoesNotContain("BDC", operators);
            Assert.DoesNotContain("BMC", operators);
            Assert.DoesNotContain("EMC", operators);
        }
    }

    [Fact]
    public void Build_WithAccessibility_EmitsMarkedContentThePlainRenderDoesNot()
    {
        var tagged = ReadTagged(AuthorInvoice());

        var page = Assert.Single(BuildTestSupport.PageLeaves(tagged)).Page;

        Assert.NotEmpty(MarkedContentInOrder(tagged, page));
    }

    [Fact]
    public void Build_StructureNestingMatchesAuthoringOrder()
    {
        var emission = Emit(AuthorInvoice(), TaggedRenderer());
        var (structureRoot, sect) = SectionElement(emission);

        var kinds = ChildElements(StructureKids("section element", sect))
            .Select(number => EffectiveType(structureRoot, RawType(IndirectObject(emission, number))))
            .ToArray();

        Assert.Equal(new[] { "H1", "P", "P", "Table", "Figure" }, kinds);
    }

    [Fact]
    public void Build_TableRowsUseThForHeaderAndTdForBody()
    {
        var emission = Emit(AuthorInvoice(), TaggedRenderer());
        var (structureRoot, sect) = SectionElement(emission);

        var rows = ChildElements(StructureKids("table element", TableElement(emission, structureRoot, sect)));
        Assert.Equal(3, rows.Length);
        Assert.All(rows, row => Assert.Equal("TR", EffectiveType(structureRoot, RawType(IndirectObject(emission, row)))));

        var headerCells = ChildElements(StructureKids("header row", IndirectObject(emission, rows[0])));
        Assert.Equal(2, headerCells.Length);
        Assert.All(
            headerCells,
            cell => Assert.Equal("TH", EffectiveType(structureRoot, RawType(IndirectObject(emission, cell)))));

        foreach (var row in rows.Skip(1))
        {
            var cells = ChildElements(StructureKids("body row", IndirectObject(emission, row)));
            Assert.Equal(2, cells.Length);
            Assert.All(
                cells,
                cell => Assert.Equal("TD", EffectiveType(structureRoot, RawType(IndirectObject(emission, cell)))));
        }
    }

    [Fact]
    public void Build_TaggedCellsNestTheirParagraphsInsideTheCell()
    {
        var emission = Emit(AuthorInvoice(), TaggedRenderer());
        var (structureRoot, sect) = SectionElement(emission);

        var rows = ChildElements(StructureKids("table element", TableElement(emission, structureRoot, sect)));

        foreach (var cell in rows.SelectMany(row => ChildElements(StructureKids("row element", IndirectObject(emission, row)))))
        {
            var kids = StructureKids("cell element", IndirectObject(emission, cell));
            Assert.Empty(Mcids(kids));

            var paragraph = IndirectObject(emission, Assert.Single(ChildElements(kids)));
            Assert.Equal("P", EffectiveType(structureRoot, RawType(paragraph)));
            Assert.NotEmpty(Mcids(StructureKids("paragraph element", paragraph)));
        }
    }

    [Fact]
    public void Build_Heading1StyleMapsToH1WithPageAndMcid()
    {
        var emission = Emit(AuthorInvoice(), TaggedRenderer());
        var (structureRoot, sect) = SectionElement(emission);

        var heading = IndirectObject(emission, ChildElements(StructureKids("section element", sect))[0]);

        Assert.Equal("H1", EffectiveType(structureRoot, RawType(heading)));
        Carries("heading element", "/Pg ", heading);
        Assert.NotEmpty(Mcids(StructureKids("heading element", heading)));
    }

    [Fact]
    public void Build_StructureTreeReferencesExactlyThePageMarks()
    {
        var reader = ReadTagged(AuthorInvoice());
        var structRoot = StructTreeRoot(reader);
        var document = DocumentElement(reader, structRoot);

        var pages = BuildTestSupport.PageLeaves(reader);
        var page = Assert.Single(pages).Page;

        var marked = MarkedContentInOrder(reader, page);
        Assert.NotEmpty(marked);

        var treeOrder = new List<int>();
        CollectMcidsInStructureOrder(document, treeOrder);

        var streamOrder = marked.Select(m => m.Mcid).ToList();
        Assert.Equal(streamOrder.Order().ToList(), treeOrder.Order().ToList());
    }

    [Fact]
    public void Build_ParentTreeResolvesPageMcidsToOwningElements()
    {
        var reader = ReadTagged(AuthorInvoice());
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
