using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class CookieThemeServiceTests
    {
        private static ThemeService CreateThemeService(TestContext ctx, string cookies)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.Setup<string>("Radzen.getCookies").SetResult(cookies);
            ctx.Services.AddScoped<ThemeService>();

            return ctx.Services.GetRequiredService<ThemeService>();
        }

        [Theory]
        [InlineData("Theme=dark")]
        [InlineData("other=1; Theme=dark")]
        [InlineData("nameless; Theme=dark")]
        [InlineData("other=1; nameless; Theme=dark")]
        [InlineData("; Theme=dark")]
        public void CookieThemeService_AppliesThemeCookie_WhenOtherCookiesHaveNoValue(string cookies)
        {
            using var ctx = new TestContext();
            var themeService = CreateThemeService(ctx, cookies);

            using var cookieThemeService = new CookieThemeService(ctx.JSInterop.JSRuntime, themeService, Options.Create(new CookieThemeServiceOptions()));

            Assert.Equal("dark", themeService.Theme);
        }

        [Theory]
        [InlineData("")]
        [InlineData("nameless")]
        [InlineData("other=1")]
        [InlineData("Theme")]
        public void CookieThemeService_KeepsTheme_WhenNoThemeCookieValueIsPresent(string cookies)
        {
            using var ctx = new TestContext();
            var themeService = CreateThemeService(ctx, cookies);
            themeService.SetTheme("material", false);

            using var cookieThemeService = new CookieThemeService(ctx.JSInterop.JSRuntime, themeService, Options.Create(new CookieThemeServiceOptions()));

            Assert.Equal("material", themeService.Theme);
        }
    }
}
