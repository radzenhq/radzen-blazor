using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Executes the count and page-materialization steps of a data-bound Radzen component asynchronously.
    /// </summary>
    /// <remarks>
    /// When a component is bound to an <see cref="IQueryable{T}" /> backed by a provider that supports
    /// asynchronous execution (for example Entity Framework Core), the component would otherwise call the
    /// synchronous <c>Count()</c> / <c>ToList()</c> operators, which block the calling thread on database I/O.
    /// Components detect such queryables automatically through the built-in executor, which recognizes
    /// providers exposing <see cref="IAsyncEnumerable{T}" /> without referencing any specific data provider.
    /// The <c>Radzen.Blazor.DisableAsyncQueryExecution</c> <see cref="AppContext" /> switch turns
    /// asynchronous execution off entirely. When the bound queryable is not supported, components use the
    /// synchronous path unchanged.
    /// </remarks>
    internal interface IAsyncQueryExecutor
    {
        /// <summary>
        /// Determines whether the specified <paramref name="queryable" /> can be executed asynchronously by
        /// this executor (for example, whether its provider is an Entity Framework async query provider).
        /// </summary>
        bool IsSupported<T>(IQueryable<T> queryable);

        /// <summary>
        /// Asynchronously counts the elements of <paramref name="queryable" />.
        /// </summary>
        Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously materializes <paramref name="queryable" /> into a list.
        /// </summary>
        Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether a component may answer <paramref name="exception" /> by running the same query
        /// synchronously instead.
        /// </summary>
        /// <remarks>
        /// The default recognizes conventional query-translation failures. Executor implementations should
        /// override it when the same exception type can also represent a non-retriable failure.
        /// </remarks>
        /// <param name="exception">The exception an awaited query threw.</param>
        bool CanFallBackToSynchronous(Exception exception) =>
            exception is InvalidOperationException or NotSupportedException;
    }
}
