using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace Radzen.EntityFrameworkCore
{
    /// <summary>
    /// An <see cref="IAsyncQueryExecutor" /> that runs the count and page-materialization queries of a
    /// data-bound Radzen component through Entity Framework Core's asynchronous operators.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by the dependency injection container through AddRadzenQueryableEntityFrameworkAdapter.")]
    internal sealed class RadzenEntityFrameworkQueryExecutor : IAsyncQueryExecutor
    {
        /// <summary>
        /// Returns <c>true</c> when the queryable is provided by Entity Framework Core, i.e. its provider
        /// implements <see cref="IAsyncQueryProvider" />.
        /// </summary>
        public bool IsSupported<T>(IQueryable<T> queryable) => queryable.Provider is IAsyncQueryProvider;

        /// <inheritdoc />
        public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            => queryable.CountAsync(cancellationToken);

        /// <inheritdoc />
        public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            => queryable.ToListAsync(cancellationToken);

        /// <inheritdoc />
        /// <remarks>
        /// A busy <c>DbContext</c> cannot safely retry the same query synchronously.
        /// </remarks>
        public bool CanFallBackToSynchronous(Exception exception) =>
            (exception is InvalidOperationException or NotSupportedException) && !IsContextBusy(exception);

        static bool IsContextBusy(Exception exception) =>
#pragma warning disable EF1001
            exception.TargetSite?.DeclaringType == typeof(ConcurrencyDetector);
#pragma warning restore EF1001
    }
}
