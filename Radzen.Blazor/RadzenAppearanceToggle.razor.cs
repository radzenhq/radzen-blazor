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
        private ThemeService ThemeService { get; set; }
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
        public string LightTheme { get; set; }

        /// <summary>
        /// Gets or sets the dark theme. Not set by default - the component uses the dark version of the current theme.
        /// </summary>
        [Parameter]
        public string DarkTheme { get; set; }

        private string CurrentLightTheme => LightTheme ?? ThemeService.Theme?.ToLowerInvariant() switch
        {
            "dark" => "default",
            "material-dark" => "material",
            "fluent-dark" => "fluent",
            "material3-dark" => "material3",
            "software-dark" => "software",
            "humanistic-dark" => "humanistic",
            "standard-dark" => "standard",
            _ => ThemeService.Theme,
        };

        private string CurrentDarkTheme => DarkTheme ?? ThemeService.Theme?.ToLowerInvariant() switch
        {
            "default" => "dark",
            "material" => "material-dark",
            "fluent" => "fluent-dark",
            "material3" => "material3-dark",
            "software" => "software-dark",
            "humanistic" => "humanistic-dark",
            "standard" => "standard-dark",
            _ => ThemeService.Theme,
        };

        private bool value;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            ThemeService.ThemeChanged += OnThemeChanged;

            value = ThemeService.Theme != CurrentDarkTheme;
        }

        private void OnThemeChanged()
        {
            value = ThemeService.Theme != CurrentDarkTheme;

            StateHasChanged();
        }

        void OnChange(bool value)
        {
            ThemeService.SetTheme(value ? CurrentLightTheme : CurrentDarkTheme);
        }

        private string Icon => value ? "dark_mode" : "light_mode";

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            ThemeService.ThemeChanged -= OnThemeChanged;
        }
    }
}