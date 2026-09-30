using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class InlineContentTests
{
    private static (Document Document, Paragraph Paragraph) Load(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);
        return (document, (Paragraph)document.Children[0]);
    }

    private static string Write(Document document, string markdown) => MarkdownWriter.Write(document, markdown);

    [Theory]
    [InlineData("plain text")]
    [InlineData("a *b* **c** ~~d~~ `e` [f](g \"h\") <https://x.io> ![i](j)")]
    [InlineData("_under_ and __score__ and ~~double~~")]
    [InlineData("hard  \nbreak and\\\nanother")]
    [InlineData("a [r] b\n\n[r]: /u")]
    public void RebuildingUnchangedRunsReproducesTheMarkdown(string markdown)
    {
        var (document, paragraph) = Load(markdown);
        var runs = InlineContent.Flatten(paragraph.Children);

        paragraph.ReplaceInlines(InlineContent.Rebuild(runs));

        Assert.Equal(markdown, Write(document, markdown));
    }

    [Theory]
    [InlineData("a _b_ c", 3, "X", "a _bX_ c")]
    [InlineData("a _b_ c", 2, "X", "a X*b* c")]
    [InlineData("a **b** c", 3, " d", "a **b** d c")]
    [InlineData("a **b** c", 3, "d", "a **bd** c")]
    [InlineData("a `co` b", 4, "d", "a `cod` b")]
    [InlineData("a [t](u) b", 3, "x", "a [tx](u) b")]
    [InlineData("plain", 5, " *", "plain \\*")]
    public void InsertingTextKeepsTheSurroundingMarks(string markdown, int offset, string text, string expected)
    {
        var (document, paragraph) = Load(markdown);
        var runs = InlineContent.Insert(InlineContent.Flatten(paragraph.Children), offset, text);

        paragraph.ReplaceInlines(InlineContent.Rebuild(runs));

        Assert.Equal(expected, Write(document, markdown));
    }

    [Theory]
    [InlineData("a _bcd_ e", 3, 4, "a _bd_ e")]
    [InlineData("a _b_ c", 2, 3, "a  c")]
    [InlineData("a **b** c **d** e", 2, 6, "a **d** e")]
    [InlineData("x  \ny", 1, 2, "xy")]
    public void DeletingARangeTrimsOrRemovesMarks(string markdown, int start, int end, string expected)
    {
        var (document, paragraph) = Load(markdown);
        var runs = InlineContent.Delete(InlineContent.Flatten(paragraph.Children), start, end);

        paragraph.ReplaceInlines(InlineContent.Rebuild(runs));

        Assert.Equal(expected, Write(document, markdown));
    }

    [Theory]
    [InlineData("say hello world", 4, 9, "Strong", "say **hello** world")]
    [InlineData("say **hello** world", 4, 9, "Strong", "say hello world")]
    [InlineData("say **hello** world", 6, 8, "Strong", "say **he**ll**o** world")]
    [InlineData("**di**rect**ly**", 2, 6, "Strong", "**directly**")]
    [InlineData("Edit *this text* **directly**", 10, 17, "Strong", "Edit *this **text*** **directly**")]
    [InlineData("*~~Toggle~~*", 0, 6, "Emphasis", "~~Toggle~~")]
    [InlineData("parax**y**", 5, 6, "Strikethrough", "para&#120;**~~y~~**")]
    [InlineData("The quick **brown** fox", 10, 19, "Strong", "The quick **brown fox**")]
    public void TogglingAMarkOnASelectionAbsorbsPartiallySelectedRuns(string markdown, int start, int end, string kind, string expected)
    {
        var (document, paragraph) = Load(markdown);
        var runs = InlineContent.ToggleMark(InlineContent.Flatten(paragraph.Children), start, end, new Mark(System.Enum.Parse<MarkKind>(kind)));

        paragraph.ReplaceInlines(InlineContent.Rebuild(runs));

        Assert.Equal(expected, Write(document, markdown));
    }

    [Fact]
    public void SplittingRunsSplitsMarksIntoBothHalves()
    {
        var (document, paragraph) = Load("a **Des|ign** b");
        var runs = InlineContent.Flatten(paragraph.Children);
        var head = InlineContent.Split(InlineContent.Delete(runs, 5, 6), 5, out var tail);
        var second = new Paragraph();

        paragraph.ReplaceInlines(InlineContent.Rebuild(head));
        second.ReplaceInlines(InlineContent.Rebuild(tail));
        document.Add(second);

        Assert.Equal("a **Des**\n\n**ign** b", Write(document, "a **Des|ign** b"));
    }
}
