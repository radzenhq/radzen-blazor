using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;
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

    [Theory]
    [InlineData("*a ", "a* ")]
    [InlineData("**a ", "a** ")]
    [InlineData("_a ", "a_ ")]
    [InlineData("~~a ", "a~~ ")]
    [InlineData("![", "](u)")]
    [InlineData("[![", "](u)](v)")]
    public void Inline_Nesting_Stops_At_100_Levels(string open, string close)
    {
        var markdown = string.Concat(Enumerable.Repeat(open, 6000)) + "a" + string.Concat(Enumerable.Repeat(close, 6000));

        var document = MarkdownParser.Parse(markdown);

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(document.Children));
        Assert.InRange(MaxDepth(paragraph.Children), 1, 100);

        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = ctx.RenderComponent<RadzenMarkdown>(parameters => parameters.Add(p => p.Text, markdown));
        Assert.Contains("rz-markdown", component.Markup);
    }

    [Fact]
    public void Table_Fills_At_Most_10000_Missing_Cells()
    {
        var header = "|" + string.Concat(Enumerable.Repeat(" a |", 300)) + "\n|" + string.Concat(Enumerable.Repeat(" - |", 300)) + "\n";
        var markdown = header + string.Concat(Enumerable.Repeat("| x |\n", 20000));

        var stopwatch = Stopwatch.StartNew();
        var table = Assert.IsType<Table>(Assert.Single(MarkdownParser.Parse(markdown).Children));

        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"took {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal(20000, table.Rows.Count(row => row is not TableHeaderRow));
        Assert.InRange(table.Rows.Where(row => row is not TableHeaderRow).Sum(row => row.Cells.Count), 20000, 20000 + 10000);
    }

    [Fact]
    public async Task Markdown_Without_Links_Renders_Without_NavigationManager()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var services = new ServiceCollection();
        services.AddSingleton(ctx.JSInterop.JSRuntime);
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<RadzenMarkdown>(ParameterView.FromDictionary(new Dictionary<string, object> { ["Text"] = "# Title\n\n**bold** <b>x</b>" }));
            return output.ToHtmlString();
        });

        Assert.Contains("<b>x</b>", html);
    }

    private static int MaxDepth(IEnumerable<Inline> roots)
    {
        var max = 0;
        var pending = new Stack<(Inline Node, int Depth)>(roots.Select(node => (node, 1)));

        while (pending.Count > 0)
        {
            var (node, depth) = pending.Pop();

            if (node is InlineContainer container)
            {
                max = System.Math.Max(max, depth);

                foreach (var child in container.Children)
                {
                    pending.Push((child, depth + 1));
                }
            }
        }

        return max;
    }
}
