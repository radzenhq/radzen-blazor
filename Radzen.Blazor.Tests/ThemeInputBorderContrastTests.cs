using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ThemeInputBorderContrastTests
    {
        public static IEnumerable<object[]> WcagThemes =>
            new[] { "material", "material-dark", "standard", "standard-dark", "default", "dark", "software", "software-dark", "humanistic", "humanistic-dark" }
                .Select(theme => new object[] { theme });

        [Theory]
        [MemberData(nameof(WcagThemes))]
        public void Wcag_Theme_Input_Border_Meets_Non_Text_Contrast(string theme)
        {
            var cssDir = FindCssDir();
            if (cssDir == null)
            {
                return;
            }

            var variables = LoadVariables(Path.Combine(cssDir, $"{theme}.css"), Path.Combine(cssDir, $"{theme}-wcag.css"));

            var border = LastColor(Resolve(variables, variables["--rz-input-border"]));
            var borderBlockEnd = LastColor(Resolve(variables, variables["--rz-input-border-block-end"]));
            var hover = LastColor(Resolve(variables, variables["--rz-input-hover-border"]));
            var field = LastColor(Resolve(variables, variables["--rz-input-background-color"]));
            var page = LastColor(Resolve(variables, variables["--rz-body-background-color"]));

            Assert.True(Contrast(border, field) >= 3, $"{theme}-wcag input border {border} is {Contrast(border, field):F2}:1 against the field {field}");
            Assert.True(Contrast(border, page) >= 3, $"{theme}-wcag input border {border} is {Contrast(border, page):F2}:1 against the page {page}");
            Assert.True(Contrast(borderBlockEnd, field) >= 3, $"{theme}-wcag input bottom border {borderBlockEnd} is {Contrast(borderBlockEnd, field):F2}:1 against the field {field}");
            Assert.True(Contrast(hover, field) >= 3, $"{theme}-wcag input hover border {hover} is {Contrast(hover, field):F2}:1 against the field {field}");
        }

        private static Dictionary<string, string> LoadVariables(params string[] files)
        {
            var variables = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var file in files)
            {
                var css = File.ReadAllText(file);

                foreach (Match block in Regex.Matches(css, @"(?::root[^{}]*|\.rz-[a-z-]+)\s*\{([^{}]*)\}"))
                {
                    foreach (Match declaration in Regex.Matches(block.Groups[1].Value, @"(--[\w-]+)\s*:\s*([^;]+);?"))
                    {
                        variables[declaration.Groups[1].Value] = declaration.Groups[2].Value.Trim();
                    }
                }
            }

            return variables;
        }

        private static string Resolve(Dictionary<string, string> variables, string value, int depth = 0)
        {
            if (depth > 20)
            {
                return value;
            }

            return Regex.Replace(value, @"var\((--[\w-]+)(?:\s*,\s*([^)]+))?\)", match =>
                variables.TryGetValue(match.Groups[1].Value, out var inner)
                    ? Resolve(variables, inner, depth + 1)
                    : match.Groups[2].Success ? match.Groups[2].Value : match.Value);
        }

        private static string LastColor(string value)
        {
            var matches = Regex.Matches(value, @"#[0-9a-fA-F]{3,8}\b");
            Assert.True(matches.Count > 0, $"No hex colour in '{value}'");
            return matches[^1].Value;
        }

        private static double Contrast(string a, string b)
        {
            var (la, lb) = (Luminance(a), Luminance(b));
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 3)
            {
                hex = string.Concat(hex.Select(c => new string(c, 2)));
            }

            double Channel(int offset)
            {
                var c = int.Parse(hex.Substring(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(0) + 0.7152 * Channel(2) + 0.0722 * Channel(4);
        }

        private static string FindCssDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Radzen.Blazor", "wwwroot", "css", "default-wcag.css");
                if (File.Exists(candidate)) return Path.GetDirectoryName(candidate);
                dir = dir.Parent;
            }
            return null;
        }
    }
}
