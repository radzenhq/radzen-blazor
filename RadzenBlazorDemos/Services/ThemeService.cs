using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Components;

namespace RadzenBlazorDemos
{
    public class ThemeService
    {
        public class Theme
        {
            public string Text { get; set; }
            public string Value { get; set; }
            public string Primary { get; set; }
            public string Secondary { get; set; }
            public string Base { get; set; }
            public string Content { get; set; }
            public string TitleText { get; set; }
            public string ContentText { get; set; }
            public string Selection { get; set; }
            public string SelectionText { get; set; }
            public string ButtonRadius { get; set; }
            public string CardRadius { get; set; }
            public string SeriesA { get; set; }
            public string SeriesB { get; set; }
            public string SeriesC { get; set; }
            public bool Premium { get; set; }
        }
        public static readonly Theme[] Themes = new []
        {
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
                Text = "Material Dark",
                Value = "material-dark",
                Premium = true,
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
                SeriesA = "#0479cc",
                SeriesB = "#68d5c8",
                SeriesC = "#ff6d41"
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
            }
        };

        public const string DefaultTheme = "material3";
        public const string QueryParameter = "theme";
        public const string WCAGQueryParameter = "wcag";
        public const string RTLQueryParameter = "rtl";

        public string CurrentTheme { get; set; } = DefaultTheme;
        public bool WCAG { get; set; }
        public bool RTL { get; set; }

        public void Initialize(NavigationManager navigationManager)
        {
            var uri = new Uri(navigationManager.ToAbsoluteUri(navigationManager.Uri).ToString());
            var query = HttpUtility.ParseQueryString(uri.Query);
            var value = query.Get(QueryParameter);

            if (Themes.Any(theme => theme.Value == value))
            {
                CurrentTheme = value;
            }

            WCAG = bool.Parse(query.Get(WCAGQueryParameter) ?? "false");
            RTL = bool.Parse(query.Get(RTLQueryParameter) ?? "false");
        }

        public void Change(NavigationManager navigationManager, string theme, bool wcag, bool rtl)
        {
            var url = navigationManager.GetUriWithQueryParameters(navigationManager.Uri,
                new Dictionary<string, object>() { { QueryParameter, theme }, { WCAGQueryParameter, $"{wcag}".ToLowerInvariant() }, { RTLQueryParameter, $"{rtl}".ToLowerInvariant() } });

            navigationManager.NavigateTo(url, true);
        }
    }
}