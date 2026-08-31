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
    /// Registering an <see cref="IAsyncQueryExecutor" /> in the service collection lets the component
    /// <c>await</c> real asynchronous queries instead. Radzen.Blazor does not reference any specific data
    /// provider; an adapter package supplies the implementation (see <c>AddRadzenQueryableEntityFrameworkAdapter</c>).
    /// When no executor is registered, or the bound queryable is not supported, components fall back to the
    /// synchronous path unchanged.
    /// </remarks>
    public interface IAsyncQueryExecutor
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
        /// The default recognizes conventional query-translation failures. Provider adapters should
        /// override it when the same exception type can also represent a non-retriable failure.
        /// </remarks>
        /// <param name="exception">The exception an awaited query threw.</param>
        bool CanFallBackToSynchronous(Exception exception) =>
            exception is InvalidOperationException or NotSupportedException;
    }
}
