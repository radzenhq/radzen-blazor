using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class RangeNavigatorPrerenderTests
    {
        class NullResultJSRuntime : IJSRuntime
        {
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => new(Task.FromResult(default(TValue)));

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);
        }

        class StaticRenderJSRuntime : IJSRuntime
        {
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) =>
                throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time. This is because the component is being statically rendered.");

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);
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

        class DataItem
        {
            public DateOnly Date { get; set; }
            public double Value { get; set; }
        }

        static async Task<string> RenderAsync(IJSRuntime jsRuntime, RenderFragment fragment)
        {
            var services = new ServiceCollection();
            services.AddSingleton(jsRuntime);
            services.AddSingleton<NavigationManager, TestNavigationManager>();
            services.AddRadzenComponents();

            await using var provider = services.BuildServiceProvider();
            await using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

            var render = renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new System.Collections.Generic.Dictionary<string, object?> { ["ChildContent"] = fragment });
                var output = await renderer.RenderComponentAsync<Host>(parameters);
                return output.ToHtmlString();
            });

            var completed = await Task.WhenAny(render, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.True(completed == render, "RadzenRangeNavigator did not finish prerendering.");

            return await render;
        }

        class Host : ComponentBase
        {
            [Parameter]
            public RenderFragment? ChildContent { get; set; }

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.AddContent(0, ChildContent);
            }
        }

        static RenderFragment BareNavigator(bool showAxis) => builder =>
        {
            builder.OpenComponent<RadzenRangeNavigator>(0);
            builder.AddAttribute(1, nameof(RadzenRangeNavigator.Style), "width: 500px; height: 100px");
            builder.AddAttribute(2, nameof(RadzenRangeNavigator.ShowAxis), showAxis);
            builder.AddAttribute(3, nameof(RadzenRangeNavigator.ShowHandleLabels), showAxis);
            builder.AddAttribute(4, nameof(RadzenRangeNavigator.ShowTooltip), showAxis);
            builder.CloseComponent();
        };

        static RenderFragment NavigatorWithSeries(object? data) => builder =>
        {
            builder.OpenComponent<RadzenRangeNavigator>(0);
            builder.AddAttribute(1, nameof(RadzenRangeNavigator.Style), "width: 500px; height: 100px");
            builder.AddAttribute(2, nameof(RadzenRangeNavigator.ShowAxis), true);
            builder.AddAttribute(3, nameof(RadzenRangeNavigator.ChildContent), (RenderFragment)(child =>
            {
                child.OpenComponent<RadzenRangeNavigatorLineSeries<DataItem>>(0);
                child.AddAttribute(1, "Data", data);
                child.AddAttribute(2, "CategoryProperty", nameof(DataItem.Date));
                child.AddAttribute(3, "ValueProperty", nameof(DataItem.Value));
                child.CloseComponent();
            }));
            builder.CloseComponent();
        };

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Prerender_WithoutData_Completes_WhenInteropReturnsNothing(bool showAxis)
        {
            var html = await RenderAsync(new NullResultJSRuntime(), BareNavigator(showAxis));

            Assert.Contains("rz-range-nav-window", html);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Prerender_WithoutData_Completes_WhenInteropIsUnavailable(bool showAxis)
        {
            var html = await RenderAsync(new StaticRenderJSRuntime(), BareNavigator(showAxis));

            Assert.Contains("rz-range-nav-window", html);
        }

        [Fact]
        public async Task Prerender_WithSeriesWithoutData_Completes()
        {
            var html = await RenderAsync(new NullResultJSRuntime(), NavigatorWithSeries(null));

            Assert.Contains("rz-range-nav-window", html);
        }

        static RenderFragment NavigatorWithDataCreatedPerRender() => builder =>
        {
            builder.OpenComponent<RadzenRangeNavigator>(0);
            builder.AddAttribute(1, nameof(RadzenRangeNavigator.Style), "width: 500px; height: 100px");
            builder.AddAttribute(2, nameof(RadzenRangeNavigator.ChildContent), (RenderFragment)(child =>
            {
                child.OpenComponent<RadzenRangeNavigatorLineSeries<DataItem>>(0);
                child.AddAttribute(1, "Data", new[]
                {
                    new DataItem { Date = new DateOnly(2024, 1, 1), Value = 42000 },
                    new DataItem { Date = new DateOnly(2024, 2, 1), Value = 38000 },
                });
                child.AddAttribute(2, "CategoryProperty", nameof(DataItem.Date));
                child.AddAttribute(3, "ValueProperty", nameof(DataItem.Value));
                child.CloseComponent();
            }));
            builder.CloseComponent();
        };

        [Fact]
        public async Task Prerender_WithSeriesDataCreatedPerRender_Completes()
        {
            var html = await RenderAsync(new NullResultJSRuntime(), NavigatorWithDataCreatedPerRender());

            Assert.Contains("rz-range-nav-window", html);
        }

        [Fact]
        public async Task Prerender_WithDateOnlySeries_Completes()
        {
            var data = new[]
            {
                new DataItem { Date = new DateOnly(2025, 1, 1), Value = 1 },
                new DataItem { Date = new DateOnly(2025, 12, 31), Value = 2 },
            };

            var html = await RenderAsync(new NullResultJSRuntime(), NavigatorWithSeries(data));

            Assert.Contains("rz-range-nav-window", html);
        }
    }
}
