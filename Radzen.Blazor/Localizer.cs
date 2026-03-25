using System.Globalization;
using System.Resources;

namespace Radzen;

internal class Localizer(ILocalizer? custom)
{
    internal static readonly Localizer Default = new(null);

    private readonly ILocalizer? custom = custom;
    private readonly ResourceManager resources = Blazor.RadzenStrings.ResourceManager;

    public string Get(string key, CultureInfo culture) => custom?.Get(key, culture) ?? resources.GetString(key, culture) ?? key;
}
