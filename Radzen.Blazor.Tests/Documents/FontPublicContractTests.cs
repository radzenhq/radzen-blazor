#nullable enable
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class FontPublicContractTests
{
    private static Document DocumentWith(Paragraph paragraph)
    {
        var document = new Document();
        document.Sections.Add().Blocks.Add(paragraph);
        return document;
    }

    [Fact]
    public void EveryValueStartsUnset()
    {
        var font = new Font();

        Assert.Null(font.Family);
        Assert.Null(font.Size);
        Assert.Null(font.Bold);
        Assert.Null(font.Italic);
        Assert.Null(font.Underline);
        Assert.Null(font.Strikethrough);
        Assert.Null(font.Color);
    }

    [Fact]
    public void UnsetValuesResolveToHelveticaTenPointAndUnstyled()
    {
        var paragraph = new Paragraph { Text = "contract" };
        var resolved = DocumentWith(paragraph).Resolve(paragraph).Font;

        Assert.Equal("Helvetica", resolved.Family);
        Assert.Equal(Unit.Parse("10pt"), resolved.Size);
        Assert.False(resolved.Bold);
        Assert.False(resolved.Italic);
        Assert.False(resolved.Underline);
        Assert.False(resolved.Strikethrough);
        Assert.Equal(Color.Black, resolved.Color);
    }

    [Fact]
    public void SizeIsAUnitAndTwelveEqualsTwelvePoints()
    {
        var font = new Font { Size = 12 };

        Assert.Equal(Unit.Parse("12pt"), font.Size);

        font.Size = "2.54cm";

        Assert.Equal(Unit.Parse("1in"), font.Size);
    }

    [Fact]
    public void AssignmentsStick()
    {
        var font = new Font
        {
            Family = "Contract Sans",
            Size = "12pt",
            Bold = true,
            Italic = true,
            Underline = true,
            Strikethrough = true,
            Color = Color.Blue,
        };

        Assert.Equal("Contract Sans", font.Family);
        Assert.Equal(Unit.Parse("12pt"), font.Size);
        Assert.True(font.Bold);
        Assert.True(font.Italic);
        Assert.True(font.Underline);
        Assert.True(font.Strikethrough);
        Assert.Equal(Color.Blue, font.Color);
    }

    [Fact]
    public void AssigningNullResetsToInherited()
    {
        var paragraph = new Paragraph { Text = "contract" };
        paragraph.Font.Family = "Contract Sans";
        paragraph.Font.Size = "12pt";
        paragraph.Font.Bold = true;
        paragraph.Font.Italic = true;
        paragraph.Font.Underline = true;
        paragraph.Font.Strikethrough = true;
        paragraph.Font.Color = Color.Blue;

        var document = DocumentWith(paragraph);

        paragraph.Font.Family = null;
        paragraph.Font.Size = null;
        paragraph.Font.Bold = null;
        paragraph.Font.Italic = null;
        paragraph.Font.Underline = null;
        paragraph.Font.Strikethrough = null;
        paragraph.Font.Color = null;

        Assert.Null(paragraph.Font.Family);

        var resolved = document.Resolve(paragraph).Font;

        Assert.Equal("Helvetica", resolved.Family);
        Assert.Equal(Unit.Parse("10pt"), resolved.Size);
        Assert.False(resolved.Bold);
        Assert.Equal(Color.Black, resolved.Color);
    }

    [Fact]
    public void AssignmentMarksTheFontModified()
    {
        var font = new Font();

        Assert.False(font.IsModified);

        font.Bold = true;

        Assert.True(font.IsModified);
    }

    [Fact]
    public void ResettingAValueIsTrackedAsAModification()
    {
        var font = new Font();
        font.Bold = null;

        Assert.True(font.IsModified);
        Assert.Null(font.Bold);
    }

    [Fact]
    public void FontIsSealed() => Assert.True(typeof(Font).IsSealed);

    [Fact]
    public void FontDoesNotExposeAPublicAcceptChangesOperation()
        => Assert.Null(typeof(Font).GetMethod("AcceptChanges"));
}
