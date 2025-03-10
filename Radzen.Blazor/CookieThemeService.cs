using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Radzen
{
    /// <summary>
    /// Specifies the SameSite attribute for the cookie.
    /// </summary>
    public enum CookieSameSiteMode
    {
        /// <summary>
        /// No SameSite attribute.
        /// </summary>
        None,
        /// <summary>
        /// Lax SameSite attribute.
        /// </summary>
        Lax,
        /// <summary>
        /// Strict SameSite attribute.
        /// </summary>
        Strict
    }

    /// <summary>
    /// Options for the <see cref="CookieThemeService" />.
    /// </summary>
    public class CookieThemeServiceOptions
    {
        /// <summary>
        /// Gets or sets the cookie name.
        /// </summary>
        public string Name { get; set; } = "Theme";

        /// <summary>
        /// Gets or sets the cookie duration.
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromDays(365);

        /// <summary>
        /// Gets or sets a value indicating whether to use secure cookies.
        /// </summary>
        public bool IsSecure { get; set; } = false;

        /// <summary>
        /// Gets or sets the SameSite attribute for the cookie.
        /// </summary>
        public CookieSameSiteMode? SameSite { get; set; } = null;
    }

    /// <summary>
    /// Persist the current theme in a cookie. Requires <see cref="ThemeService" /> to be registered in the DI container.
    /// </summary>
    public class CookieThemeService
    {
        private readonly CookieThemeServiceOptions options;
        private readonly IJSRuntime jsRuntime;
        private readonly ThemeService themeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CookieThemeService" /> class.
        /// </summary>
        public CookieThemeService(IJSRuntime jsRuntime, ThemeService themeService, IOptions<CookieThemeServiceOptions> options)
        {
            this.jsRuntime = jsRuntime;
            this.themeService = themeService;
            this.options = options.Value;

            themeService.ThemeChanged += OnThemeChanged;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var cookies = await jsRuntime.InvokeAsync<string>("eval", "document.cookie");

                var themeCookie = cookies?.Split("; ").Select(x =>
                {
                    var parts = x.Split("=");

                    return (Key: parts[0], Value: parts[1]);
                })
                .FirstOrDefault(x => x.Key == options.Name);

                var theme = themeCookie?.Value;

                if (!string.IsNullOrEmpty(theme) && themeService.Theme != theme)
                {
                    themeService.SetTheme(theme);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void OnThemeChanged()
        {
            var expiration = DateTime.Now.Add(options.Duration);
            var cookie = $"{options.Name}={themeService.Theme}; expires={expiration:R}; path=/";

            if (options.SameSite.HasValue)
            {
                cookie += $"; SameSite={options.SameSite}";
            }

            if (options.IsSecure)
            {
                cookie += "; Secure";
            }

            _ = jsRuntime.InvokeVoidAsync("eval", $"document.cookie = \"{cookie}\"");
        }
    }

    /// <summary>
    /// Extension methods to register the <see cref="CookieThemeService" />.
    /// </summary>
    public static class CookieThemeServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="CookieThemeService" /> to the service collection.
        /// </summary>
        public static IServiceCollection AddRadzenCookieThemeService(this IServiceCollection services)
        {
            services.AddOptions<CookieThemeServiceOptions>();
            services.AddScoped<CookieThemeService>();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="CookieThemeService" /> to the service collection with the specified configuration.
        /// </summary>
        public static IServiceCollection AddRadzenCookieThemeService(this IServiceCollection services, Action<CookieThemeServiceOptions> configure)
        {
            services.Configure(configure);
            services.AddScoped<CookieThemeService>();

            return services;
        }
    }
}