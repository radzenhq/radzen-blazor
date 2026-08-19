using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Radzen;

internal class Localizer(ILocalizer? custom)
{
    internal static readonly Localizer Default = new(null);

    private static Assembly? appAssembly = Assembly.GetEntryAssembly();
    private static ResourceManager? appResources;
    private static readonly ConcurrentDictionary<string, ResourceSet[]> appResourceSets = new();

    internal static Assembly? AppAssembly
    {
        get => appAssembly;
        set
        {
            appAssembly = value;
            appResources = null;
            appResourceSets.Clear();
        }
    }

    private static ResourceManager? AppResources
    {
        get
        {
            var assembly = appAssembly;

            if (assembly == null || assembly == typeof(Blazor.RadzenStrings).Assembly)
            {
                return null;
            }

            return appResources ??= new ResourceManager("Radzen.Blazor.RadzenStrings", assembly);
        }
    }

    private readonly ILocalizer? custom = custom;
    private readonly ResourceManager resources = Blazor.RadzenStrings.ResourceManager;

    public string Get(string key, CultureInfo culture) => custom?.Get(key, culture) ?? GetAppString(key, culture) ?? resources.GetString(key, culture) ?? key;

    private static string? GetAppString(string key, CultureInfo culture)
    {
        if (AppResources == null)
        {
            return null;
        }

        var sets = appResourceSets.GetOrAdd(culture.Name, _ => GetAppResourceSets(culture));

        foreach (var set in sets)
        {
            var value = set.GetString(key);

            if (value != null)
            {
                return value;
            }
        }

        return null;
    }

    private static ResourceSet[] GetAppResourceSets(CultureInfo culture)
    {
        var resources = AppResources;

        if (resources == null)
        {
            return Array.Empty<ResourceSet>();
        }

        var sets = new List<ResourceSet>();

        for (var current = culture; ; current = current.Parent)
        {
            try
            {
                var set = resources.GetResourceSet(current, true, false);

                if (set != null)
                {
                    sets.Add(set);
                }
            }
            catch (MissingManifestResourceException)
            {
            }
            catch (MissingSatelliteAssemblyException)
            {
            }

            if (Equals(current, CultureInfo.InvariantCulture))
            {
                break;
            }
        }

        return sets.ToArray();
    }
}
