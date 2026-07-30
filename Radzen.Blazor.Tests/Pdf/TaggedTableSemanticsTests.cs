#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedTableSemanticsTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private static Document Invoice()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Invoice";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();

        var header = table.Rows.Add();
        header.IsHeaderRow = true;
        TableLayoutSupport.Fill(header.Cells[0], "Item");
        TableLayoutSupport.Fill(header.Cells[1], "Price");

        var body = table.Rows.Add();
        TableLayoutSupport.Fill(body.Cells[0], "Apple");
        TableLayoutSupport.Fill(body.Cells[1], "111");

        return document;
    }

    [Fact]
    public void CellContent_IsCapturedAsParagraphsBeneathTheCell()
    {
        var root = TaggedStructureProbe.Root(BuildTestSupport.Read(Invoice(), Accessible()));

        foreach (var cell in TaggedStructureProbe.All(root, "TH").Concat(TaggedStructureProbe.All(root, "TD")))
        {
            Assert.Empty(cell.Mcids);
            var paragraph = Assert.Single(cell.Children);
            Assert.Equal("P", paragraph.Type);
            Assert.NotEmpty(paragraph.Mcids);
        }
    }

    [Fact]
    public void CellHeadingStyle_KeepsItsHeadingIntentInsideTheCell()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Heading cell";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var cell = table.Rows.Add().Cells[0];
        TableLayoutSupport.Fill(cell, "Summary");
        ((Paragraph)cell.Blocks[0]).StyleName = "Heading3";

        var root = TaggedStructureProbe.Root(BuildTestSupport.Read(document, Accessible()));
        var td = TaggedStructureProbe.Single(root, "TD");

        Assert.Equal("H3", Assert.Single(td.Children).Type);
    }

    [Fact]
    public void HeaderCell_DerivesItsScopeFromTheHeaderRow()
    {
        var reader = BuildTestSupport.Read(Invoice(), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        foreach (var th in TaggedStructureProbe.All(root, "TH"))
        {
            var attributes = Assert.IsType<DictionaryObject>(reader.Resolve(th.Dict["A"]!));
            Assert.Equal("Table", BuildTestSupport.Name(reader, attributes, "O"));
            Assert.Equal("Column", BuildTestSupport.Name(reader, attributes, "Scope"));
        }

        foreach (var td in TaggedStructureProbe.All(root, "TD"))
        {
            Assert.False(td.Dict.ContainsKey("A"), "a body cell carries no table scope");
        }
    }

    private static Document Matrix(bool headerRow, bool headerColumn)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Matrix";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        table.Columns[0].IsHeaderColumn = headerColumn;

        var first = table.Rows.Add();
        first.IsHeaderRow = headerRow;
        TableLayoutSupport.Fill(first.Cells[0], "Corner");
        TableLayoutSupport.Fill(first.Cells[1], "Price");

        var body = table.Rows.Add();
        TableLayoutSupport.Fill(body.Cells[0], "Apple");
        TableLayoutSupport.Fill(body.Cells[1], "111");

        return document;
    }

    private static DictionaryObject Attributes(DocumentReader reader, ProbeElement element)
        => Assert.IsType<DictionaryObject>(reader.Resolve(element.Dict["A"]!));

    [Fact]
    public void HeaderColumn_GivesItsCellsTheRowScope()
    {
        var reader = BuildTestSupport.Read(Matrix(headerRow: false, headerColumn: true), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        var headers = TaggedStructureProbe.All(root, "TH");

        Assert.Equal(2, headers.Count);
        Assert.All(headers, th => Assert.Equal("Row", BuildTestSupport.Name(reader, Attributes(reader, th), "Scope")));
        Assert.Equal(2, TaggedStructureProbe.All(root, "TD").Count);
    }

    [Fact]
    public void HeaderRowAndHeaderColumn_MeetInACellScopedToBoth()
    {
        var reader = BuildTestSupport.Read(Matrix(headerRow: true, headerColumn: true), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        var scopes = TaggedStructureProbe.All(root, "TH")
            .Select(th => BuildTestSupport.Name(reader, Attributes(reader, th), "Scope"))
            .ToList();

        Assert.Equal(["Both", "Column", "Row"], scopes.OrderBy(scope => scope).ToList());
        Assert.Single(TaggedStructureProbe.All(root, "TD"));
    }

    [Fact]
    public void HeaderRowAlone_ScopesEveryHeaderCellToItsColumn()
    {
        var reader = BuildTestSupport.Read(Matrix(headerRow: true, headerColumn: false), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        var headers = TaggedStructureProbe.All(root, "TH");

        Assert.Equal(2, headers.Count);
        Assert.All(headers, th => Assert.Equal("Column", BuildTestSupport.Name(reader, Attributes(reader, th), "Scope")));
    }

    private static Document Merged(int rowSpan, int columnSpan)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Merged";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();

        var first = table.Rows.Add();
        first.Cells[0].RowSpan = rowSpan;
        first.Cells[0].ColumnSpan = columnSpan;
        TableLayoutSupport.Fill(first.Cells[0], "Merged");
        if (columnSpan == 1)
        {
            TableLayoutSupport.Fill(first.Cells[1], "Right");
        }

        var second = table.Rows.Add();
        if (rowSpan == 1)
        {
            TableLayoutSupport.Fill(second.Cells[0], "Below");
            TableLayoutSupport.Fill(second.Cells[1], "Corner");
        }
        else if (columnSpan == 1)
        {
            TableLayoutSupport.Fill(second.Cells[0], "Below");
        }

        return document;
    }

    [Fact]
    public void SpanningCell_CarriesItsRowAndColumnSpanIntoTheStructureTree()
    {
        var reader = BuildTestSupport.Read(Merged(rowSpan: 2, columnSpan: 2), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        var merged = TaggedStructureProbe.All(root, "TD")[0];
        var attributes = Attributes(reader, merged);

        Assert.Equal("Table", BuildTestSupport.Name(reader, attributes, "O"));
        Assert.Equal(2, BuildTestSupport.Int(attributes, "RowSpan"));
        Assert.Equal(2, BuildTestSupport.Int(attributes, "ColSpan"));
    }

    [Fact]
    public void SingleColumnSpan_IsLeftAtTheImplicitDefault()
    {
        var reader = BuildTestSupport.Read(Merged(rowSpan: 2, columnSpan: 1), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        var attributes = Attributes(reader, TaggedStructureProbe.All(root, "TD")[0]);

        Assert.Equal(2, BuildTestSupport.Int(attributes, "RowSpan"));
        Assert.False(attributes.ContainsKey("ColSpan"), "an unspanned column is left implicit");
    }

    [Fact]
    public void UnspannedCells_CarryNoTableAttributes()
    {
        var reader = BuildTestSupport.Read(Merged(rowSpan: 1, columnSpan: 1), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        Assert.All(
            TaggedStructureProbe.All(root, "TD"),
            td => Assert.False(td.Dict.ContainsKey("A"), "an unspanned body cell carries no table attributes"));
    }

    private static Document Spanning()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Rows";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(140));
        section.Margins.SetAll(Unit.FromPoint(20));

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        header.IsHeaderRow = true;
        TableLayoutSupport.Fill(header.Cells[0], "Column");
        for (var i = 0; i < 12; i++)
        {
            TableLayoutSupport.Fill(table.Rows.Add().Cells[0], $"row {i}");
        }

        return document;
    }

    private static List<(string Tag, int? Mcid)> Marked(DocumentReader reader, int pageIndex)
    {
        var result = new List<(string, int?)>();
        var stack = new List<(string Tag, int? Mcid)>();
        foreach (var operation in ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, pageIndex)))
        {
            switch (operation.Operator)
            {
                case "BDC" or "BMC":
                    {
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

                        stack.Add((operation.Operands.Count > 0 ? operation.Operands[0].Text : "", mcid));
                        break;
                    }

                case "EMC":
                    if (stack.Count > 0)
                    {
                        stack.RemoveAt(stack.Count - 1);
                    }

                    break;

                case "Tj" or "TJ":
                    if (stack.Count > 0)
                    {
                        result.Add(stack[^1]);
                    }

                    break;
            }
        }

        return result;
    }

    [Fact]
    public void RepeatedHeaderRow_TagsTheOriginalPlacementAndArtifactsEveryRepeat()
    {
        var reader = BuildTestSupport.Read(Spanning(), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        var th = TaggedStructureProbe.Single(root, "TH");
        var paragraph = Assert.Single(th.Children);
        Assert.Equal("P", paragraph.Type);
        var mark = Assert.Single(paragraph.Mcids);

        var pages = BuildTestSupport.PageLeaves(reader).Count;
        Assert.True(pages > 1, "the table spans more than one page");

        Assert.Contains(Marked(reader, 0), entry => entry.Mcid == mark);
        for (var page = 1; page < pages; page++)
        {
            var marked = Marked(reader, page);
            Assert.NotEmpty(marked);
            Assert.Contains(marked, entry => entry.Tag == "Artifact");
        }
    }

    [Fact]
    public void RepeatedHeaderRow_ProducesASingleSemanticHeaderCell()
    {
        var root = TaggedStructureProbe.Root(BuildTestSupport.Read(Spanning(), Accessible()));

        Assert.Single(TaggedStructureProbe.All(root, "TH"));
    }
}
