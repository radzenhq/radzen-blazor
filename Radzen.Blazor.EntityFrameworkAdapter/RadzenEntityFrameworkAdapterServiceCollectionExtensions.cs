using Microsoft.Extensions.DependencyInjection;
using Radzen.EntityFrameworkCore;

namespace Radzen
{
    /// <summary>
    /// Extension methods that register the Entity Framework Core adapter for data-bound Radzen components.
    /// </summary>
    public static class RadzenEntityFrameworkAdapterServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="IAsyncQueryExecutor" /> so that Radzen components bound to an Entity
        /// Framework Core <see cref="System.Linq.IQueryable{T}" /> count and page their data with awaited
        /// asynchronous queries instead of blocking the calling thread on <c>Count()</c> / <c>ToList()</c>.
        /// </summary>
        /// <param name="services">The service collection to add the adapter to.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddRadzenQueryableEntityFrameworkAdapter(this IServiceCollection services)
        {
            services.AddSingleton<IAsyncQueryExecutor, RadzenEntityFrameworkQueryExecutor>();
            return services;
        }
    }
}
