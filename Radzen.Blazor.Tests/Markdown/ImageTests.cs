using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class ImageTests
{

    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"![foo](/url ""title"")", @"<document>
    <paragraph>
        <image destination=""/url"" title=""title"">
            <text>foo</text>
        </image>
    </paragraph>
</document>")]
    [InlineData(@"![foo ![bar](/url)](/url2)", @"<document>
    <paragraph>
        <image destination=""/url2"" title="""">
            <text>foo </text>
            <image destination=""/url"" title="""">
                <text>bar</text>
            </image>
        </image>
    </paragraph>
</document>")]
    [InlineData(@"![foo [bar](/url)](/url2)", @"<document>
    <paragraph>
        <image destination=""/url2"" title="""">
            <text>foo </text>
            <link destination=""/url"" title="""">
                <text>bar</text>
            </link>
        </image>
    </paragraph>
</document>")]
    [InlineData(@"![foo](train.jpg)", @"<document>
    <paragraph>
        <image destination=""train.jpg"" title="""">
            <text>foo</text>
        </image>
    </paragraph>
</document>")]
    [InlineData(@"My ![foo bar](/path/to/train.jpg  ""title""   )", @"<document>
    <paragraph>
        <text>My </text>
        <image destination=""/path/to/train.jpg"" title=""title"">
            <text>foo bar</text>
        </image>
    </paragraph>
</document>")]
    [InlineData(@"![foo](<url>)", @"<document>
    <paragraph>
        <image destination=""url"" title="""">
            <text>foo</text>
        </image>
    </paragraph>
</document>")]
    [InlineData(@"![](/url)", @"<document>
    <paragraph>
        <image destination=""/url"" title="""" />
    </paragraph>
</document>")]
    [InlineData(@"__Applications__ ![macOS DMG](/assets/img/macOS-drag-and-drop.png)", @"<document>
    <paragraph>
        <strong>
            <text>Applications</text>
        </strong>
        <text> </text>
        <image destination=""/assets/img/macOS-drag-and-drop.png"" title="""">
            <text>macOS DMG</text>
        </image>
    </paragraph>
</document>")]
    public void Parse_BasicImages_ReturnsImageElement(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}
