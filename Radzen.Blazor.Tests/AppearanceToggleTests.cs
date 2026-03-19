using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AppearanceToggleTests
    {
        [Fact]
        public void AppearanceToggle_Renders()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            Assert.NotNull(component.Instance);
        }

        [Fact]
        public void AppearanceToggle_DefaultVariant_IsText()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            Assert.Equal(Variant.Text, component.Instance.Variant);
        }

        [Fact]
        public void AppearanceToggle_DefaultButtonStyle_IsBase()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            Assert.Equal(ButtonStyle.Base, component.Instance.ButtonStyle);
        }

        [Fact]
        public void AppearanceToggle_Renders_CustomVariant()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Outlined);
            });

            Assert.Equal(Variant.Outlined, component.Instance.Variant);
        }

        [Fact]
        public void AppearanceToggle_Renders_CustomButtonStyle()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.Add(p => p.ButtonStyle, ButtonStyle.Primary);
            });

            Assert.Equal(ButtonStyle.Primary, component.Instance.ButtonStyle);
        }

        [Fact]
        public void AppearanceToggle_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.Empty(component.Markup.Trim());
        }

        [Fact]
        public void AppearanceToggle_Renders_ToggleShade()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.Add(p => p.ToggleShade, Shade.Lighter);
            });

            Assert.Equal(Shade.Lighter, component.Instance.ToggleShade);
        }

        [Fact]
        public void AppearanceToggle_Accepts_CustomLightTheme()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.Add(p => p.LightTheme, "my-light");
            });

            Assert.Equal("my-light", component.Instance.LightTheme);
        }

        [Fact]
        public void AppearanceToggle_Accepts_CustomDarkTheme()
        {
            using var ctx = new TestContext();
            ctx.Services.AddScoped<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.Add(p => p.DarkTheme, "my-dark");
            });

            Assert.Equal("my-dark", component.Instance.DarkTheme);
        }
    }
}
