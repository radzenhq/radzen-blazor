#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class EditableDocumentTreeTests
{
    private static List<string?> ParagraphTexts(Section section)
        => section.Blocks.OfType<Paragraph>().Select(paragraph => paragraph.Text).ToList();

    private static List<string?> ParagraphTexts(Document document)
        => document.Sections.SelectMany(ParagraphTexts).ToList();

    private static Document WithParagraphs(params string[] texts)
    {
        var document = new Document();
        var section = document.Sections.Add();
        foreach (var text in texts)
        {
            section.Blocks.Add(new Paragraph(text));
        }

        return document;
    }

    [Fact]
    public void BlockInsert_PlacesTheBlockAtThePosition()
    {
        var document = WithParagraphs("a", "c");
        document.Sections[0].Blocks.Insert(1, new Paragraph { Text = "b" });

        Assert.Equal(["a", "b", "c"], ParagraphTexts(document.Sections[0]));
    }

    [Fact]
    public void BlockInsertAtCount_AppendsAtTheEnd()
    {
        var document = WithParagraphs("a", "b");
        var blocks = document.Sections[0].Blocks;
        blocks.Insert(blocks.Count, new Paragraph { Text = "c" });

        Assert.Equal(["a", "b", "c"], ParagraphTexts(document.Sections[0]));
    }

    [Fact]
    public void BlockInsertOutOfRange_Throws()
    {
        var blocks = WithParagraphs("a").Sections[0].Blocks;

        Assert.Throws<ArgumentOutOfRangeException>(() => blocks.Insert(2, new Paragraph()));
        Assert.Throws<ArgumentOutOfRangeException>(() => blocks.Insert(-1, new Paragraph()));
    }

    [Fact]
    public void BlockRemove_DropsTheBlock()
    {
        var document = WithParagraphs("a", "b", "c");
        var blocks = document.Sections[0].Blocks;

        Assert.True(blocks.Remove(blocks[1]));
        Assert.Equal(["a", "c"], ParagraphTexts(document.Sections[0]));
    }

    [Fact]
    public void BlockRemove_OfAForeignBlock_ReturnsFalse()
    {
        var blocks = WithParagraphs("a").Sections[0].Blocks;

        Assert.False(blocks.Remove(new Paragraph()));
        Assert.Single(blocks);
    }

    [Fact]
    public void BlockRemoveAt_DropsTheBlock()
    {
        var document = WithParagraphs("a", "b", "c");
        document.Sections[0].Blocks.RemoveAt(0);

        Assert.Equal(["b", "c"], ParagraphTexts(document.Sections[0]));
    }

    [Fact]
    public void BlockMove_ReordersTheSection()
    {
        var document = WithParagraphs("a", "b", "c");
        document.Sections[0].Blocks.Move(2, 0);

        Assert.Equal(["c", "a", "b"], ParagraphTexts(document.Sections[0]));
    }

    [Fact]
    public void BlockMove_ToItsOwnIndex_IsANoOp()
    {
        var document = WithParagraphs("a", "b");
        document.Sections[0].Blocks.Move(1, 1);

        Assert.Equal(["a", "b"], ParagraphTexts(document.Sections[0]));
    }

    [Fact]
    public void BlockMove_OutOfRange_Throws()
    {
        var blocks = WithParagraphs("a", "b").Sections[0].Blocks;

        Assert.Throws<ArgumentOutOfRangeException>(() => blocks.Move(0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => blocks.Move(2, 0));
    }

    [Fact]
    public void BlockClear_EmptiesTheSection()
    {
        var document = WithParagraphs("a", "b");
        document.Sections[0].Blocks.Clear();

        Assert.Empty(document.Sections[0].Blocks);
    }

    [Fact]
    public void BlockRemovedThenReAdded_ReordersTheSection()
    {
        var document = WithParagraphs("a", "b", "c");
        var blocks = document.Sections[0].Blocks;
        var middle = blocks[1];

        blocks.Remove(middle);
        Assert.Equal(["a", "c"], ParagraphTexts(document.Sections[0]));

        blocks.Insert(0, middle);
        Assert.Equal(["b", "a", "c"], ParagraphTexts(document.Sections[0]));
    }

    [Fact]
    public void StructuralMutation_IsObservedByChangeTracking()
    {
        var blocks = WithParagraphs("a", "b").Sections[0].Blocks;
        blocks.AcceptStructure();
        Assert.False(blocks.StructureChanged);

        blocks.RemoveAt(0);
        Assert.True(blocks.StructureChanged);

        blocks.AcceptStructure();
        blocks.Move(0, 0);
        Assert.False(blocks.StructureChanged);

        blocks.Add(new Paragraph("c"));
        blocks.AcceptStructure();
        blocks.Move(0, 1);
        Assert.True(blocks.StructureChanged);

        blocks.AcceptStructure();
        blocks.Clear();
        Assert.True(blocks.StructureChanged);
    }

    [Fact]
    public void BlockAddedToASecondCollection_Throws()
    {
        var document = WithParagraphs();
        var other = document.Sections.Add();
        var paragraph = document.Sections[0].Blocks.Add(new Paragraph("a"));

        Assert.Throws<InvalidOperationException>(() => other.Blocks.Add(paragraph));
        Assert.Empty(other.Blocks);
        Assert.Same(paragraph, document.Sections[0].Blocks[0]);
    }

    [Fact]
    public void BlockInsertedIntoASecondCollection_Throws()
    {
        var document = WithParagraphs();
        var other = document.Sections.Add();
        var paragraph = document.Sections[0].Blocks.Add(new Paragraph("a"));

        Assert.Throws<InvalidOperationException>(() => other.Blocks.Insert(0, paragraph));
    }

    [Fact]
    public void BlockRemovedFromItsParent_MayBeAddedElsewhere()
    {
        var document = WithParagraphs();
        var other = document.Sections.Add();
        var first = document.Sections[0];
        var paragraph = first.Blocks.Add(new Paragraph("a"));

        first.Blocks.Remove(paragraph);
        other.Blocks.Add(paragraph);

        Assert.Empty(first.Blocks);
        Assert.Same(paragraph, other.Blocks[0]);
    }

    [Fact]
    public void ClearedBlock_MayBeAddedElsewhere()
    {
        var document = WithParagraphs();
        var other = document.Sections.Add();
        var first = document.Sections[0];
        var paragraph = first.Blocks.Add(new Paragraph("a"));

        first.Blocks.Clear();
        other.Blocks.Add(paragraph);

        Assert.Same(paragraph, other.Blocks[0]);
    }

    [Fact]
    public void ContainerAddedToItself_Throws()
    {
        var container = new Container();

        var error = Assert.Throws<InvalidOperationException>(() => container.Blocks.Add(container));
        Assert.Contains("cyclic", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ContainerAddedToItsOwnDescendant_Throws()
    {
        var outer = new Container();
        var middle = outer.Blocks.Add(new Container());
        var inner = middle.Blocks.Add(new Container());

        Assert.Throws<InvalidOperationException>(() => inner.Blocks.Add(outer));
    }

    [Fact]
    public void TableAddedToItsOwnCell_Throws()
    {
        var table = new Table();
        table.Columns.Add();
        var row = table.Rows.Add();

        Assert.Throws<InvalidOperationException>(() => row.Cells[0].Blocks.Add(table));
    }

    [Fact]
    public void ListAddedToAnItemOfItself_Throws()
    {
        var list = new ListBlock();
        var item = list.Items.Add("a");

        Assert.Throws<InvalidOperationException>(() => item.NestedList = list);
    }

    [Fact]
    public void NestedListAssignedTwice_Throws()
    {
        var list = new ListBlock();
        var first = list.Items.Add("a");
        var second = list.Items.Add("b");
        var nested = first.NestedList = new ListBlock();

        Assert.Throws<InvalidOperationException>(() => second.NestedList = nested);
    }

    [Fact]
    public void NestedListDetached_MayBeReassigned()
    {
        var list = new ListBlock();
        var first = list.Items.Add("a");
        var second = list.Items.Add("b");
        var nested = first.NestedList = new ListBlock();

        first.NestedList = null;
        second.NestedList = nested;

        Assert.Null(first.NestedList);
        Assert.Same(nested, second.NestedList);
    }

    [Fact]
    public void RunAddedToASecondParagraph_Throws()
    {
        var first = new Paragraph();
        var second = new Paragraph();
        var run = first.Inlines.Add("a");

        Assert.Throws<InvalidOperationException>(() => second.Inlines.Add(run));
    }

    [Fact]
    public void InlineInsertRemoveAndMove_ReorderTheParagraphText()
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("a");
        paragraph.Inlines.Add("c");
        paragraph.Inlines.Insert(1, "b");
        Assert.Equal("abc", paragraph.Text);

        paragraph.Inlines.Move(0, 2);
        Assert.Equal("bca", paragraph.Text);

        paragraph.Inlines.RemoveAt(1);
        Assert.Equal("ba", paragraph.Text);

        Assert.True(paragraph.Inlines.Remove(paragraph.Inlines[0]));
        Assert.Equal("a", paragraph.Text);

        paragraph.Inlines.Clear();
        Assert.Null(paragraph.Text);
    }

    [Fact]
    public void InlineMutation_ChangesTheAttachedParagraphText()
    {
        var document = WithParagraphs();
        var paragraph = document.Sections[0].Blocks.Add(new Paragraph("one"));
        paragraph.Inlines.Add("three");
        Assert.Equal("onethree", paragraph.Text);

        paragraph.Inlines.Insert(1, "two");
        Assert.Equal("onetwothree", paragraph.Text);

        paragraph.Inlines.RemoveAt(1);
        Assert.Equal("onethree", paragraph.Text);
    }

    [Fact]
    public void SectionInsertRemoveAndMove_ReorderTheDocument()
    {
        var document = WithParagraphs();
        document.Sections[0].Blocks.Add(new Paragraph("a"));
        document.Sections.Add().Blocks.Add(new Paragraph("c"));
        document.Sections.Insert(1).Blocks.Add(new Paragraph("b"));
        Assert.Equal(["a", "b", "c"], ParagraphTexts(document));

        document.Sections.Move(0, 2);
        Assert.Equal(["b", "c", "a"], ParagraphTexts(document));

        document.Sections.RemoveAt(0);
        Assert.Equal(["c", "a"], ParagraphTexts(document));
    }

    [Fact]
    public void SectionRemovedThenReAdded_KeepsItsContent()
    {
        var document = WithParagraphs();
        document.Sections[0].Blocks.Add(new Paragraph("a"));
        var second = document.Sections.Add();
        second.Blocks.Add(new Paragraph("b"));

        document.Sections.Remove(second);
        Assert.Equal(["a"], ParagraphTexts(document));

        document.Sections.Insert(0, second);
        Assert.Equal(["b", "a"], ParagraphTexts(document));
    }

    [Fact]
    public void SectionAddedToASecondDocument_Throws()
    {
        var document = new Document();
        var section = document.Sections.Add();

        Assert.Throws<InvalidOperationException>(() => new Document().Sections.Add(section));
    }

    [Fact]
    public void SectionClear_EmptiesTheDocument()
    {
        var document = WithParagraphs("a");
        document.Sections.Clear();

        Assert.Empty(document.Sections);
    }

    [Fact]
    public void ListItemInsertRemoveAndMove_ReorderTheItems()
    {
        var list = new ListBlock();
        list.Items.Add("a");
        list.Items.Add("c");
        list.Items.Insert(1, "b");
        Assert.Equal(["a", "b", "c"], list.Items.Select(item => item.Text));

        list.Items.Move(0, 2);
        Assert.Equal(["b", "c", "a"], list.Items.Select(item => item.Text));

        list.Items.RemoveAt(0);
        Assert.Equal(["c", "a"], list.Items.Select(item => item.Text));

        list.Items.Clear();
        Assert.Empty(list.Items);
    }

    [Fact]
    public void ListItemAddedToASecondList_Throws()
    {
        var first = new ListBlock();
        var item = first.Items.Add("a");

        Assert.Throws<InvalidOperationException>(() => new ListBlock().Items.Add(item));
    }

    [Fact]
    public void ListMutation_ReordersTheAttachedItems()
    {
        var document = WithParagraphs();
        var list = document.Sections[0].Blocks.Add(new ListBlock { Style = ListStyle.Number });
        list.AddItem("a");
        list.AddItem("c");
        list.Items.Insert(1, "b");

        Assert.Equal(["a", "b", "c"], list.Items.Select(item => item.Text));

        list.Items.Move(2, 0);
        Assert.Equal(["c", "a", "b"], list.Items.Select(item => item.Text));
    }

    [Fact]
    public void ColumnInsert_AddsACellAtTheSamePositionInEveryRow()
    {
        var table = new Table();
        table.Columns.Add();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Text = "a";
        row.Cells[1].Text = "c";

        table.Columns.Insert(1);
        row.Cells[1].Text = "b";

        Assert.Equal(3, table.Columns.Count);
        Assert.Equal(["a", "b", "c"], row.Cells.Select(cell => cell.Text));
    }

    [Fact]
    public void ColumnRemove_DropsTheMatchingCellOfEveryRow()
    {
        var table = new Table();
        table.Columns.Add();
        table.Columns.Add();
        var first = table.Rows.Add();
        var second = table.Rows.Add();
        first.Cells[0].Text = "a";
        first.Cells[1].Text = "b";
        second.Cells[0].Text = "c";
        second.Cells[1].Text = "d";

        Assert.True(table.Columns.Remove(table.Columns[0]));

        Assert.Single(table.Columns);
        Assert.Equal(["b"], first.Cells.Select(cell => cell.Text));
        Assert.Equal(["d"], second.Cells.Select(cell => cell.Text));
    }

    [Fact]
    public void ColumnMove_MovesTheMatchingCellOfEveryRow()
    {
        var table = new Table();
        table.Columns.Add();
        table.Columns.Add();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Text = "a";
        row.Cells[1].Text = "b";
        row.Cells[2].Text = "c";

        table.Columns.Move(2, 0);

        Assert.Equal(["c", "a", "b"], row.Cells.Select(cell => cell.Text));
    }

    [Fact]
    public void ColumnClear_EmptiesEveryRow()
    {
        var table = new Table();
        table.Columns.Add();
        var row = table.Rows.Add();

        table.Columns.Clear();

        Assert.Empty(table.Columns);
        Assert.Empty(row.Cells);
        Assert.Single(table.Rows);
    }

    [Fact]
    public void RowInsertMoveAndRemove_ReorderTheTable()
    {
        var table = new Table();
        table.Columns.Add();
        table.Rows.Add().Cells[0].Text = "a";
        table.Rows.Add().Cells[0].Text = "c";
        table.Rows.Insert(1).Cells[0].Text = "b";

        Assert.Equal(["a", "b", "c"], table.Rows.Select(row => row.Cells[0].Text));

        table.Rows.Move(0, 2);
        Assert.Equal(["b", "c", "a"], table.Rows.Select(row => row.Cells[0].Text));

        table.Rows.RemoveAt(1);
        Assert.Equal(["b", "a"], table.Rows.Select(row => row.Cells[0].Text));
    }

    [Fact]
    public void RowRemovedThenReAdded_KeepsItsCells()
    {
        var table = new Table();
        table.Columns.Add();
        var first = table.Rows.Add();
        var second = table.Rows.Add();
        first.Cells[0].Text = "a";
        second.Cells[0].Text = "b";

        Assert.True(table.Rows.Remove(second));
        table.Rows.Insert(0, second);

        Assert.Equal(["b", "a"], table.Rows.Select(row => row.Cells[0].Text));
    }

    [Fact]
    public void RowAddedToASecondTable_Throws()
    {
        var table = new Table();
        table.Columns.Add();
        var row = table.Rows.Add();

        var other = new Table();
        other.Columns.Add();

        Assert.Throws<InvalidOperationException>(() => other.Rows.Add(row));
    }

    [Fact]
    public void RowWithAMismatchedCellCount_Throws()
    {
        var table = new Table();
        table.Columns.Add();
        var row = table.Rows.Add();
        table.Rows.Remove(row);

        var other = new Table();
        other.Columns.Add();
        other.Columns.Add();

        var error = Assert.Throws<InvalidOperationException>(() => other.Rows.Add(row));
        Assert.Contains("one cell per column", error.Message, StringComparison.Ordinal);
        Assert.Empty(other.Rows);

        table.Rows.Add(row);
        Assert.Single(table.Rows);
    }

    [Fact]
    public void RowClear_KeepsTheColumns()
    {
        var table = new Table();
        table.Columns.Add();
        table.Rows.Add();

        table.Rows.Clear();

        Assert.Empty(table.Rows);
        Assert.Single(table.Columns);
    }
}
