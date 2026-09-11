using System.Collections.Generic;
using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class StrikethroughTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);
        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData("~~struck~~", @"<document>
    <paragraph>
        <strikethrough>
            <text>struck</text>
        </strikethrough>
    </paragraph>
</document>")]
    [InlineData("a ~~b **c** d~~ e", @"<document>
    <paragraph>
        <text>a </text>
        <strikethrough>
            <text>b </text>
            <strong>
                <text>c</text>
            </strong>
            <text> d</text>
        </strikethrough>
        <text> e</text>
    </paragraph>
</document>")]
    [InlineData("~one~", @"<document>
    <paragraph>
        <text>~</text>
        <text>one</text>
        <text>~</text>
    </paragraph>
</document>")]
    [InlineData("~~a ~~b", @"<document>
    <paragraph>
        <text>~~</text>
        <text>a </text>
        <text>~~</text>
        <text>b</text>
    </paragraph>
</document>")]
    public void Strikethrough_parses(string markdown, string expected)
    {
        Assert.Equal(expected.Replace("\r\n", "\n"), ToXml(markdown).Replace("\r\n", "\n"));
    }

    [Fact]
    public void Visitor_without_VisitStrikethrough_receives_the_children()
    {
        var visitor = new TextOnlyVisitor();

        MarkdownParser.Parse("a ~~b~~ c").Accept(visitor);

        Assert.Equal("a b c", visitor.Text);
    }

    private class TextOnlyVisitor : INodeVisitor
    {
        public string Text { get; private set; } = string.Empty;

        public void VisitText(Text text) => Text += text.Value;
        public void VisitDocument(Document document) => Visit(document.Children);
        public void VisitParagraph(Paragraph paragraph) => Visit(paragraph.Children);
        private void Visit(IEnumerable<INode> nodes)
        {
            foreach (var node in nodes)
            {
                node.Accept(this);
            }
        }

        public void VisitHeading(Heading heading) { }
        public void VisitBlockQuote(BlockQuote blockQuote) { }
        public void VisitUnorderedList(UnorderedList unorderedList) { }
        public void VisitListItem(ListItem listItem) { }
        public void VisitOrderedList(OrderedList orderedList) { }
        public void VisitEmphasis(Emphasis emphasis) { }
        public void VisitStrong(Strong strong) { }
        public void VisitCode(Code code) { }
        public void VisitLink(Link link) { }
        public void VisitImage(Image image) { }
        public void VisitHtmlInline(HtmlInline html) { }
        public void VisitLineBreak(LineBreak lineBreak) { }
        public void VisitSoftLineBreak(SoftLineBreak softLineBreak) { }
        public void VisitThematicBreak(ThematicBreak thematicBreak) { }
        public void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock) { }
        public void VisitFencedCodeBlock(FencedCodeBlock fencedCodeBlock) { }
        public void VisitHtmlBlock(HtmlBlock htmlBlock) { }
        public void VisitTable(Table table) { }
        public void VisitTableHeaderRow(TableHeaderRow header) { }
        public void VisitTableRow(TableRow row) { }
        public void VisitTableCell(TableCell cell) { }
    }
}
