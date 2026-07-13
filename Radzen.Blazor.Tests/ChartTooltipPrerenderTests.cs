using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartTooltipPrerenderTests
    {
        class PrerenderJSRuntime : IJSRuntime
        {
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) =>
                throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time. This is because the component is being statically rendered.");

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
                InvokeAsync<TValue>(identifier, args);
        }

        class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
            }
        }

        [Fact]
        public async Task DisposeAsync_Completes_WhenChartTooltipIsOpenedDuringPrerendering()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IJSRuntime, PrerenderJSRuntime>();
            services.AddSingleton<NavigationManager, TestNavigationManager>();
            services.AddScoped<TooltipService>();

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();

            var renderer = new HtmlRenderer(scope.ServiceProvider, NullLoggerFactory.Instance);
            var service = scope.ServiceProvider.GetRequiredService<TooltipService>();

            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                await renderer.RenderComponentAsync<RadzenChartTooltip>();

                service.OpenChartTooltip(default, 10, 10, _ => builder => builder.AddContent(0, "tooltip"), new ChartTooltipOptions());
            });

            var dispose = Task.Run(async () =>
            {
                await renderer.DisposeAsync();
                await scope.DisposeAsync();
                await provider.DisposeAsync();
            });

            var completed = await Task.WhenAny(dispose, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.True(completed == dispose, "RadzenChartTooltip.DisposeAsync did not complete after a chart tooltip was opened during prerendering.");

            await dispose;
        }

        [Fact]
        public void Sparkline_RenderedWithoutMouseInteraction_DoesNotOpenTooltip()
        {
            using var ctx = CreateChartContext();
            var tooltipService = ctx.Services.GetRequiredService<TooltipService>();
            var opens = new List<(double x, double y)>();
            tooltipService.OnOpenChartTooltip += (element, x, y, options) => opens.Add((x, y));

            var data = new[]
            {
                new DataItem { Category = "Quiz 1", Value = 92 },
                new DataItem { Category = "Quiz 2", Value = 58 },
                new DataItem { Category = "Quiz 3", Value = 52 },
                new DataItem { Category = "Quiz 4", Value = 61 },
            };

            ctx.RenderComponent<RadzenSparkline>(p => p
                .Add(c => c.Style, "width: 100px; height: 20px")
                .AddChildContent<RadzenLineSeries<DataItem>>(s => s
                    .Add(x => x.Smooth, true)
                    .Add(x => x.CategoryProperty, nameof(DataItem.Category))
                    .Add(x => x.ValueProperty, nameof(DataItem.Value))
                    .Add(x => x.Data, data))
                .AddChildContent<RadzenCategoryAxis>(a => a
                    .Add(x => x.Visible, false)
                    .Add(x => x.Padding, -20)));

            Assert.Empty(opens);
        }
    }
}
