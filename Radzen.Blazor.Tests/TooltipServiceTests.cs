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
    }
}
