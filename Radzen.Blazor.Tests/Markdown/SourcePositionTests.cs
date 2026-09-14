using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class SourcePositionTests
{
    public static IEnumerable<object[]> SpecExamples()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("commonmark-spec.json")!;
        using var document = JsonDocument.Parse(stream);

        foreach (var example in document.RootElement.EnumerateArray())
        {
            yield return [example.GetProperty("example").GetInt32(), example.GetProperty("markdown").GetString()!];
        }
    }

    private sealed class Walker : NodeVisitorBase
    {
        public readonly List<(Block Block, Inline Inline)> Inlines = [];
        public readonly List<Block> Blocks = [];
        private Block? current;

        public override void VisitDocument(Document document)
        {
            Visit(document, () => base.VisitDocument(document));
        }

        private void Visit(Block block, Action visitChildren)
        {
            Blocks.Add(block);
            var previous = current;
            current = block;
            visitChildren();
            current = previous;
        }

        public override void VisitParagraph(Paragraph paragraph) => Visit(paragraph, () => base.VisitParagraph(paragraph));
        public override void VisitHeading(Heading heading) => Visit(heading, () => base.VisitHeading(heading));
        public override void VisitBlockQuote(BlockQuote blockQuote) => Visit(blockQuote, () => base.VisitBlockQuote(blockQuote));
        public override void VisitListItem(ListItem listItem) => Visit(listItem, () => base.VisitListItem(listItem));
        public override void VisitUnorderedList(UnorderedList list) => Visit(list, () => base.VisitUnorderedList(list));
        public override void VisitOrderedList(OrderedList list) => Visit(list, () => base.VisitOrderedList(list));
        public override void VisitFencedCodeBlock(FencedCodeBlock block) => Visit(block, () => base.VisitFencedCodeBlock(block));
        public override void VisitIndentedCodeBlock(IndentedCodeBlock block) => Visit(block, () => base.VisitIndentedCodeBlock(block));
        public override void VisitHtmlBlock(HtmlBlock block) => Visit(block, () => base.VisitHtmlBlock(block));
        public override void VisitThematicBreak(ThematicBreak block) => Visit(block, () => base.VisitThematicBreak(block));
        public override void VisitTable(Table table) => Visit(table, () => base.VisitTable(table));

        private void Inline(Inline inline, Action visitChildren)
        {
            Inlines.Add((current!, inline));
            visitChildren();
        }

        public override void VisitText(Text text) => Inline(text, () => { });
        public override void VisitCode(Code code) => Inline(code, () => { });
        public override void VisitHtmlInline(HtmlInline html) => Inline(html, () => { });
        public override void VisitLineBreak(LineBreak lineBreak) => Inline(lineBreak, () => { });
        public override void VisitSoftLineBreak(SoftLineBreak softLineBreak) => Inline(softLineBreak, () => { });
        public override void VisitEmphasis(Emphasis emphasis) => Inline(emphasis, () => base.VisitEmphasis(emphasis));
        public override void VisitStrong(Strong strong) => Inline(strong, () => base.VisitStrong(strong));
        public override void VisitStrikethrough(Strikethrough strikethrough) => Inline(strikethrough, () => base.VisitStrikethrough(strikethrough));
        public override void VisitLink(Link link) => Inline(link, () => base.VisitLink(link));
        public override void VisitImage(Image image) => Inline(image, () => base.VisitImage(image));
    }

    private static Walker Walk(string markdown)
    {
        var walker = new Walker();
        MarkdownParser.Parse(markdown).Accept(walker);
        return walker;
    }

    [Theory]
    [MemberData(nameof(SpecExamples))]
    public void EveryNodeHasAValidRangeInsideItsParent(int example, string markdown)
    {
        var walker = Walk(markdown);

        foreach (var block in walker.Blocks)
        {
            Assert.True(0 <= block.SourceStart && block.SourceStart <= block.SourceEnd && block.SourceEnd <= markdown.Length,
                $"example {example}: {block.GetType().Name} [{block.SourceStart},{block.SourceEnd}) outside [0,{markdown.Length}) in {Show(markdown)}");
        }

        foreach (var (block, inline) in walker.Inlines)
        {
            Assert.True(block.SourceStart <= inline.SourceStart && inline.SourceStart <= inline.SourceEnd && inline.SourceEnd <= block.SourceEnd,
                $"example {example}: {inline.GetType().Name} [{inline.SourceStart},{inline.SourceEnd}) outside {block.GetType().Name} [{block.SourceStart},{block.SourceEnd}) in {Show(markdown)}");

            if (inline is InlineContainer container)
            {
                var previousEnd = inline.SourceStart;

                foreach (var child in container.Children)
                {
                    Assert.True(previousEnd <= child.SourceStart && child.SourceEnd <= inline.SourceEnd,
                        $"example {example}: child {child.GetType().Name} [{child.SourceStart},{child.SourceEnd}) outside or unordered in {inline.GetType().Name} [{inline.SourceStart},{inline.SourceEnd}) in {Show(markdown)}");
                    previousEnd = child.SourceEnd;
                }
            }
        }

        foreach (var group in walker.Inlines.GroupBy(pair => pair.Block))
        {
            var previousEnd = group.Key.SourceStart;

            foreach (var (_, inline) in group)
            {
                if (group.Key is IBlockInlineContainer top && top.Children.Contains(inline))
                {
                    Assert.True(previousEnd <= inline.SourceStart,
                        $"example {example}: {inline.GetType().Name} [{inline.SourceStart},{inline.SourceEnd}) starts before the previous sibling ended at {previousEnd} in {Show(markdown)}");
                    previousEnd = inline.SourceEnd;
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(SpecExamples))]
    public void PlainTextMatchesItsSource(int example, string markdown)
    {
        foreach (var (_, inline) in Walk(markdown).Inlines)
        {
            var source = markdown[inline.SourceStart..inline.SourceEnd];

            switch (inline)
            {
                case Text text when text.Value.Length > 0 && source.IndexOfAny(['\\', ':', '\n', '\r', '\t', '&']) < 0:
                    Assert.True(text.Value == source, $"example {example}: text \"{text.Value}\" has source \"{source}\" in {Show(markdown)}");
                    break;
                case Code:
                    Assert.True(source.StartsWith('`') && source.EndsWith('`'), $"example {example}: code source \"{source}\" in {Show(markdown)}");
                    break;
                case Emphasis or Strong:
                    Assert.True(source.Length >= 2 && source[0] is '*' or '_' && source[^1] is '*' or '_', $"example {example}: emphasis source \"{source}\" in {Show(markdown)}");
                    break;
                case Strikethrough:
                    Assert.True(source.StartsWith("~~") && source.EndsWith("~~"), $"example {example}: strikethrough source \"{source}\" in {Show(markdown)}");
                    break;
                case Image:
                    Assert.True(source.StartsWith("![") && source.EndsWith(')'), $"example {example}: image source \"{source}\" in {Show(markdown)}");
                    break;
                case Link:
                    Assert.True(source.StartsWith('[') && source.EndsWith(')') || source.StartsWith('<') && source.EndsWith('>') || source.StartsWith('[') && source.EndsWith(']'), $"example {example}: link source \"{source}\" in {Show(markdown)}");
                    break;
            }
        }
    }

    private static string Show(string markdown) => '"' + markdown.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + '"';

    private static string Dump(string markdown)
    {
        var walker = Walk(markdown);
        return string.Join("\n", walker.Inlines.Select(pair => $"{pair.Inline.GetType().Name} [{pair.Inline.SourceStart},{pair.Inline.SourceEnd}) {Show(markdown[pair.Inline.SourceStart..pair.Inline.SourceEnd])}"));
    }

    [Theory]
    [InlineData("**foo** bar", "Strong [0,7) \"**foo**\"\nText [2,5) \"foo\"\nText [7,11) \" bar\"")]
    [InlineData("a \\* b", "Text [0,2) \"a \"\nText [2,4) \"\\\\*\"\nText [4,6) \" b\"")]
    [InlineData("> quoted *em*\n> more", "Text [2,9) \"quoted \"\nEmphasis [9,13) \"*em*\"\nText [10,12) \"em\"\nSoftLineBreak [13,14) \"\\n\"\nText [16,20) \"more\"")]
    [InlineData("- [x] done", "Text [6,10) \"done\"")]
    [InlineData("a\r\nb", "Text [0,1) \"a\"\nSoftLineBreak [1,3) \"\\r\\n\"\nText [3,4) \"b\"")]
    [InlineData("# Head", "Text [2,6) \"Head\"")]
    [InlineData("[t](u)", "Link [0,6) \"[t](u)\"\nText [1,2) \"t\"")]
    [InlineData("`co de`", "Code [0,7) \"`co de`\"")]
    [InlineData("| a | b |\n| - | - |\n| c | d |", "Text [2,3) \"a\"\nText [6,7) \"b\"\nText [22,23) \"c\"\nText [26,27) \"d\"")]
    [InlineData("x  \ny", "Text [0,1) \"x\"\nLineBreak [1,4) \"  \\n\"\nText [4,5) \"y\"")]
    [InlineData("1. one\n2. *two*", "Text [3,6) \"one\"\nEmphasis [10,15) \"*two*\"\nText [11,14) \"two\"")]
    public void InlinePositionsPointAtTheirSource(string markdown, string expected)
    {
        Assert.Equal(expected, Dump(markdown));
    }

    [Theory]
    [InlineData("```js\nx = 1\n```", typeof(FencedCodeBlock), 0, 15, 6)]
    [InlineData("    code\n", typeof(IndentedCodeBlock), 4, 8, 4)]
    [InlineData("para\n\n# h", typeof(AtxHeading), 6, 9, 8)]
    [InlineData("Title\n=====", typeof(SetExtHeading), 0, 11, 0)]
    public void BlockPositionsPointAtTheirSource(string markdown, Type type, int start, int end, int contentStart)
    {
        var block = Walk(markdown).Blocks.Single(block => block.GetType() == type);

        Assert.Equal((start, end), (block.SourceStart, block.SourceEnd));
        Assert.Equal(contentStart, ((Leaf)block).Content.ToSource(0));
    }
}
