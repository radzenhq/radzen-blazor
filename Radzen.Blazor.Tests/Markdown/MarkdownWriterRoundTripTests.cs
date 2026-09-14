using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class MarkdownWriterRoundTripTests
{
    [Theory]
    [InlineData("Some code:\n\n    Here it is\n\nPara")]
    [InlineData("[text](https://example.com/_file/#~anchor)")]
    [InlineData("#hashtag")]
    [InlineData("#######")]
    [InlineData("```\n1\n\n```")]
    [InlineData("Hello\nWorld")]
    [InlineData("Hello\n\nWorld")]
    [InlineData("> Hello\n> World")]
    [InlineData("> Hello\n\n> World")]
    [InlineData("Hey\n\n---")]
    [InlineData("***")]
    [InlineData("# Hello\nWorld")]
    [InlineData("###### Hello")]
    [InlineData("```\nHello\n\nWorld\n```")]
    [InlineData("```ruby\nHello\n```\n\nWorld")]
    [InlineData("5. Hello\n\n6. World")]
    [InlineData("* Hello\n\n* World")]
    [InlineData("![alt](src \"title\")")]
    [InlineData("_Hello_")]
    [InlineData("__Hello__")]
    [InlineData("Hel`lo wo`rld")]
    [InlineData("___[`Hello`](https://example.com)___")]
    [InlineData("hello -- world")]
    [InlineData("hello --- world")]
    [InlineData("hello... world")]
    [InlineData("+-5")]
    [InlineData("(tm)")]
    [InlineData("\"hello\" world")]
    [InlineData("it's here")]
    [InlineData("`hello -- world`")]
    [InlineData("Hey ~~wrod~~ ~~uord~~ World")]
    [InlineData("***~~[`Hello`](https://example.com)~~***")]
    [InlineData("[Example](https://example.com \"Example Title\")")]
    [InlineData("<https://example.com/~user> https://example.com/~user")]
    [InlineData("[Docs](https://example.com/docs/#section)")]
    [InlineData("- ```plaintext\n  console.log('Hello, world!');\n  ```")]
    [InlineData("    print('Hello, world!')")]
    [InlineData("-     print('Hello, world!')")]
    [InlineData("| Header 1 | Header 2 |\n| --- | --- |\n| Cell 1 | Cell 2 |")]
    [InlineData("| Left | Center | Right |\n| :--- | :---: | ---: |\n| A | B | C |")]
    [InlineData("| Line1<br>Line2 | Cell 2 |\n| --- | --- |\n| Cell 3 | Cell 4 |")]
    [InlineData("| Link | Note |\n| --- | --- |\n| [x\\|y](https://example.com \"title\\|value\") | ok |")]
    public void RegeneratingKeepsTheStructure(string markdown)
    {
        var written = MarkdownWriter.Write(Regenerated(markdown), markdown);

        Assert.Equal(Canonical.Html(markdown), Canonical.Html(written));
    }

    [Theory]
    [InlineData("hello!")]
    [InlineData("# one\n\n## two\n\nthree")]
    [InlineData("> once\n\n> > twice")]
    [InlineData("* foo\n\n  * bar\n\n  * baz\n\n* quux")]
    [InlineData("1. Hello\n\n2. Goodbye\n\n3. Nest\n\n   1. Hey\n\n   2. Aye")]
    [InlineData("3. Foo\n\n4. Bar")]
    [InlineData("* # Foo")]
    [InlineData("Some code:\n\n```\nHere it is\n```\n\nPara")]
    [InlineData("foo\n\n```javascript\n1\n```")]
    [InlineData("Hello. Some *em* text, some **strong** text, and some `code`")]
    [InlineData("This is **strong *emphasized text with `code` in* it**")]
    [InlineData("**[link](foo) is bold**")]
    [InlineData("[link *foo **bar** `#`*](foo)")]
    [InlineData("**`code` is bold**")]
    [InlineData("``` one backtick: ` two backticks: `` ```")]
    [InlineData("foo\\\nbar")]
    [InlineData("*foo\\\nbar*")]
    [InlineData("My [link](foo) goes to foo")]
    [InlineData("Link to <https://prosemirror.net>")]
    [InlineData("[foo.html](foo.html)")]
    [InlineData("[a](x.html \"title \\\"quoted\\\"\")")]
    [InlineData("[link](http://foo.com/a_b_c)")]
    [InlineData("Link to *<https://prosemirror.net>*")]
    [InlineData("Here's an image: ![x](img.png)")]
    [InlineData("line one\\\nline two")]
    [InlineData("one two\n\n---\n\nthree")]
    [InlineData("Foo < img> bar")]
    [InlineData("1\\. foo")]
    [InlineData("* foo\n* bar")]
    [InlineData("1. foo\n2. bar")]
    [InlineData("* list item\n\n```\ncode\n```")]
    [InlineData("foo`*`")]
    [InlineData("abc_def")]
    [InlineData("abc___def")]
    [InlineData("\\_abc\\_")]
    [InlineData("/\\_abc\\_)")]
    [InlineData("<https://example.com/_file/#~anchor>")]
    [InlineData("* 1\\. hi\n\n* x")]
    [InlineData("123 [0. com](foo)\n\n123 [2. 2](foo)")]
    [InlineData("1.2kg")]
    [InlineData("\\### text")]
    [InlineData("\\###")]
    [InlineData("\\#hashtag")]
    [InlineData("\\#######")]
    [InlineData("\\#\u3000\u3053\u3093\u306b\u3061\u306f")]
    [InlineData("# 1. foo")]
    [InlineData("+++")]
    [InlineData("````\n```\ncode\n```\n````")]
    public void RegeneratingReproducesCanonicalText(string markdown)
    {
        Assert.Equal(markdown, MarkdownWriter.Write(Regenerated(markdown), markdown));
    }

    private static Document Regenerated(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        foreach (var block in Blocks(document))
        {
            block.Pristine = false;

            if (block is Table table)
            {
                foreach (var cell in table.Rows.SelectMany(row => row.Cells))
                {
                    cell.Pristine = false;
                }
            }
        }

        return document;
    }

    private static IEnumerable<Block> Blocks(Block block) => block is BlockContainer container ? container.Children.SelectMany(Blocks).Prepend(block) : [block];

    private static string Serialize(params Inline[] inlines)
    {
        var document = MarkdownParser.Parse("plain");
        ((Paragraph)document.Children[0]).ReplaceInlines(inlines);
        document.Children[0].Pristine = false;
        return MarkdownWriter.Write(document, "plain");
    }

    private static InlineContainer Mark(InlineContainer container, params Inline[] children)
    {
        foreach (var child in children)
        {
            container.Add(child);
        }

        return container;
    }

    private static Link Anchor(string destination, params Inline[] children) => (Link)Mark(new Link { Destination = destination }, children);

    [Fact]
    public void SerializesALineBreakInsideAMark()
    {
        Assert.Equal("**text1\ntext2**", Serialize(Mark(new Strong(), new Text("text1"), new SoftLineBreak(), new Text("text2"))));
    }

    [Fact]
    public void WritesTrailingHardBreaksWithSpaces()
    {
        Assert.Equal("a  \n", Serialize(new Text("a"), new LineBreak { Backslash = true }, new LineBreak { Backslash = true }));
    }

    [Fact]
    public void ExpelsEnclosingWhitespaceFromNestedMarks()
    {
        Assert.Equal("Some emphasized text with  ***whitespace***   surrounding the emphasis.", Serialize(new Text("Some emphasized text with"), Mark(new Strong(), Mark(new Emphasis(), new Text("  whitespace   "))), new Text("surrounding the emphasis.")));
    }

    [Fact]
    public void ExpelsWhitespaceFromEmphasisWithANestedLink()
    {
        Assert.Equal("One *two [three](foo) four* five", Serialize(new Text("One"), Mark(new Emphasis(), new Text(" two "), Anchor("foo", new Text("three")), new Text(" four ")), new Text("five")));
    }

    [Fact]
    public void ExpelsWhitespaceBeforeAHardBreak()
    {
        Assert.Equal("**foo** \\\nbar", Serialize(Mark(new Strong(), new Text("foo "), new LineBreak { Backslash = true }), new Text("bar")));
    }

    [Fact]
    public void MovesAHardBreakEndingAMarkAfterTheMark()
    {
        Assert.Equal("**foo**  \n", Serialize(Mark(new Strong(), new Text("foo"), new LineBreak { Backslash = true })));
    }

    [Fact]
    public void DropsMarksWhoseWhitespaceIsAllExpelled()
    {
        Assert.Equal("Text with an emphasized space", Serialize(new Text("Text with"), Mark(new Emphasis(), new Text(" ")), new Text("an emphasized space")));
    }

    [Theory]
    [InlineData("foo):", "[link](foo\\):)")]
    [InlineData("(foo", "[link](\\(foo)")]
    public void EscapesParenthesesInLinkDestinations(string destination, string expected)
    {
        Assert.Equal(expected, Serialize(Anchor(destination, new Text("link"))));
    }

    [Theory]
    [InlineData("foo):", "![x](foo\\):)")]
    [InlineData("(foo", "![x](\\(foo)")]
    public void EscapesParenthesesInImageDestinations(string destination, string expected)
    {
        Assert.Equal(expected, Serialize(Mark(new Image { Destination = destination }, new Text("x"))));
    }

    [Fact]
    public void KeepsAQuoteInALinkDestinationWithATitle()
    {
        Assert.Equal("[link](foo%20\" \"bar\")", Serialize((Link)Mark(new Link { Destination = "foo%20\"", Title = "bar" }, new Text("link"))));
    }

    [Theory]
    [InlineData("**text**.text")]
    [InlineData("*text*.text")]
    [InlineData("~~text~~.text")]
    public void ExpelsTrailingPunctuationThatWouldBreakTheClosingDelimiter(string expected)
    {
        InlineContainer container = expected[0] == '~' ? new Strikethrough() : expected[1] == '*' ? new Strong() : new Emphasis();

        Assert.Equal(expected, Serialize(Mark(container, new Text("text.")), new Text("text")));
    }

    [Fact]
    public void ExpelsLeadingPunctuationThatWouldBreakTheOpeningDelimiter()
    {
        Assert.Equal("text.**word**", Serialize(new Text("text"), Mark(new Strong(), new Text(".word"))));
    }

    [Fact]
    public void ExpelsARunOfBoundaryPunctuation()
    {
        Assert.Equal("**text**..text", Serialize(Mark(new Strong(), new Text("text..")), new Text("text")));
    }

    [Theory]
    [InlineData("Hello.", null, "**Hello.**")]
    [InlineData("Hello.", " world", "**Hello.** world")]
    [InlineData("Hello", "!", "**Hello**!")]
    [InlineData("text", "text", "**text**text")]
    public void LeavesValidBoundariesUntouched(string strong, string? following, string expected)
    {
        var inlines = new List<Inline> { Mark(new Strong(), new Text(strong)) };

        if (following != null)
        {
            inlines.Add(new Text(following));
        }

        Assert.Equal(expected, Serialize(inlines.ToArray()));
    }

    [Fact]
    public void ExpelsPunctuationFromNestedStrongAndEmphasis()
    {
        Assert.Equal("***text***.text", Serialize(Mark(new Strong(), Mark(new Emphasis(), new Text("text."))), new Text("text")));
    }
}
