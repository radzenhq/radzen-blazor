#nullable enable
using System;
using System.Linq;
using Radzen.Blazor.Pdf.Tests;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

public class PublicSurfaceWartTests
{
    private static Table Table()
    {
        var table = new Document().Sections.Add().Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        table.Rows.Add();
        table.Rows.Add();
        return table;
    }

    [Fact]
    public void RemovedColumnCanBeAddedBack()
    {
        var table = Table();
        var column = table.Columns[0];

        Assert.True(table.Columns.Remove(column));
        Assert.Equal(1, table.Rows[0].Cells.Count);

        Assert.Same(column, table.Columns.Add(column));
        Assert.Equal(2, table.Columns.Count);
        Assert.Equal(2, table.Rows[0].Cells.Count);
        Assert.Same(column, table.Columns[1]);
    }

    [Fact]
    public void RemovedColumnCanBeInsertedBack()
    {
        var table = Table();
        var column = table.Columns[1];
        table.Columns.Remove(column);

        table.Columns.Insert(0, column);

        Assert.Same(column, table.Columns[0]);
        Assert.Equal(2, table.Rows[1].Cells.Count);
    }

    [Fact]
    public void AddingAColumnThatAlreadyHasATableThrows()
    {
        var table = Table();

        Assert.Throws<InvalidOperationException>(() => table.Columns.Add(table.Columns[0]));
        Assert.Throws<InvalidOperationException>(() => Table().Columns.Add(table.Columns[0]));
    }

    [Fact]
    public void TabStopCollectionAddsByPositionThroughAnAddOverload()
    {
        var paragraph = new Paragraph();

        var stop = paragraph.TabStops.Add(Unit.FromPoint(120), TabAlignment.Right, '.');

        Assert.Same(stop, Assert.Single(paragraph.TabStops));
        Assert.Equal(Unit.FromPoint(120), stop.Position);
        Assert.Equal(TabAlignment.Right, stop.Alignment);
        Assert.Equal('.', stop.Leader);
    }

    [Fact]
    public void GradientAcceptsASingleStop()
    {
        var brush = new LinearGradient(0, 0, 10, 0, new GradientStop(0, Color.Red));

        Assert.Single(brush.Stops);
    }

    [Fact]
    public void HeadingLevelIsAbsentAsNullOnBothTheStyleAndTheResolvedFormat()
    {
        var document = new Document();
        var paragraph = document.Sections.Add().Blocks.AddParagraph("Body");

        Assert.Null(document.Styles["Normal"].HeadingLevel);
        Assert.Null(document.Resolve(paragraph).HeadingLevel);

        paragraph.StyleName = "Heading2";

        Assert.Equal(2, document.Resolve(paragraph).HeadingLevel);
    }

    [Fact]
    public void ImageFitBoxIsReadableAndClearable()
    {
        var image = new Document().Sections.Add().Blocks.AddImage(
            PdfTestResources.Open("Images/rgb.jpg"));

        Assert.Null(image.FitBox);

        image.FitBox = (Unit.FromPoint(100), Unit.FromPoint(80));

        Assert.Equal((Unit.FromPoint(100), Unit.FromPoint(80)), image.FitBox);

        image.FitBox = null;

        Assert.Null(image.FitBox);
    }

    [Fact]
    public void TheOnlyInlineKindsAreTextRunsPageFieldsImagesAndFormInputs()
    {
        var kinds = typeof(Inline).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Inline)) && !type.IsAbstract)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "CheckBox", "DropDown", "InlineImage", "PageCountField", "PageNumberField",
                "RadioButton", "Run", "TextInput",
            ],
            kinds);
    }

    [Fact]
    public void TextOnlyMembersDoNotReachNonTextInlines()
    {
        foreach (var member in new[]
        {
            "Text", "Font", "LetterSpacing", "WordSpacing", "VerticalAlignment",
            "VerticalAlignmentScale", "HorizontalScale", "Invisible",
        })
        {
            Assert.Null(typeof(Inline).GetProperty(member));
        }

        Assert.NotNull(typeof(TextInline).GetProperty("Font"));
        Assert.NotNull(typeof(Run).GetProperty("Text"));
    }
}
