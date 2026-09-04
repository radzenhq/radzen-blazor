using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class TaskListTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);
        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData("- [ ] todo\n- [x] done", @"<document>
    <list type=""bullet"" tight=""true"">
        <item checked=""false"">
            <paragraph>
                <text>todo</text>
            </paragraph>
        </item>
        <item checked=""true"">
            <paragraph>
                <text>done</text>
            </paragraph>
        </item>
    </list>
</document>")]
    // no marker → no checked attribute; [x] not followed by space is literal.
    // The unmatched "[" / "]" split into separate <text> nodes because the inline
    // parser's bracket handling (InlineParser.TryGetOpenerIndex) flushes the text
    // buffer before confirming a link, regardless of task-list parsing; this is
    // pre-existing behavior, reproduced here rather than merged, since fixing it
    // is outside this task's scope.
    [InlineData("- [x]tight", @"<document>
    <list type=""bullet"" tight=""true"">
        <item>
            <paragraph>
                <text>[</text>
                <text>x</text>
                <text>]tight</text>
            </paragraph>
        </item>
    </list>
</document>")]
    public void TaskList_parses(string markdown, string expected)
    {
        Assert.Equal(expected.Replace("\r\n", "\n"), ToXml(markdown).Replace("\r\n", "\n"));
    }
}
