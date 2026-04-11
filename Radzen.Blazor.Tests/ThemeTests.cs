using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ThemeTests
    {
        [Fact]
        public void Theme_Renders_Embedded_Default_CssPath()
        {
            const string path = "_content/Radzen.Blazor/css";

            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenTheme>(parameters =>
            {
                parameters.Add(p => p.Theme, "material");
            });

            Assert.Contains(path, component.Markup);
        }

        [Fact]
        public void Theme_Renders_Non_Embedded_Default_CssPath()
        {
            const string path = "\"css";

            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenTheme>(parameters =>
            {
                parameters.Add(p => p.Theme, "awesome");
            });

            Assert.Contains($"{path}/awesome-base.css", component.Markup);
        }

        [Fact]
        public void Theme_Renders_Non_Embedded_Custom_CssPath()
        {
            const string path = "_content/custom-assembly/css";

            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenTheme>(parameters =>
            {
                parameters.Add(p => p.CssPath, path);
                parameters.Add(p => p.Theme, "my-light");
            });

            Assert.Contains(path, component.Markup);
        }

        [Fact]
        public void Theme_Renders_Embedded_CssPath_For_Embedded_Themes()
        {
            const string path = "_content/Radzen.Blazor/css";
            const string customPath = "_content/custom-assembly/css";

            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenTheme>(parameters =>
            {
                parameters.Add(p => p.CssPath, customPath);
                parameters.Add(p => p.Theme, "material");
            });

            Assert.Contains(path, component.Markup);
            Assert.DoesNotContain(customPath, component.Markup);
        }

        [Fact]
        public void Theme_WcagHref_Uses_Custom_CssPath()
        {
            const string path = "_content/custom-assembly/css";

            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenTheme>(parameters =>
            {
                parameters.Add(p => p.CssPath, path);
                parameters.Add(p => p.Theme, "my-light");
                parameters.Add(p => p.Wcag, true);
            });

            Assert.Contains($"{path}/my-light-wcag.css", component.Markup);
        }
    }
}
