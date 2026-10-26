using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Dark or light theme switch. Requires <see cref="ThemeService" /> to be registered in the DI container.
    /// </summary>
    public partial class RadzenAppearanceToggle : RadzenComponent
    {
        [Inject]
        private ThemeService? ThemeService { get; set; }
        /// <summary>
        /// Gets or sets the switch button variant.
        /// </summary>
        /// <value>The switch button variant.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Text;

        /// <summary>
        /// Gets or sets the switch button style.
        /// </summary>
        /// <value>The switch button style.</value>
        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Base;

        /// <summary>
        /// Gets or sets the switch button toggled shade.
        /// </summary>
        /// <value>The switch button toggled shade.</value>
        [Parameter]
        public Shade ToggleShade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the switch button toggled style.
        /// </summary>
        /// <value>The switch button toggled style.</value>
        [Parameter]
        public ButtonStyle ToggleButtonStyle { get; set; } = ButtonStyle.Base;

        /// <summary>
        /// Gets or sets the light theme. Not set by default - the component uses the light version of the current theme.
        /// </summary>
        [Parameter]
        public string? LightTheme { get; set; }

        /// <summary>
        /// Gets or sets the dark theme. Not set by default - the component uses the dark version of the current theme.
        /// </summary>
        [Parameter]
        public string? DarkTheme { get; set; }

        private string? ariaLabel;

        /// <summary>
        /// Gets or sets the accessible name of the button, exposed via the <c>aria-label</c> attribute. The button reports <c>aria-pressed="true"</c> while the dark theme is active.
        /// When not set, an <c>aria-label</c> attribute supplied through <see cref="RadzenComponent.Attributes"/> is used, otherwise the localized "Dark theme" text.
        /// </summary>
        [Parameter]
        public string AriaLabel { get => CurrentAriaLabel; set => ariaLabel = value; }

        private string CurrentAriaLabel => ariaLabel ?? (Attributes != null && Attributes.TryGetValue("aria-label", out var attribute) ? attribute?.ToString() : null) ?? Localize(nameof(RadzenStrings.AppearanceToggle_AriaLabel));

        private bool IsInert => CurrentLightTheme == CurrentDarkTheme;

        private string CurrentLightTheme => LightTheme ?? ThemeService?.Theme?.ToLowerInvariant() switch
        {
            "dark" => "default",
            "material-dark" => "material",
            "fluent-dark" => "fluent",
            "material3-dark" => "material3",
            "material3expressive-dark" => "material3expressive",
            "zen-dark" => "zen",
            "software-dark" => "software",
            "humanistic-dark" => "humanistic",
            "standard-dark" => "standard",
            _ => ThemeService?.Theme ?? string.Empty,
        };

        private string CurrentDarkTheme => DarkTheme ?? ThemeService?.Theme?.ToLowerInvariant() switch
        {
            "default" => "dark",
            "material" => "material-dark",
            "fluent" => "fluent-dark",
            "material3" => "material3-dark",
            "material3expressive" => "material3expressive-dark",
            "zen" => "zen-dark",
            "software" => "software-dark",
            "humanistic" => "humanistic-dark",
            "standard" => "standard-dark",
            _ => ThemeService?.Theme ?? string.Empty,
        };

        private bool value;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ThemeService != null)
            {
                ThemeService.ThemeChanged += OnThemeChanged;

                value = !IsInert && ThemeService.Theme == CurrentDarkTheme;
            }
        }

        private void OnThemeChanged()
        {
            if (ThemeService != null)
            {
                value = !IsInert && ThemeService.Theme == CurrentDarkTheme;
            }

            StateHasChanged();
        }

        void OnChange(bool value)
        {
            ThemeService?.SetTheme(value ? CurrentDarkTheme : CurrentLightTheme);
        }

        private string Icon => value ? "light_mode" : "dark_mode";

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (ThemeService != null)
            {
                ThemeService.ThemeChanged -= OnThemeChanged;
            }

            GC.SuppressFinalize(this);
        }
    }
}