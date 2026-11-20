using System;
using System.Globalization;
using Bunit;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartCultureTests
    {
        static readonly CultureInfo German = new CultureInfo("de-DE");

        static void WithCurrentCulture(string name, Action action)
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(name);

            try
            {
                action();
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void Scales_FormatTick_UseSpecifiedCulture()
        {
            WithCurrentCulture("en-US", () =>
            {
                Assert.Equal("300.000,00 €", new LinearScale().FormatTick("{0:C}", 300000d, German));
                Assert.Equal("300.000,00 €", new LogarithmicScale().FormatTick("{0:C}", 300000d, German));
                Assert.Equal("1,5", new LinearScale().FormatTick("", 1.5, German));
                Assert.Equal("März", new DateScale().FormatTick("{0:MMMM}", new DateTime(2026, 3, 5), German));
                Assert.Equal("05.03.2026", new DateScale().FormatTick("", new DateTime(2026, 3, 5), German));
            });
        }

        [Fact]
        public void ValueAxis_FormatString_UsesChartCulture()
        {
            WithCurrentCulture("en-US", () =>
            {
                using var ctx = CreateChartContext();

                var chart = ctx.RenderComponent<RadzenChart>(p => p
                    .Add(c => c.Culture, German)
                    .AddChildContent<RadzenValueAxis>(axis => axis
                        .Add(a => a.FormatString, "{0:C}"))
                    .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.Data, SampleData)));

                chart.InvokeAsync(() => chart.Instance.Resize(400, 300)).GetAwaiter().GetResult();

                Assert.Contains("€", chart.Markup);
                Assert.DoesNotContain("$", chart.Markup);
            });
        }

        [Fact]
        public void ValueAxis_FormatString_UsesCurrentCultureWithoutChartCulture()
        {
            WithCurrentCulture("de-DE", () =>
            {
                using var ctx = CreateChartContext();

                var chart = ctx.RenderComponent<RadzenChart>(p => p
                    .AddChildContent<RadzenValueAxis>(axis => axis
                        .Add(a => a.FormatString, "{0:C}"))
                    .AddChildContent<RadzenColumnSeries<DataItem>>(s => s
                        .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                        .Add(x => x.ValueProperty, nameof(DataItem.Value))
                        .Add(x => x.Data, SampleData)));

                chart.InvokeAsync(() => chart.Instance.Resize(400, 300)).GetAwaiter().GetResult();

                Assert.Contains("€", chart.Markup);
            });
        }

        [Fact]
        public void LinearGauge_FormatString_UsesGaugeCulture()
        {
            WithCurrentCulture("en-US", () =>
            {
                using var ctx = new TestContext();
                ctx.JSInterop.Mode = JSRuntimeMode.Loose;

                var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                    parameters.Add(p => p.Style, "width:300px;height:150px")
                        .Add(p => p.Culture, German)
                        .AddChildContent<RadzenLinearGaugeScale>(scale =>
                            scale.Add(p => p.FormatString, "{0:N1}")
                                .Add(p => p.Min, 0)
                                .Add(p => p.Max, 100)
                                .Add(p => p.Step, 50)));

                Assert.Contains(">50,0<", component.Markup);
            });
        }

        [Fact]
        public void RadialGauge_FormatString_UsesGaugeCulture()
        {
            WithCurrentCulture("en-US", () =>
            {
                using var ctx = new TestContext();
                ctx.JSInterop.Mode = JSRuntimeMode.Loose;

                var component = ctx.RenderComponent<RadzenRadialGauge>(parameters =>
                    parameters.Add(p => p.Style, "width:300px;height:300px")
                        .Add(p => p.Culture, German)
                        .AddChildContent<RadzenRadialGaugeScale>(scale =>
                            scale.Add(p => p.FormatString, "{0:N1}")
                                .Add(p => p.Min, 0)
                                .Add(p => p.Max, 100)
                                .Add(p => p.Step, 50)));

                component.InvokeAsync(() => component.Instance.Resize(300, 300)).GetAwaiter().GetResult();

                Assert.Contains("50,0", component.Markup);
            });
        }
    }
}
