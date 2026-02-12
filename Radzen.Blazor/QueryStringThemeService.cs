using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Radzen
{
    /// <summary>
    /// Options for the <see cref="QueryStringThemeService" />.
    /// </summary>
    public class QueryStringThemeServiceOptions
    {
        /// <summary>
        /// Gets or sets the query string parameter for the theme.
        /// </summary>
        public string ThemeParameter { get; set; } = "theme";
        /// <summary>
        /// Gets or sets the query string parameter for the wcag compatible color theme.
        /// </summary>
        public string WcagParameter { get; set; } = "wcag";

        /// <summary>
        /// Gets or sets the query string parameter for the right to left theme.
        /// </summary>
        public string RightToLeftParameter { get; set; } = "rtl";
    }

    /// <summary>
    /// Persist the current theme in the query string. Requires <see cref="ThemeService" /> to be registered in the DI container.
    /// </summary>
    public class QueryStringThemeService : IDisposable
    {
        private readonly NavigationManager navigationManager;
        private readonly ThemeService themeService;

        private readonly IDisposable? registration;
        private readonly QueryStringThemeServiceOptions? options;
        private readonly PropertyInfo? hasAttachedJSRuntimeProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringThemeService" /> class.
        /// </summary>
        public QueryStringThemeService(NavigationManager navigationManager, ThemeService themeService, IOptions<QueryStringThemeServiceOptions> options)
        {
            ArgumentNullException.ThrowIfNull(navigationManager);
            ArgumentNullException.ThrowIfNull(themeService);
            ArgumentNullException.ThrowIfNull(options);

            this.navigationManager = navigationManager;
            this.themeService = themeService;
            this.options = options.Value;

            hasAttachedJSRuntimeProperty = navigationManager.GetType().GetProperty("HasAttachedJSRuntime");

            var state = GetStateFromQueryString(navigationManager.Uri);

            if (state.theme != null && RequiresChange(state))
            {
                themeService.SetTheme(new ThemeOptions
                {
                    Theme = state.theme,
                    Wcag = state.wcag,
                    RightToLeft = state.rightToLeft,
                    TriggerChange = true
                });
            }

            themeService.ThemeChanged += OnThemeChanged;

            try
            {
                registration = navigationManager.RegisterLocationChangingHandler(OnLocationChanging);
            }
            catch (NotSupportedException)
            {
                // HttpNavigationManager does not support RegisterLocationChangingHandler.
                // This means we are server-side rendering. Unsubscribe from ThemeChanged to
                // avoid calling NavigateTo which would cause a 302 redirect during prerendering.
                themeService.ThemeChanged -= OnThemeChanged;
            }
        }

        private bool RequiresChange((string? theme, bool? wcag, bool? rightToLeft) state) =>
            (state.theme != null && !string.Equals(themeService.Theme, state.theme, StringComparison.OrdinalIgnoreCase)) ||
            themeService.Wcag != state.wcag || themeService.RightToLeft != state.rightToLeft;

        private ValueTask OnLocationChanging(LocationChangingContext context)
        {
            var state = GetStateFromQueryString(context.TargetLocation);

            if (RequiresChange(state))
            {
                context.PreventNavigation();

                navigationManager.NavigateTo(GetUriWithStateQueryParameters(context.TargetLocation), replace: true);
            }

            return ValueTask.CompletedTask;
        }

        private (string? theme, bool? wcag, bool? rightToLeft) GetStateFromQueryString(string uri)
        {
            var queryString = uri.Contains('?', StringComparison.Ordinal) ? uri[(uri.IndexOf('?', StringComparison.Ordinal) + 1)..] : string.Empty;

            var query = HttpUtility.ParseQueryString(queryString.Contains('#', StringComparison.Ordinal) ? queryString[..queryString.IndexOf('#', StringComparison.Ordinal)] : queryString);

            bool? wcag = options?.WcagParameter != null ? (query.Get(options.WcagParameter) != null ? query.Get(options.WcagParameter) == "true" : null) : null;
            bool? rtl = options?.RightToLeftParameter != null ? (query.Get(options.RightToLeftParameter) != null ? query.Get(options.RightToLeftParameter) == "true" : null) : null;

            return (query?.Get(options?.ThemeParameter), wcag, rtl);
        }

        private string GetUriWithStateQueryParameters(string uri)
        {
            var parameters = new Dictionary<string, object?>
            {
                { options?.ThemeParameter ?? string.Empty, themeService?.Theme?.ToLowerInvariant() ?? string.Empty },
            };

            if (themeService?.Wcag != null && options?.WcagParameter != null)
            {
                parameters.Add(options.WcagParameter, themeService.Wcag.Value ? "true" : "false");
            }

            if (themeService?.RightToLeft != null && options?.RightToLeftParameter != null)
            {
                parameters.Add(options.RightToLeftParameter, themeService.RightToLeft.Value ? "true" : "false");
            }

            return navigationManager.GetUriWithQueryParameters(uri, new Dictionary<string, object?>(parameters));
        }

        private void OnThemeChanged()
        {

            if (hasAttachedJSRuntimeProperty is null || hasAttachedJSRuntimeProperty.GetValue(navigationManager) is true)
            {
                var state = GetStateFromQueryString(navigationManager.Uri);

                navigationManager.NavigateTo(GetUriWithStateQueryParameters(navigationManager.Uri),
                    forceLoad: state.rightToLeft != themeService.RightToLeft);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            themeService.ThemeChanged -= OnThemeChanged;

            registration?.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Extension methods to register the <see cref="QueryStringThemeService" />.
    /// </summary>
    public static class QueryStringThemeServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="QueryStringThemeService" /> to the service collection.
        /// </summary>
        public static IServiceCollection AddRadzenQueryStringThemeService(this IServiceCollection services)
        {
            services.AddOptions<QueryStringThemeServiceOptions>();
            services.AddScoped<QueryStringThemeService>();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="QueryStringThemeService" /> to the service collection with the specified condiguration.
        /// </summary>
        public static IServiceCollection AddRadzenQueryStringThemeService(this IServiceCollection services, Action<QueryStringThemeServiceOptions> configure)
        {
            services.Configure(configure);
            services.AddScoped<QueryStringThemeService>();

            return services;
        }
    }
}