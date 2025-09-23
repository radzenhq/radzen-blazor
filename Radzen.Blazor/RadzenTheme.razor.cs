using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace Radzen.Blazor
{
    /// <summary>
    /// Registers and manages the current theme. Requires <see cref="ThemeService" /> to be registered in the DI container.
    /// </summary>
    public partial class RadzenTheme : IDisposable
    {
        /// <summary>
        /// Gets or sets the theme.
        /// </summary>
        [Parameter]
        public string? Theme { get; set; }

        /// <summary>
        /// When set to true the icon font will be preloadd.
        /// </summary>
        [Parameter]
        public bool PreloadIconFont { get; set; } = true;

        /// <summary>
        /// Enables WCAG contrast requirements. If set to true additional CSS file will be loaded.
        /// </summary>
        [Parameter]
        public bool Wcag { get; set; }

        private PersistentComponentState? persistentComponentState;

        private string? theme;

        private bool wcag;

        private string Href => ThemeService.Href;

        private string WcagHref => ThemeService.WcagHref;

        private string IconFontPath => ThemeService.Embedded ? $"_content/Radzen.Blazor/fonts" : "fonts";

        private string IconFontHref => $"{IconFontPath}/MaterialSymbolsOutlined.woff2";

        private PersistingComponentStateSubscription? persistingSubscription;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            persistentComponentState = ServiceProvider.GetService<PersistentComponentState>();

            theme = ThemeService.Theme ?? GetCurrentTheme();
            wcag = ThemeService.Wcag ?? Wcag;

            ThemeService.SetTheme(theme, true);

            theme = theme?.ToLowerInvariant();

            ThemeService.ThemeChanged += OnThemeChanged;

            persistingSubscription = persistentComponentState?.RegisterOnPersisting(PersistTheme);

            base.OnInitialized();
        }

        private string? GetCurrentTheme()
        {
            if (persistentComponentState?.TryTakeFromJson(nameof(Theme), out string? theme) == true)
            {
                return theme;
            }

            return Theme;
        }

        private Task PersistTheme()
        {
            persistentComponentState?.PersistAsJson(nameof(Theme), theme);

            return Task.CompletedTask;
        }

        private void OnThemeChanged()
        {
            var requiresChange = false;

            var newTheme = ThemeService.Theme.ToLowerInvariant();

            if (theme != newTheme)
            {
                theme = newTheme;
                requiresChange = true;
            }

            var newWcag = ThemeService.Wcag ?? Wcag;

            if (wcag != newWcag)
            {
                wcag = newWcag;
                requiresChange = true;
            }

            if (requiresChange)
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// Releases all resources used by the component.
        /// </summary>
        public void Dispose()
        {
            if (ThemeService != null)
            {
                ThemeService.ThemeChanged -= OnThemeChanged;
            }

            persistingSubscription?.Dispose();
        }
    }
}