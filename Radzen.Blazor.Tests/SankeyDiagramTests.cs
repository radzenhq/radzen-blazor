using System.Collections.Generic;
using System.Linq;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class SankeyDiagramTests
    {
        class Flow
        {
            public string Source { get; set; } = "";
            public string Target { get; set; } = "";
            public double Value { get; set; }
        }

        static readonly List<Flow> flows = new()
        {
            new Flow { Source = "A", Target = "B", Value = 10 },
            new Flow { Source = "A", Target = "C", Value = 5 },
            new Flow { Source = "B", Target = "D", Value = 7 },
        };

        static IRenderedComponent<RadzenSankeyDiagram<Flow>> Render(TestContext ctx, bool visible)
        {
            return ctx.RenderComponent<RadzenSankeyDiagram<Flow>>(parameters =>
            {
                parameters.Add(p => p.Visible, visible);
                parameters.Add(p => p.Data, flows);
                parameters.Add(p => p.SourceProperty, nameof(Flow.Source));
                parameters.Add(p => p.TargetProperty, nameof(Flow.Target));
                parameters.Add(p => p.ValueProperty, nameof(Flow.Value));
            });
        }

        [Fact]
        public void SankeyDiagram_Renders_Nodes_And_Links()
        {
            using var ctx = CreateChartContext();

            var component = Render(ctx, true);

            Assert.Contains("rz-sankey-diagram", component.Markup);
            Assert.Contains("<svg", component.Markup);
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));
        }

        [Fact]
        public void SankeyDiagram_Visible_Toggle_Recreates_Resizable()
        {
            using var ctx = CreateChartContext();

            var component = Render(ctx, false);

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Empty(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Contains("rz-sankey-diagram", component.Markup);
            Assert.Contains("<svg", component.Markup);
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.disposeElement"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Contains("<svg", component.Markup);
            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createResizable"));
        }
    }
}
