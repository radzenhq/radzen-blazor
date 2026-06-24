using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Radzen;

namespace RadzenBlazorDemos.Services
{
    /// <summary>
    /// Supplies the built-in Radzen translations to the demo from an embedded JSON file.
    /// Used so the live localization demo can switch culture in-place, including on Blazor
    /// WebAssembly where satellite resource assemblies are only downloaded for the startup culture.
    /// Returns null for any culture/key it does not cover, falling back to the built-in English resx.
    /// </summary>
    public class DemoLocalizer : ILocalizer
    {
        private static readonly Dictionary<string, Dictionary<string, string>> translations = Load();

        private static Dictionary<string, Dictionary<string, string>> Load()
        {
            var assembly = typeof(DemoLocalizer).Assembly;
            using var stream = assembly.GetManifestResourceStream("RadzenBlazorDemos.Localization.RadzenTranslations.json");

            if (stream == null)
            {
                return new Dictionary<string, Dictionary<string, string>>();
            }

            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)
                ?? new Dictionary<string, Dictionary<string, string>>();
        }

        public string Get(string key, CultureInfo culture)
        {
            return translations.TryGetValue(culture.TwoLetterISOLanguageName, out var strings)
                && strings.TryGetValue(key, out var value)
                ? value
                : null;
        }
    }
}
