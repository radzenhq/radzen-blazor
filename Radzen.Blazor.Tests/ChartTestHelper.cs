using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor.Tests
{
    internal static class ChartTestHelper
    {
        internal static TestContext CreateChartContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.Setup<Rect>("Radzen.createChart", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 200, Height = 200 });
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 200, Height = 200 });
            ctx.JSInterop.Setup<double[]>("Radzen.createRangeNavigator", _ => true)
                .SetResult(new double[] { 200, 200 });
            ctx.Services.AddScoped<TooltipService>();
            return ctx;
        }

        internal class DataItem
        {
            public string Category { get; set; } = "";
            public double Value { get; set; }
            public double Value2 { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public double Open { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
            public double Close { get; set; }
            public double Target { get; set; }
            public double LowerWhisker { get; set; }
            public double LowerQuartile { get; set; }
            public double Median { get; set; }
            public double UpperQuartile { get; set; }
            public double UpperWhisker { get; set; }
            public bool IsSummary { get; set; }
        }

        internal static DataItem[] SampleData => new[]
        {
            new DataItem { Category = "A", Value = 10, Value2 = 20, Min = 5, Max = 25 },
            new DataItem { Category = "B", Value = 20, Value2 = 30, Min = 10, Max = 35 },
            new DataItem { Category = "C", Value = 15, Value2 = 25, Min = 8, Max = 30 },
        };
    }
}
