using Microsoft.Extensions.DependencyInjection;

namespace Radzen;

/// <summary>
/// Class with IServiceCollection extensions methods.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Radzen Blazor components services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRadzenComponents(this IServiceCollection services)
    {
        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<ContextMenuService>();
        services.AddScoped<ThemeService>();
        services.AddAIChatService();

        return services;
    }
}

