using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Radzen
{
    /// <summary>
    /// Options for changing the theme.
    /// </summary>
    public class ThemeOptions
    {
        /// <summary>
        /// Specifies the theme.
        /// </summary>
        public string Theme { get; set; }
        /// <summary>
        /// Specifies if the theme colors should meet WCAG contrast requirements.
        /// </summary>
        public bool? Wcag { get; set; }
        /// <summary>
        /// Specifies if the theme should be right-to-left.
        /// </summary>
        public bool? RightToLeft { get; set; }
        /// <summary>
        /// Specifies if the theme change should trigger the <see cref="ThemeService.ThemeChanged" /> event.
        /// </summary>
        public bool TriggerChange { get; set; }
    }

    /// <summary>
    /// Theme definition.
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// Specifies the user-friendly theme name e.g. Material3.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Specifies the theme value e.g. material3.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Specifies the primary color.
        /// </summary>
        public string Primary { get; set; }
        /// <summary>
        /// Specifies the secondary color.
        /// </summary>
        public string Secondary { get; set; }
        /// <summary>
        /// Specifies the base color.
        /// </summary>
        public string Base { get; set; }
        /// <summary>
        /// Specifies the content color.
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Specifies the title text color.
        /// </summary>
        public string TitleText { get; set; }
        /// <summary>
        /// Specifies the content text color.
        /// </summary>
        public string ContentText { get; set; }
        /// <summary>
        /// Specifies the selection color.
        /// </summary>
        public string Selection { get; set; }
        /// <summary>
        /// Specifies the selection text color.
        /// </summary>
        public string SelectionText { get; set; }
        /// <summary>
        /// Specifies the button radius.
        /// </summary>
        public string ButtonRadius { get; set; }
        /// <summary>
        /// Specifies the card radius.
        /// </summary>
        public string CardRadius { get; set; }
        /// <summary>
        /// Specifies the series A color.
        /// </summary>
        public string SeriesA { get; set; }
        /// <summary>
        /// Specifies the series B color.
        /// </summary>
        public string SeriesB { get; set; }
        /// <summary>
        /// Specifies the series C color.
        /// </summary>
        public string SeriesC { get; set; }
        /// <summary>
        /// Specifies if the theme is premium.
        /// </summary>
        public bool Premium { get; set; }
    }

    /// <summary>
    /// Predefined themes.
    /// </summary>
    public static class Themes
    {
        /// <summary>
        /// Predefined themes.
        /// </summary>
        public static readonly Theme[] All = [
            new Theme {
                Text = "Material 3",
                Value = "material3",
                Premium = true,
                Primary = "#3481e5",
                Secondary = "#5b6471",
                Base = "#ffffff",
                Selection = "rgba(0, 107, 255, 0.13)",
                SelectionText = "#1b1d20",
                Content = "#f8f7ff",
                TitleText = "#1b1d20",
                ContentText = "#5b6471",
                ButtonRadius = "4",
                CardRadius = "8",
                SeriesA = "#75abf0",
                SeriesB = "#9c75f0",
                SeriesC = "#f075e8"
            },
            new Theme {
                Text = "Material 3 Dark",
                Value = "material3-dark",
                Premium = true,
                Primary = "#94c1ff",
                Secondary = "#c2cbdc",
                Base = "#121418",
                Selection = "rgba(144, 181, 255, 0.28)",
                SelectionText = "#c2cbdc",
                Content = "#1b1d20",
                TitleText = "#e0e0e9",
                ContentText = "#e0e0e9",
                ButtonRadius = "4",
                CardRadius = "8",
                SeriesA = "#3d71b8",
                SeriesB = "#663db8",
                SeriesC = "#b83dae"
            },
            new Theme {
                Text = "Fluent",
                Value = "fluent",
                Premium = true,
                Primary = "#0078d4",
                Secondary = "#8a8886",
                Base = "#f5f5f5",
                Selection = "#0078d4",
                SelectionText = "#ffffff",
                Content = "white",
                TitleText = "black",
                ContentText = "#605e5c",
                ButtonRadius = "1",
                CardRadius = "2",
                SeriesA = "#0078d4",
                SeriesB = "#b4a0ff",
                SeriesC = "#d83b01"
            },
            new Theme {
                Text = "Fluent Dark",
                Value = "fluent-dark",
                Premium = true,
                Primary = "#479ef5",
                Secondary = "#5c5c5c",
                Base = "#292929",
                Selection = "#479ef5",
                SelectionText = "#0a0a0a",
                Content = "#333333",
                TitleText = "#ffffff",
                ContentText = "#d6d6d6",
                ButtonRadius = "1",
                CardRadius = "2",
                SeriesA = "#0078d4",
                SeriesB = "#b4a0ff",
                SeriesC = "#d83b01"
            },
            new Theme {
                Text = "Material",
                Value = "material",
                Primary = "#4340d2",
                Secondary = "#e91e63",
                Base = "#f5f5f5",
                Selection = "rgba(67, 64, 210, 0.12)",
                SelectionText = "#4340d2",
                Content = "#ffffff",
                TitleText = "#212121",
                ContentText = "#bdbdbd",
                ButtonRadius = "2",
                CardRadius = "4",
                SeriesA = "#3700b3",
                SeriesB = "#ba68c8",
                SeriesC = "#f06292"
            },
            new Theme {
                Text = "Material Dark",
                Value = "material-dark",
                Primary = "#bb86fc",
                Secondary = "#01a299",
                Base = "#121212",
                Selection = "rgba(187, 134, 252, 0.12)",
                SelectionText = "#bb86fc",
                Content = "#252525",
                TitleText = "#ffffff",
                ContentText = "#a0a0a0",
                ButtonRadius = "2",
                CardRadius = "4",
                SeriesA = "#3700b3",
                SeriesB = "#ba68c8",
                SeriesC = "#f06292"
            },
            new Theme {
                Text = "Standard",
                Value = "standard",
                Primary = "#1151f3",
                Secondary = "rgba(17, 81, 243, 0.16)",
                Base = "#f4f5f9",
                Selection = "rgba(114, 152, 248, 0.16)",
                SelectionText = "#1151f3",
                Content = "#ffffff",
                TitleText = "#262526",
                ContentText = "#afafb2",
                ButtonRadius = "2",
                CardRadius = "4",
                SeriesA = "#376df5",
                SeriesB = "#64dfdf",
                SeriesC = "#f68769"
            },
            new Theme {
                Text = "Standard Dark",
                Value = "standard-dark",
                Primary = "#3871ff",
                Secondary = "#2a3c68",
                Base = "#19191a",
                Selection = "rgba(56, 113, 255, 0.2)",
                SelectionText = "#88aaff",
                Content = "#242527",
                TitleText = "#ffffff",
                ContentText = "#eaebec",
                ButtonRadius = "2",
                CardRadius = "4",
                SeriesA = "#376df5",
                SeriesB = "#64dfdf",
                SeriesC = "#f68769"
            },
            new Theme {
                Text = "Default",
                Value = "default",
                Primary = "#ff6d41",
                Secondary = "#35a0d7",
                Base = "#f6f7fa",
                Selection = "#3193c6",
                SelectionText = "#ffffff",
                Content = "#ffffff",
                TitleText = "#28363c",
                ContentText = "#95a4a8",
                ButtonRadius = "2",
                CardRadius = "4",
                SeriesA = "#0479cc",
                SeriesB = "#68d5c8",
                SeriesC = "#ff6d41"
            },
            new Theme {
                Text = "Dark",
                Value="dark",
                Primary = "#ff6d41",
                Secondary = "#35a0d7",
                Base = "#28363c",
                Selection = "#3692c4",
                SelectionText = "#ffffff",
                Content = "#38474e",
                TitleText = "#ffffff",
                ContentText = "#a8b4b8",
                ButtonRadius = "2",
                CardRadius = "4",
                SeriesA = "#0479cc",
                SeriesB = "#68d5c8",
                SeriesC = "#ff6d41"
            },
            new Theme {
                Text = "Humanistic",
                Value = "humanistic",
                Primary = "#d64d42",
                Secondary = "#3ba5fc",
                Base = "#f3f5f7",
                Selection = "#3698e8",
                SelectionText = "#ffffff",
                Content = "#ffffff",
                TitleText = "#2b3a50",
                ContentText = "#7293b6",
                ButtonRadius = "0",
                CardRadius = "0",
                SeriesA = "#376df5",
                SeriesB = "#64dfdf",
                SeriesC = "#f68769"
            },
            new Theme {
                Text = "Humanistic Dark",
                Value = "humanistic-dark",
                Primary = "#d64d42",
                Secondary = "#3ba5fc",
                Base = "#2b3a50",
                Selection = "#3698e8",
                SelectionText = "#ffffff",
                Content = "#30445f",
                TitleText = "#ffffff",
                ContentText = "#d9e1ea",
                ButtonRadius = "0",
                CardRadius = "0",
                SeriesA = "#376df5",
                SeriesB = "#64dfdf",
                SeriesC = "#f68769"
            },
            new Theme {
                Text = "Software",
                Value = "software",
                Primary = "#598087",
                Secondary = "#80a4ab",
                Base = "#f6f7fa",
                Selection = "#76979d",
                SelectionText = "#ffffff",
                Content = "#ffffff",
                TitleText = "#28363c",
                ContentText = "#95a4a8",
                ButtonRadius = "2",
                CardRadius = "4",
                SeriesA = "#376df5",
                SeriesB = "#64dfdf",
                SeriesC = "#f68769"
            },
            new Theme {
                Text = "Software Dark",
                Value = "software-dark",
                Primary = "#598087",
                Secondary = "#80a4ab",
                Base = "#28363c",
                Selection = "#76979d",
                SelectionText = "#ffffff",
                Content = "#3a474d",
                TitleText = "#ffffff",
                ContentText = "#f5f8f9",
                ButtonRadius = "2",
                CardRadius = "4",
                SeriesA = "#376df5",
                SeriesB = "#64dfdf",
                SeriesC = "#f68769"
            }
        ];

        /// <summary>
        /// Free themes.
        /// </summary>
        public static IEnumerable<Theme> Free => All.Where(theme => !theme.Premium);

        /// <summary>
        /// Premium themes.
        /// </summary>
        public static IEnumerable<Theme> Premium => All.Where(theme => theme.Premium);
    }

    /// <summary>
    /// Service for theme registration and management.
    /// </summary>
    public class ThemeService
    {
        /// <summary>
        /// Gets the current theme.
        /// </summary>
        public string Theme { get; private set; }

        /// <summary>
        /// Specify if the theme colors should meet WCAG contrast requirements.
        /// </summary>
        public bool? Wcag { get; private set; }

        /// <summary>
        /// Specify if the theme should be right-to-left.
        /// </summary>
        public bool? RightToLeft { get; private set; }

        /// <summary>
        /// Raised when the theme changes.
        /// </summary>
        public event Action ThemeChanged;

        /// <summary>
        /// Changes the current theme.
        /// </summary>
        public void SetTheme(string theme, bool triggerChange = true)
        {
            SetTheme(new ThemeOptions
            {
                Theme = theme,
                Wcag = Wcag,
                RightToLeft = RightToLeft,
                TriggerChange = triggerChange
            });
        }

        /// <summary>
        /// Changes the current theme with additional options.
        /// </summary>
        public void SetTheme(ThemeOptions options)
        {
            var requiresChange = false;

            if (Theme != options.Theme)
            {
                Theme = options.Theme;
                requiresChange = true;
            }

            if (Wcag != options.Wcag)
            {
                Wcag = options.Wcag;
                requiresChange = true;
            }

            if (RightToLeft != options.RightToLeft)
            {
                RightToLeft = options.RightToLeft;
                requiresChange = true;
            }

            if (requiresChange && options.TriggerChange)
            {
                ThemeChanged?.Invoke();
            }
        }

        /// <summary>
        /// Enables or disables WCAG contrast requirements.
        /// </summary>
        public void SetWcag(bool wcag)
        {
            SetTheme(new ThemeOptions
            {
                Theme = Theme,
                Wcag = wcag,
                RightToLeft = RightToLeft,
                TriggerChange = true
            });
        }

        /// <summary>
        /// Specifies if the theme should be right-to-left.
        /// </summary>
        public void SetRightToLeft(bool rightToLeft)
        {
            SetTheme(new ThemeOptions
            {
                Theme = Theme,
                Wcag = Wcag,
                RightToLeft = rightToLeft,
                TriggerChange = true
            });
        }
    }
}