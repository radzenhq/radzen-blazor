using Microsoft.Extensions.DependencyInjection;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class that provides Extension Methods for <see cref="IServiceCollection"/>
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add all Radzen Services to the Dependency Injection Container using their Instaces.
        /// Adds <see cref="ITooltipService"/>, <see cref="INotificationService"/>, <see cref="IDialogService"/> and <see cref="IContextMenuService"/>
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRadzenServices(this IServiceCollection services) => services
            .AddScoped<ITooltipService, TooltipService>()
            .AddScoped<INotificationService, NotificationService>()
            .AddScoped<IDialogService, DialogService>()
            .AddScoped<IContextMenuService, ContextMenuService>()
            ;

    }
}
