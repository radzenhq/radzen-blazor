using System;
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
        }
        public static readonly Theme[] Themes = new [] 
        {
            new Theme { Text = "Default Theme", Value = "default"},
            new Theme { Text = "Dark Theme", Value="dark" },
            new Theme { Text = "Software Theme", Value = "software"},
            new Theme { Text = "Humanistic Theme", Value = "humanistic" },
            new Theme { Text = "Standard Theme", Value = "standard" }
        };

        public const string DefaultTheme = "standard";
        public const string QueryParameter = "theme";

        public string CurrentTheme { get; set; } = DefaultTheme;

        public void Initialize(NavigationManager navigationManager)
        {
            var uri = new Uri(navigationManager.ToAbsoluteUri(navigationManager.Uri).ToString());
            var query = HttpUtility.ParseQueryString(uri.Query);
            var value = query.Get(QueryParameter);

            if (Themes.Any(theme => theme.Value == value))
            {
                CurrentTheme = value;
            }
        }

        public void Change(NavigationManager navigationManager, string theme)
        {
            var url = navigationManager.GetUriWithQueryParameter(QueryParameter, theme);

            navigationManager.NavigateTo(url, true);
        }
    }
}