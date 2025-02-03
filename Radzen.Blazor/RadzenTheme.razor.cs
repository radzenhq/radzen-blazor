using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

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
        public string Theme { get; set; }

        /// <summary>
        /// Enables WCAG contrast requirements. If set to true additional CSS file will be loaded.
        /// </summary>
        [Parameter]
        public bool Wcag { get; set; }

        private string theme;

        private bool wcag;

        private static readonly string Version = typeof(RadzenTheme).Assembly.GetName().Version.ToString();

        private string Href => $"{Path}/{theme}-base.css?v={Version}";

        private string WcagHref => $"{Path}/{theme}-wcag.css?v={Version}";

        private string Path => Embedded ? $"_content/Radzen.Blazor/css" : "css";

        private bool Embedded => theme switch
        {
            "material" => true,
            "material-dark" => true,
            "standard" => true,
            "standard-dark" => true,
            "humanistic" => true,
            "humanistic-dark" => true,
            "software" => true,
            "software-dark" => true,
            "default" => true,
            "dark" => true,
            _ => false
        };

        private PersistingComponentStateSubscription persistingSubscription;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            theme = ThemeService.Theme ?? GetCurrentTheme();
            wcag = ThemeService.Wcag ?? Wcag;

            ThemeService.SetTheme(theme, true);

            theme = theme.ToLowerInvariant();

            ThemeService.ThemeChanged += OnThemeChanged;

            persistingSubscription = PersistentComponentState.RegisterOnPersisting(PersistTheme);

            base.OnInitialized();
        }

        private string GetCurrentTheme()
        {
            if (PersistentComponentState.TryTakeFromJson(nameof(Theme), out string theme))
            {
                return theme;
            }

            return Theme;
        }

        private Task PersistTheme()
        {
            PersistentComponentState.PersistAsJson(nameof(Theme), theme);

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
            persistingSubscription.Dispose();
        }
    }
}