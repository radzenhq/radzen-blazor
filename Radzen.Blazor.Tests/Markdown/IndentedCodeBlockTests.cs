using Xunit;

namespace Radzen.Blazor.Markdown.Tests;

public class IndentedCodeBlockTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);

        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(@"    a simple
      indented code block
", @"<document>
    <code_block>a simple
  indented code block
</code_block>
</document>")]
    [InlineData(@"  - foo

    bar", @"<document>
    <list type=""bullet"" tight=""false"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <paragraph>
                <text>bar</text>
            </paragraph>
        </item>
    </list>
</document>")]
    [InlineData(
@"
1.  foo

    - bar", @"<document>
    <list type=""ordered"" start=""1"" tight=""false"">
        <item>
            <paragraph>
                <text>foo</text>
            </paragraph>
            <list type=""bullet"" tight=""true"">
                <item>
                    <paragraph>
                        <text>bar</text>
                    </paragraph>
                </item>
            </list>
        </item>
    </list>
</document>")]
    [InlineData(@"    <a/>
    *hi*

    - one", @"<document>
    <code_block>&lt;a/&gt;
*hi*

- one
</code_block>
</document>")]
    [InlineData(@"    chunk1

    chunk2
  
 
 
    chunk3", @"<document>
    <code_block>chunk1

chunk2



chunk3
</code_block>
</document>")]
    [InlineData(@"    chunk1
      
      chunk2", @"<document>
    <code_block>chunk1
  
  chunk2
</code_block>
</document>")]
    [InlineData(@"Foo
    bar", @"<document>
    <paragraph>
        <text>Foo</text>
        <softbreak />
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData(@"    foo
bar", @"<document>
    <code_block>foo
</code_block>
    <paragraph>
        <text>bar</text>
    </paragraph>
</document>")]
    [InlineData(@"# Heading
    foo
Heading
------
    foo
----", @"<document>
    <heading level=""1"">
        <text>Heading</text>
    </heading>
    <code_block>foo
</code_block>
    <heading level=""2"">
        <text>Heading</text>
    </heading>
    <code_block>foo
</code_block>
    <thematic_break />
</document>")]
    [InlineData(@"        foo
    bar", @"<document>
    <code_block>    foo
bar
</code_block>
</document>")]
    [InlineData(@"
    
    foo
", @"<document>
    <code_block>foo
</code_block>
</document>")]
    [InlineData(@"    foo  ", @"<document>
    <code_block>foo  
</code_block>
</document>")]
    public void Parse_IndentedCodeBlock(string markdown, string expected)
    {
        Assert.Equal(expected, ToXml(markdown));
    }
}