using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartViewChangeAnchorTests
    {
        class PinRightHost : ComponentBase
        {
            public double Start;
            public double End = 1;
            public double Zoom = 100;
            public RadzenChart Chart;
            public bool PinRight = true;

            static readonly DataItem[] Months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }
                .Select((month, index) => new DataItem { Category = month, Value = 10 + index })
                .ToArray();

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenChart>(0);
                builder.AddAttribute(1, nameof(RadzenChart.AllowZoom), true);
                builder.AddAttribute(2, nameof(RadzenChart.AllowPan), true);
                builder.AddAttribute(3, nameof(RadzenChart.ViewStart), Start);
                builder.AddAttribute(4, nameof(RadzenChart.ViewStartChanged), EventCallback.Factory.Create<double>(this, value => Start = value));
                builder.AddAttribute(5, nameof(RadzenChart.ViewEnd), End);
                builder.AddAttribute(6, nameof(RadzenChart.ViewEndChanged), EventCallback.Factory.Create<double>(this, value => End = value));
                builder.AddAttribute(7, nameof(RadzenChart.Zoom), Zoom);
                builder.AddAttribute(8, nameof(RadzenChart.ZoomChanged), EventCallback.Factory.Create<double>(this, value => Zoom = value));
                builder.AddAttribute(9, nameof(RadzenChart.ViewChange), EventCallback.Factory.Create<Radzen.ChartViewChangeEventArgs>(this, args =>
                {
                    if (PinRight)
                    {
                        Start = 1 - (args.ViewEnd - args.ViewStart);
                        End = 1;
                    }
                }));
                builder.AddAttribute(10, "ChildContent", (RenderFragment)(content =>
                {
                    content.OpenComponent<RadzenLineSeries<DataItem>>(0);
                    content.AddAttribute(1, nameof(RadzenLineSeries<DataItem>.CategoryProperty), nameof(DataItem.Category));
                    content.AddAttribute(2, nameof(RadzenLineSeries<DataItem>.ValueProperty), nameof(DataItem.Value));
                    content.AddAttribute(3, nameof(RadzenLineSeries<DataItem>.Data), Months);
                    content.CloseComponent();
                }));
                builder.AddComponentReferenceCapture(11, reference => Chart = (RadzenChart)reference);
                builder.CloseComponent();
            }
        }

        static async Task<IRenderedComponent<PinRightHost>> RenderHost(TestContext ctx)
        {
            var host = ctx.RenderComponent<PinRightHost>();
            await host.InvokeAsync(() => host.Instance.Chart.Resize(600, 300));
            return host;
        }

        [Fact]
        public async Task WheelZoom_ViewChangeHandlerCanAnchorRangeToEnd()
        {
            using var ctx = CreateChartContext();
            var host = await RenderHost(ctx);
            var chart = host.Instance.Chart;

            await host.InvokeAsync(() => chart.OnWheel(300, -1));

            Assert.Equal(1, chart.ZoomEnd);
            Assert.Equal(0.2, chart.ZoomStart, 10);
            Assert.Equal(host.Instance.Start, chart.ZoomStart);
            Assert.Equal(host.Instance.End, chart.ZoomEnd);
            Assert.Equal(125, host.Instance.Zoom);
            Assert.Contains("Dec", host.Markup);
            Assert.DoesNotContain("Jan", host.Markup);

            await host.InvokeAsync(() => chart.OnWheel(300, -1));

            Assert.Equal(1, chart.ZoomEnd);
            Assert.Equal(0.36, chart.ZoomStart, 10);
            Assert.Equal(host.Instance.Start, chart.ZoomStart);
            Assert.Equal(156, host.Instance.Zoom);
        }

        [Fact]
        public async Task WheelZoom_WithoutHandlerRewriteKeepsCursorAnchoredRange()
        {
            using var ctx = CreateChartContext();
            var host = await RenderHost(ctx);
            var chart = host.Instance.Chart;
            host.Instance.PinRight = false;

            await host.InvokeAsync(() => chart.OnWheel(300, -1));

            Assert.True(chart.ZoomEnd < 1);
            Assert.True(chart.ZoomStart > 0);
            Assert.Equal(host.Instance.Start, chart.ZoomStart);
            Assert.Equal(host.Instance.End, chart.ZoomEnd);
            Assert.Equal(125, host.Instance.Zoom);
        }

        [Fact]
        public async Task ScrollbarPan_ViewChangeHandlerRewriteIsAdopted()
        {
            using var ctx = CreateChartContext();
            var host = await RenderHost(ctx);
            var chart = host.Instance.Chart;

            await host.InvokeAsync(() => chart.OnWheel(300, -1));
            await host.InvokeAsync(() => chart.OnPan(0));

            Assert.Equal(1, chart.ZoomEnd);
            Assert.Equal(0.2, chart.ZoomStart, 10);
            Assert.Equal(host.Instance.Start, chart.ZoomStart);
        }
    }
}
