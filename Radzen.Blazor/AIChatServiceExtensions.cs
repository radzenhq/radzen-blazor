using System;
using Microsoft.Extensions.DependencyInjection;

namespace Radzen;

/// <summary>
/// Extension methods for configuring AIChatService in the dependency injection container.
/// </summary>
public static class AIChatServiceExtensions
{
    /// <summary>
    /// Adds the AIChatService to the service collection with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The action to configure the AIChatService options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAIChatService(this IServiceCollection services, Action<AIChatServiceOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        services.Configure(configureOptions);
        services.AddScoped<IAIChatService, AIChatService>();

        return services;
    }

    /// <summary>
    /// Adds the AIChatService to the service collection with default options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAIChatService(this IServiceCollection services)
    {
        services.AddOptions<AIChatServiceOptions>();
        services.AddScoped<IAIChatService, AIChatService>();

        return services;
    }
}

