#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedTableSemanticsTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private static string[] Elements(string emission, string type)
        => [.. Regex.Matches(emission, $@"\d+ 0 obj\n<< {Regex.Escape(StructureMarker(type))}[^\n]*")
            .Select(match => match.Value)];

    private static string Scope(string headerCell)
        => Shaped("header cell", @"/Scope /(\w+)", headerCell).Groups[1].Value;

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
        var emission = Emit(Invoice(), Accessible());

        foreach (var cell in Elements(emission, "TH").Concat(Elements(emission, "TD")))
        {
            var kids = StructureKids("cell element", cell);
            Assert.Empty(Mcids(kids));

            var child = Assert.Single(ChildElements(kids));
            var paragraph = IndirectObject(emission, child);
            Carries($"cell child {child} 0 R", StructureMarker("P"), paragraph);
            Assert.NotEmpty(Mcids(StructureKids($"paragraph {child} 0 R", paragraph)));
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

        var emission = Emit(document, Accessible());
        var td = Assert.Single(Elements(emission, "TD"));

        var child = Assert.Single(ChildElements(StructureKids("body cell", td)));
        Carries($"body cell child {child} 0 R", StructureMarker("H3"), IndirectObject(emission, child));
    }

    [Fact]
    public void HeaderCell_DerivesItsScopeFromTheHeaderRow()
    {
        var emission = Emit(Invoice(), Accessible());

        foreach (var th in Elements(emission, "TH"))
        {
            Carries("header cell", "/A << /O /Table ", th);
            Carries("header cell", "/Scope /Column ", th);
        }

        foreach (var td in Elements(emission, "TD"))
        {
            Lacks("body cell", "/A <<", td);
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

    [Fact]
    public void HeaderColumn_GivesItsCellsTheRowScope()
    {
        var emission = Emit(Matrix(headerRow: false, headerColumn: true), Accessible());
        var headers = Elements(emission, "TH");

        Assert.Equal(2, headers.Length);
        Assert.All(headers, th => Assert.Equal("Row", Scope(th)));
        Assert.Equal(2, Elements(emission, "TD").Length);
    }

    [Fact]
    public void HeaderRowAndHeaderColumn_MeetInACellScopedToBoth()
    {
        var emission = Emit(Matrix(headerRow: true, headerColumn: true), Accessible());

        var scopes = Elements(emission, "TH").Select(Scope).ToList();

        Assert.Equal(["Both", "Column", "Row"], scopes.OrderBy(scope => scope, StringComparer.Ordinal).ToList());
        Assert.Single(Elements(emission, "TD"));
    }

    [Fact]
    public void HeaderRowAlone_ScopesEveryHeaderCellToItsColumn()
    {
        var emission = Emit(Matrix(headerRow: true, headerColumn: false), Accessible());
        var headers = Elements(emission, "TH");

        Assert.Equal(2, headers.Length);
        Assert.All(headers, th => Assert.Equal("Column", Scope(th)));
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
        var emission = Emit(Merged(rowSpan: 2, columnSpan: 2), Accessible());
        var merged = Assert.Single(Elements(emission, "TD"));

        Carries("merged cell", "/A << /O /Table ", merged);
        Assert.Equal(2, NumberIn(merged, "RowSpan"));
        Assert.Equal(2, NumberIn(merged, "ColSpan"));
    }

    private static Document CombinedSpans()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Combined spans";
        BuildTestSupport.RegisterLatin(document);

        var table = document.Sections.Add().Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        table.Columns.Add();
        table.Columns[2].IsHeaderColumn = true;

        var first = table.Rows.Add();
        first.IsHeaderRow = true;
        first.Cells[0].RowSpan = 99;
        first.Cells[0].ColumnSpan = 2;
        TableLayoutSupport.Fill(first.Cells[0], "Merged");
        TableLayoutSupport.Fill(first.Cells[1], "Top right");

        var second = table.Rows.Add();
        second.Cells[0].RowSpan = 99;
        second.Cells[0].ColumnSpan = 99;
        TableLayoutSupport.Fill(second.Cells[0], "Shifted header");

        table.Rows.Add();
        return document;
    }

    [Fact]
    public void CombinedSpans_SemanticCellsExactlyMatchPhysicalPlacement()
    {
        var laidOut = DocumentLayouter.Layout(CombinedSpans());
        var physical = Assert.Single(Assert.Single(laidOut.Pages).Body.Tables).Layout.Cells;
        var structure = laidOut.Semantics.Structure;
        var logical = structure.Nodes
            .Where(node => node.Intent is SemanticIntent.TableCell or SemanticIntent.TableHeaderCell)
            .ToList();
        var logicalElements = logical
            .Select(node => structure.Nodes.IndexOf(node))
            .ToHashSet();
        var associations = structure.Associations
            .Where(association => logicalElements.Contains(association.Element))
            .ToList();

        Assert.Equal(3, physical.Length);
        Assert.Equal(3, logical.Count);
        Assert.Equal(3, associations.Count);
        Assert.Equal(3, associations.Select(association => association.Source).Distinct().Count());
        Assert.Equal(
            physical.Select(cell => cell.Source).ToHashSet(),
            associations.Select(association => association.Source).ToHashSet());

        Assert.Equal(
            [SemanticHeaderScope.ColumnHeader, SemanticHeaderScope.ColumnAndRowHeader, SemanticHeaderScope.RowHeader],
            logical.Select(node => node.HeaderScope).ToArray());
        Assert.Equal([(3, 2), (1, 1), (2, 1)], logical.Select(node => (node.RowSpan, node.ColumnSpan)).ToArray());
    }

    [Fact]
    public void CombinedSpans_TaggedGridHasExactCountsScopesAndEffectiveSpans()
    {
        var emission = Emit(CombinedSpans(), Accessible());
        var headers = Elements(emission, "TH");

        Assert.Equal(3, headers.Length);
        Assert.Empty(Elements(emission, "TD"));
        Assert.Equal(["Column", "Both", "Row"], headers.Select(Scope).ToArray());

        Assert.Equal(3, NumberIn(headers[0], "RowSpan"));
        Assert.Equal(2, NumberIn(headers[0], "ColSpan"));

        Lacks("unspanned header cell", "/RowSpan", headers[1]);
        Lacks("unspanned header cell", "/ColSpan", headers[1]);

        Assert.Equal(2, NumberIn(headers[2], "RowSpan"));
        Lacks("clamped header cell", "/ColSpan", headers[2]);
    }

    [Fact]
    public void SingleColumnSpan_IsLeftAtTheImplicitDefault()
    {
        var emission = Emit(Merged(rowSpan: 2, columnSpan: 1), Accessible());
        var cell = Elements(emission, "TD")[0];

        Assert.Equal(2, NumberIn(cell, "RowSpan"));
        Lacks("body cell", "/ColSpan", cell);
    }

    [Fact]
    public void UnspannedCells_CarryNoTableAttributes()
    {
        var emission = Emit(Merged(rowSpan: 1, columnSpan: 1), Accessible());

        Assert.All(Elements(emission, "TD"), td => Lacks("body cell", "/A <<", td));
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
        Assert.Single(Elements(Emit(Spanning(), Accessible()), "TH"));
    }
}
