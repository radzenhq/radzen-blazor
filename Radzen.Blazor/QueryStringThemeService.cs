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

#if NET7_0_OR_GREATER
        private readonly IDisposable registration;
#endif
        private readonly QueryStringThemeServiceOptions options;
        private readonly PropertyInfo hasAttachedJSRuntimeProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringThemeService" /> class.
        /// </summary>
        public QueryStringThemeService(NavigationManager navigationManager, ThemeService themeService, IOptions<QueryStringThemeServiceOptions> options)
        {
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

#if NET7_0_OR_GREATER
            try
            {
                registration = navigationManager.RegisterLocationChangingHandler(OnLocationChanging);
            }
            catch (NotSupportedException)
            {
                // HttpNavigationManager does not support that
            }
#endif
        }

        private bool RequiresChange((string theme, bool? wcag, bool? rightToLeft) state) =>
            (state.theme != null && !string.Equals(themeService.Theme, state.theme, StringComparison.OrdinalIgnoreCase)) ||
            themeService.Wcag != state.wcag || themeService.RightToLeft != state.rightToLeft;

#if NET7_0_OR_GREATER
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
#endif

        private (string theme, bool? wcag, bool? rightToLeft) GetStateFromQueryString(string uri)
        {
            var queryString = uri.Contains('?') ? uri[(uri.IndexOf('?') + 1)..] : string.Empty;

            var query = HttpUtility.ParseQueryString(queryString.Contains('#') ? queryString[..queryString.IndexOf('#')] : queryString);

            bool? wcag = query.Get(options.WcagParameter) != null ? query.Get(options.WcagParameter) == "true" : null;
            bool? rtl = query.Get(options.RightToLeftParameter) != null ? query.Get(options.RightToLeftParameter) == "true" : null;

            return (query.Get(options.ThemeParameter), wcag, rtl);
        }

        private string GetUriWithStateQueryParameters(string uri)
        {
            var parameters = new Dictionary<string, object>
            {
                { options.ThemeParameter, themeService.Theme.ToLowerInvariant() },
            };

            if (themeService.Wcag.HasValue)
            {
                parameters.Add(options.WcagParameter, themeService.Wcag.Value ? "true" : "false");
            }

            if (themeService.RightToLeft.HasValue)
            {
                parameters.Add(options.RightToLeftParameter, themeService.RightToLeft.Value ? "true" : "false");
            }

            return navigationManager.GetUriWithQueryParameters(uri, parameters);
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

#if NET7_0_OR_GREATER
            registration?.Dispose();
#endif
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