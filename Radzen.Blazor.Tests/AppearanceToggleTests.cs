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

        static TestContext CreateContext(string? theme)
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<ThemeService>();

            if (theme != null)
            {
                ctx.Services.GetRequiredService<ThemeService>().SetTheme(theme, false);
            }

            return ctx;
        }

        [Fact]
        public void AppearanceToggle_Renders_Default_AriaLabel()
        {
            using var ctx = CreateContext("material");

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            Assert.Equal("Dark theme", component.Find("button").GetAttribute("aria-label"));
        }

        [Fact]
        public void AppearanceToggle_Renders_AriaLabel_Parameter()
        {
            using var ctx = CreateContext("material");

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, "Toggle dark mode");
                parameters.AddUnmatched("aria-label", "From attributes");
            });

            Assert.Equal("Toggle dark mode", component.Find("button").GetAttribute("aria-label"));
        }

        [Fact]
        public void AppearanceToggle_Renders_AriaLabel_FromAttributes()
        {
            using var ctx = CreateContext("material");

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.AddUnmatched("aria-label", "From attributes");
                parameters.AddUnmatched("title", "Theme");
            });

            var button = component.Find("button");

            Assert.Equal("From attributes", button.GetAttribute("aria-label"));
            Assert.Equal("Theme", button.GetAttribute("title"));
        }

        [Fact]
        public void AppearanceToggle_LightTheme_IsNotPressed()
        {
            using var ctx = CreateContext("material");

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            var button = component.Find("button");

            Assert.Equal("false", button.GetAttribute("aria-pressed"));
            Assert.DoesNotContain("rz-state-active", button.ClassName);
            Assert.False(button.HasAttribute("disabled"));
            Assert.Contains("dark_mode", button.InnerHtml);
        }

        [Fact]
        public void AppearanceToggle_DarkTheme_IsPressed()
        {
            using var ctx = CreateContext("material-dark");

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            var button = component.Find("button");

            Assert.Equal("true", button.GetAttribute("aria-pressed"));
            Assert.Contains("rz-state-active", button.ClassName);
            Assert.Contains("light_mode", button.InnerHtml);
        }

        [Fact]
        public void AppearanceToggle_Click_SwitchesTheme_AndPressedState()
        {
            using var ctx = CreateContext("material");
            var themeService = ctx.Services.GetRequiredService<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            component.Find("button").Click();

            Assert.Equal("material-dark", themeService.Theme);
            Assert.Equal("true", component.Find("button").GetAttribute("aria-pressed"));

            component.Find("button").Click();

            Assert.Equal("material", themeService.Theme);
            Assert.Equal("false", component.Find("button").GetAttribute("aria-pressed"));
        }

        [Fact]
        public void AppearanceToggle_ThemeChange_UpdatesPressedState()
        {
            using var ctx = CreateContext("default");
            var themeService = ctx.Services.GetRequiredService<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            component.InvokeAsync(() => themeService.SetTheme("dark")).Wait();

            Assert.Equal("true", component.Find("button").GetAttribute("aria-pressed"));
        }

        [Fact]
        public void AppearanceToggle_UnknownTheme_IsDisabled()
        {
            using var ctx = CreateContext("my-custom");
            var themeService = ctx.Services.GetRequiredService<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>();

            var button = component.Find("button");

            Assert.True(button.HasAttribute("disabled"));

            button.Click();

            Assert.Equal("my-custom", themeService.Theme);
            Assert.Equal("false", component.Find("button").GetAttribute("aria-pressed"));
        }

        [Fact]
        public void AppearanceToggle_UnknownTheme_WithExplicitPair_IsEnabled()
        {
            using var ctx = CreateContext("my-custom");
            var themeService = ctx.Services.GetRequiredService<ThemeService>();

            var component = ctx.RenderComponent<RadzenAppearanceToggle>(parameters =>
            {
                parameters.Add(p => p.LightTheme, "my-custom");
                parameters.Add(p => p.DarkTheme, "my-custom-dark");
            });

            var button = component.Find("button");

            Assert.False(button.HasAttribute("disabled"));
            Assert.Equal("false", button.GetAttribute("aria-pressed"));

            button.Click();

            Assert.Equal("my-custom-dark", themeService.Theme);
            Assert.Equal("true", component.Find("button").GetAttribute("aria-pressed"));
        }
    }
}
