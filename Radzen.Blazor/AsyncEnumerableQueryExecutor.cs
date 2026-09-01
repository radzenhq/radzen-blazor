using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// The built-in <see cref="IAsyncQueryExecutor" />. Executes queries whose provider exposes
    /// <see cref="IAsyncEnumerable{T}" /> (for example Entity Framework Core) asynchronously without
    /// referencing any specific data provider and without requiring service registration.
    /// </summary>
    /// <remarks>
    /// Counting composes <c>GroupBy(x =&gt; 1).Select(g =&gt; g.Count())</c> so the aggregate stays a
    /// sequence the provider can stream asynchronously; providers translate it to a plain COUNT.
    /// Operations are serialized per <see cref="IQueryProvider" /> instance because queries created by
    /// one Entity Framework <c>DbContext</c> share a provider that does not allow concurrent use.
    /// </remarks>
    internal sealed class AsyncEnumerableQueryExecutor : IAsyncQueryExecutor
    {
        internal static readonly AsyncEnumerableQueryExecutor Instance = new();

        static readonly ConditionalWeakTable<IQueryProvider, SemaphoreSlim> providerGates = new();

        /// <inheritdoc />
        public bool IsSupported<T>(IQueryable<T> queryable) => queryable is IAsyncEnumerable<T>;

        /// <inheritdoc />
        public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
        {
            var gate = GateFor(queryable);

            await gate.WaitAsync(cancellationToken);

            try
            {
                var counts = queryable.GroupBy(item => 1).Select(group => group.Count());

                if (counts is IAsyncEnumerable<int> asyncCounts)
                {
                    await foreach (var count in asyncCounts.WithCancellation(cancellationToken))
                    {
                        return count;
                    }

                    return 0;
                }

                return queryable.Count();
            }
            finally
            {
                gate.Release();
            }
        }

        /// <inheritdoc />
        public async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
        {
            var gate = GateFor(queryable);

            await gate.WaitAsync(cancellationToken);

            try
            {
                if (queryable is not IAsyncEnumerable<T> asyncItems)
                {
                    return queryable.ToList();
                }

                var items = new List<T>();

                await foreach (var item in asyncItems.WithCancellation(cancellationToken))
                {
                    items.Add(item);
                }

                return items;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// A provider whose context rejects a second concurrent operation cannot safely retry the same
        /// query synchronously.
        /// </remarks>
        public bool CanFallBackToSynchronous(Exception exception) =>
            (exception is InvalidOperationException or NotSupportedException) && !IsProviderConcurrencyViolation(exception);

        static bool IsProviderConcurrencyViolation(Exception exception) =>
            exception.TargetSite?.DeclaringType is { Name: "ConcurrencyDetector", Namespace: string ns }
                && ns.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal);

        static SemaphoreSlim GateFor<T>(IQueryable<T> queryable) =>
            providerGates.GetValue(queryable.Provider, static _ => new SemaphoreSlim(1, 1));
    }
}
