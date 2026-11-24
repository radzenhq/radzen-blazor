using System.Threading.Tasks;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartAccessibilityTests
    {
        static IRenderedComponent<RadzenChart> RenderChart(TestContext ctx, string ariaLabel = null, bool allowKeyboardNavigation = true)
        {
            return ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.AriaLabel, ariaLabel)
                .Add(x => x.AllowKeyboardNavigation, allowKeyboardNavigation)
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.Title, "Revenue")
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData))
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.Title, "Cost")
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value2))
                    .Add(x => x.Data, SampleData)));
        }

        [Fact]
        public async Task Plot_Is_Focusable_And_Named_After_Series()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var plot = chart.Find("svg.rz-chart-plot");

            Assert.Equal("application", plot.GetAttribute("role"));
            Assert.Equal("0", plot.GetAttribute("tabindex"));
            Assert.EndsWith(": Revenue, Cost", plot.GetAttribute("aria-label"));

            var hint = chart.Find($"[id=\"{plot.GetAttribute("aria-describedby")}\"]");

            Assert.False(string.IsNullOrEmpty(hint.TextContent));
        }

        [Fact]
        public async Task AriaLabel_Overrides_Generated_Name()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, ariaLabel: "Revenue by quarter");

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            Assert.Equal("Revenue by quarter", chart.Find("svg.rz-chart-plot").GetAttribute("aria-label"));
        }

        [Fact]
        public async Task Plot_Is_Image_When_Keyboard_Navigation_Is_Off()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx, allowKeyboardNavigation: false);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            var plot = chart.Find("svg.rz-chart-plot");

            Assert.Equal("img", plot.GetAttribute("role"));
            Assert.False(plot.HasAttribute("tabindex"));
            Assert.Empty(chart.FindAll(".rz-chart-announcement"));
        }

        [Fact]
        public async Task Left_And_Right_Move_Along_A_Series_And_Continue_Into_The_Adjacent_One()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            Assert.Equal("Revenue, A, 10", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            Assert.Equal("Revenue, B, 20", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            Assert.Equal("Revenue, C, 15", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            Assert.Equal("Cost, A, 20", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowLeft"));
            Assert.Equal("Revenue, C, 15", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowLeft"));
            Assert.Equal("Revenue, B, 20", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowLeft"));
            Assert.Equal("Revenue, A, 10", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowLeft"));
            Assert.Equal("Revenue, A, 10", chart.Find(".rz-chart-announcement").TextContent);
        }

        [Fact]
        public async Task Up_And_Down_Switch_Series_At_The_Same_Point()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            Assert.Equal("Revenue, A, 10", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            Assert.Equal("Revenue, B, 20", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowDown"));
            Assert.Equal("Cost, B, 30", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowDown"));
            Assert.Equal("Revenue, B, 20", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowUp"));
            Assert.Equal("Cost, B, 30", chart.Find(".rz-chart-announcement").TextContent);
        }

        [Fact]
        public async Task Home_And_End_Stay_In_The_Current_Series()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            await chart.InvokeAsync(() => chart.Instance.KeyDown("End"));
            Assert.Equal("Revenue, C, 15", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("Home"));
            Assert.Equal("Revenue, A, 10", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowDown"));
            Assert.Equal("Cost, A, 20", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("End"));
            Assert.Equal("Cost, C, 25", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            Assert.Equal("Cost, C, 25", chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("Home"));
            Assert.Equal("Cost, A, 20", chart.Find(".rz-chart-announcement").TextContent);
        }

        [Fact]
        public async Task Escape_And_Blur_Clear_The_Active_Point()
        {
            using var ctx = CreateChartContext();
            var chart = RenderChart(ctx);

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            await chart.InvokeAsync(() => chart.Instance.KeyDown("Escape"));
            Assert.Equal(string.Empty, chart.Find(".rz-chart-announcement").TextContent);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            await chart.InvokeAsync(() => chart.Instance.KeyDown("Blur"));
            Assert.Equal(string.Empty, chart.Find(".rz-chart-announcement").TextContent);
        }

        [Fact]
        public async Task Enter_Raises_SeriesClick_For_The_Active_Point()
        {
            using var ctx = CreateChartContext();
            SeriesClickEventArgs clicked = null;

            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.SeriesClick, args => clicked = args)
                .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                    .Add(x => x.Title, "Revenue")
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(400, 300));

            await chart.InvokeAsync(() => chart.Instance.KeyDown("Enter"));
            Assert.Null(clicked);

            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            await chart.InvokeAsync(() => chart.Instance.KeyDown("ArrowRight"));
            await chart.InvokeAsync(() => chart.Instance.KeyDown("Enter"));

            Assert.NotNull(clicked);
            Assert.Equal("B", ((DataItem)clicked.Data).Category);
        }

        [Fact]
        public async Task Sparkline_Is_Not_A_Tab_Stop()
        {
            using var ctx = CreateChartContext();

            var chart = ctx.RenderComponent<RadzenSparkline>(p => p
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, SampleData)));

            await chart.InvokeAsync(() => chart.Instance.Resize(200, 50));

            var plot = chart.Find("svg.rz-chart-plot");

            Assert.Equal("img", plot.GetAttribute("role"));
            Assert.False(plot.HasAttribute("tabindex"));
        }
    }
}
