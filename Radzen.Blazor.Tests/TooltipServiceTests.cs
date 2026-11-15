using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TooltipServiceTests
    {
        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<TooltipService>();
            return ctx;
        }

        [Fact]
        public async Task Open_DoesNotModifyCallerOptions_WhenTooltipIsRepositioned()
        {
            using var ctx = CreateContext();
            var tooltip = ctx.RenderComponent<RadzenTooltip>();
            var service = ctx.Services.GetRequiredService<TooltipService>();

            var sharedOptions = new TooltipOptions { Position = TooltipPosition.Top, Duration = 0 };

            await tooltip.InvokeAsync(() => service.Open(default, "first", sharedOptions));

            Assert.Contains("rz-top-tooltip-content", tooltip.Markup);

            await tooltip.InvokeAsync(() => tooltip.Instance.CloseTooltip("left"));

            Assert.Equal(TooltipPosition.Top, sharedOptions.Position);

            await tooltip.InvokeAsync(() => service.Open(default, "second", sharedOptions));

            Assert.Contains("rz-top-tooltip-content", tooltip.Markup);
            Assert.DoesNotContain("rz-left-tooltip-content", tooltip.Markup);
        }

        [Fact]
        public async Task Open_DoesNotWriteTextOrContentToCallerOptions()
        {
            using var ctx = CreateContext();
            var tooltip = ctx.RenderComponent<RadzenTooltip>();
            var service = ctx.Services.GetRequiredService<TooltipService>();

            var sharedOptions = new TooltipOptions();

            await tooltip.InvokeAsync(() => service.Open(default, "hello", sharedOptions));

            Assert.Null(sharedOptions.Text);
            Assert.Contains("hello", tooltip.Markup);

            RenderFragment<TooltipService> content = _ => builder => builder.AddContent(0, "fragment");

            await tooltip.InvokeAsync(() => service.Open(default, content, sharedOptions));

            Assert.Null(sharedOptions.ChildContent);
            Assert.Null(sharedOptions.Text);
            Assert.Contains("fragment", tooltip.Markup);
        }

        [Fact]
        public async Task OpenOnTheTop_DoesNotModifyCallerPosition()
        {
            using var ctx = CreateContext();
            var tooltip = ctx.RenderComponent<RadzenTooltip>();
            var service = ctx.Services.GetRequiredService<TooltipService>();

            var sharedOptions = new TooltipOptions { Position = TooltipPosition.Bottom };

            await tooltip.InvokeAsync(() => service.OpenOnTheTop(default, "text", sharedOptions));

            Assert.Equal(TooltipPosition.Bottom, sharedOptions.Position);
            Assert.Contains("rz-top-tooltip-content", tooltip.Markup);
        }

        static int LastOpenedTooltipId(TestContext ctx)
        {
            var invocation = ctx.JSInterop.Invocations["Radzen.openTooltip"][^1];

            return (int)invocation.Arguments[^1];
        }

        [Fact]
        public async Task UpdatePosition_IsIgnored_WhenAnotherTooltipWasOpenedMeanwhile()
        {
            using var ctx = CreateContext();
            var tooltip = ctx.RenderComponent<RadzenTooltip>();
            var service = ctx.Services.GetRequiredService<TooltipService>();

            var options = new TooltipOptions { Position = TooltipPosition.Top, Duration = 0 };

            await tooltip.InvokeAsync(() => service.Open(default, "first", options));

            var firstId = LastOpenedTooltipId(ctx);

            await tooltip.InvokeAsync(() => service.Close());
            await tooltip.InvokeAsync(() => service.Open(default, "second", options));

            var secondId = LastOpenedTooltipId(ctx);

            Assert.NotEqual(firstId, secondId);

            await tooltip.InvokeAsync(() => tooltip.Instance.UpdatePosition("left", firstId));

            tooltip.Render();

            Assert.Contains("second", tooltip.Markup);
            Assert.Contains("rz-top-tooltip-content", tooltip.Markup);
            Assert.DoesNotContain("rz-left-tooltip-content", tooltip.Markup);
            Assert.Equal("top", ctx.JSInterop.Invocations["Radzen.openTooltip"][^1].Arguments[4]);
        }

        [Fact]
        public async Task UpdatePosition_IsApplied_ToTheTooltipItWasReportedFor()
        {
            using var ctx = CreateContext();
            var tooltip = ctx.RenderComponent<RadzenTooltip>();
            var service = ctx.Services.GetRequiredService<TooltipService>();

            await tooltip.InvokeAsync(() => service.Open(default, "text", new TooltipOptions { Position = TooltipPosition.Top, Duration = 0 }));

            await tooltip.InvokeAsync(() => tooltip.Instance.UpdatePosition("left", LastOpenedTooltipId(ctx)));

            tooltip.Render();

            Assert.Contains("rz-left-tooltip-content", tooltip.Markup);
            Assert.DoesNotContain("rz-top-tooltip-content", tooltip.Markup);
        }
    }
}
