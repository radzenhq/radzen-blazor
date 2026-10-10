using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.AddScoped<IAIChatService, AIChatService>();
        services.TryAddSingleton<IConversationStore, InMemoryConversationStore>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IConversationStore"/> that keeps the conversations of <see cref="IAIChatService"/>, replacing the default <see cref="InMemoryConversationStore"/>.
    /// </summary>
    /// <typeparam name="TStore">The store type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The lifetime of the store. Scoped by default so that it can use scoped services such as a DbContext.</param>
    public static IServiceCollection AddConversationStore<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped) where TStore : class, IConversationStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IConversationStore>();
        services.Add(new ServiceDescriptor(typeof(IConversationStore), typeof(TStore), lifetime));

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
        services.TryAddSingleton<IConversationStore, InMemoryConversationStore>();

        return services;
    }
}

