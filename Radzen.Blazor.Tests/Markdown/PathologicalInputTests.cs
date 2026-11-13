using System.Diagnostics;
using System.Linq;
using Bunit;
using Radzen.Blazor;
using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class PathologicalInputTests
{
    [Theory]
    [InlineData(">", "blockquote")]
    [InlineData("- ", "li")]
    [InlineData("1. ", "li")]
    public void Container_Nesting_Stops_At_100_Levels(string marker, string element)
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = ctx.RenderComponent<RadzenMarkdown>(parameters => parameters
            .Add(p => p.Text, string.Concat(Enumerable.Repeat(marker, 6000)) + "a"));

        var depth = component.FindAll(element).Max(node =>
        {
            var count = 0;
            for (var current = node; current != null; current = current.ParentElement)
            {
                count += current.LocalName == element ? 1 : 0;
            }
            return count;
        });

        Assert.InRange(depth, 1, 100);
    }

    [Fact]
    public void Multi_Line_Paragraph_Parses_In_Linear_Time()
    {
        var markdown = string.Concat(Enumerable.Repeat("xxxxxxx\n", 40000));

        var stopwatch = Stopwatch.StartNew();
        var document = MarkdownParser.Parse(markdown);

        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"took {stopwatch.ElapsedMilliseconds} ms");
        Assert.Single(document.Children);
    }
}
