using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Coordinates asynchronous query work for one data-bound component.
    /// </summary>
    /// <remarks>
    /// A coordinator is created only after its owner selects a supported query. Loads publish through
    /// generation leases because cancellation is advisory; superseding waits for the whole load and its
    /// FIFO, fault-isolated provider tail. Virtual request sources are cancelled eagerly but disposed only
    /// by the request lease after provider work exits.
    /// </remarks>
    internal sealed class AsyncQueryCoordinator : IDisposable
    {
        private const long NoLoad = 0;

        private readonly Action loadCompleted;
        private readonly IAsyncQueryExecutor executor;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
            Justification = "The load lease disposes its source only after the load body exits; coordinator disposal only cancels it.")]
        private CancellationTokenSource? loadCancellation;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
            Justification = "The virtual request lease disposes its source only after the request exits; coordinator disposal only cancels it.")]
        private CancellationTokenSource? virtualRequest;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
            Justification = "The queued lookup disposes its source only after provider work exits; coordinator disposal only cancels it.")]
        private CancellationTokenSource? lookupCancellation;
        private long loadOwner = NoLoad;
        private long loadSequence;
        private long lookupSequence;
        private int awaitingProviderTurn;
        private Task loadCompletion = Task.CompletedTask;
        private Task providerTail = Task.CompletedTask;
        private bool disposed;

        internal AsyncQueryCoordinator(Action loadCompleted, IAsyncQueryExecutor executor)
        {
            this.loadCompleted = loadCompleted;
            this.executor = executor;
        }

        /// <summary>
        /// A load's generation claim and the cancellation source it owns until the load body exits.
        /// </summary>
        internal readonly struct LoadLease
        {
            private readonly AsyncQueryCoordinator coordinator;
            private readonly CancellationTokenSource source;

            internal LoadLease(AsyncQueryCoordinator coordinator, long generation, CancellationTokenSource source)
            {
                this.coordinator = coordinator;
                Generation = generation;
                this.source = source;
                Token = source.Token;
            }

            internal long Generation { get; }

            internal CancellationToken Token { get; }

            internal bool IsCurrent => coordinator.Owns(this);

            internal CancellationTokenSource Source => source;

            internal Task<int> CountAsync<T>(IQueryable<T> query) =>
                coordinator.CountAsync(query, Token);

            internal Task<List<T>> ToListAsync<T>(IQueryable<T> query) =>
                coordinator.ToListAsync(query, Token);

            internal Task<(int Count, List<T> Items)> CountAndPageAsync<T>(IQueryable<T> source,
                IQueryable<T> page, bool pageIsSubset) =>
                coordinator.CountAndPageAsync(source, page, pageIsSubset, Token);

            internal void ThrowIfSuperseded()
            {
                if (!IsCurrent)
                {
                    throw new OperationCanceledException(Token);
                }
            }
        }

        internal readonly record struct VirtualQueryResult<T>(T Value, bool HasValue,
            bool RequiresSynchronousFallback)
        {
            internal static VirtualQueryResult<T> Applied(T value) => new(value, true, false);
            internal static VirtualQueryResult<T> SynchronousFallback() => new(default!, false, true);
        }

        /// <summary>
        /// A linked virtualization request. Disposing it after the request exits clears the coordinator's
        /// current handle when appropriate and then disposes this request's source.
        /// </summary>
        internal readonly struct VirtualRequestLease : IDisposable
        {
            private readonly AsyncQueryCoordinator? coordinator;
            private readonly CancellationTokenSource? source;
            private readonly CancellationToken requestToken;

            internal VirtualRequestLease(AsyncQueryCoordinator coordinator, CancellationTokenSource source,
                CancellationToken requestToken)
            {
                this.coordinator = coordinator;
                this.source = source;
                this.requestToken = requestToken;
                Token = source.Token;
            }

            internal CancellationToken Token { get; }

            internal Task<VirtualQueryResult<(int Count, List<T> Items)>> CountAndPageAsync<T>(
                IQueryable<T> source, IQueryable<T> page, bool pageIsSubset) =>
                ExecuteAsync((Source: source, Page: page, PageIsSubset: pageIsSubset),
                    static (coordinator, state, token) => coordinator.CountAndPageWithFallbackAsync(
                        state.Source, state.Page, state.PageIsSubset, token));

            internal Task<VirtualQueryResult<List<T>>> ToListAsync<T>(IQueryable<T> query) =>
                ExecuteAsync(query,
                    static (coordinator, state, token) => coordinator.ToListAsync(state, token));

            private async Task<VirtualQueryResult<TResult>> ExecuteAsync<TState, TResult>(TState state,
                Func<AsyncQueryCoordinator, TState, CancellationToken, Task<TResult>> query)
            {
                try
                {
                    TResult result = await query(coordinator!, state, Token);
                    return VirtualQueryResult<TResult>.Applied(result);
                }
                catch (OperationCanceledException) when (Token.IsCancellationRequested)
                {
                    requestToken.ThrowIfCancellationRequested();
                    return default;
                }
                catch (Exception exception) when (coordinator!.executor.CanFallBackToSynchronous(exception))
                {
                    return VirtualQueryResult<TResult>.SynchronousFallback();
                }
            }

            public void Dispose()
            {
                if (source != null)
                {
                    coordinator!.EndVirtualRequest(source);
                }
            }
        }

        /// <summary>Whether view getters must avoid the source while async work owns the provider.</summary>
        internal bool LoadPending => loadOwner != NoLoad || awaitingProviderTurn > 0;

        private bool Owns(LoadLease lease) => loadOwner == lease.Generation;

        /// <summary>
        /// Tracks a virtualization request with a token linked to the one supplied by Virtualize.
        /// </summary>
        internal VirtualRequestLease TrackVirtualRequest(CancellationToken request)
        {
            CancellationTokenSource? previous = virtualRequest;
            CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(request);

            virtualRequest = linked;
            previous?.Cancel();

            return new VirtualRequestLease(this, linked, request);
        }

        private void EndVirtualRequest(CancellationTokenSource request)
        {
            if (ReferenceEquals(virtualRequest, request))
            {
                virtualRequest = null;
            }

            request.Dispose();
        }

        private void CancelVirtualRequest()
        {
            virtualRequest?.Cancel();
        }

        internal void CancelLookup()
        {
            lookupSequence++;
            lookupCancellation?.Cancel();
        }

        /// <summary>
        /// Cancels the current load and virtual request, then waits until the superseded load has no more
        /// provider work to append and the resulting queue tail has settled.
        /// </summary>
        internal async Task SupersedeAsyncLoad()
        {
            Task outstanding = providerTail;
            Task superseded = loadCompletion;

            CancelLoad();
            CancelVirtualRequest();

            if (outstanding.IsCompleted && superseded.IsCompleted)
            {
                return;
            }

            awaitingProviderTurn++;

            try
            {
                await superseded;
                await providerTail;
            }
            finally
            {
                awaitingProviderTurn--;
            }
        }

        private void CancelLoad()
        {
            CancellationTokenSource? previous = loadCancellation;

            loadCancellation = null;
            loadOwner = NoLoad;

            previous?.Cancel();
        }

        private LoadLease BeginLoad()
        {
            CancellationTokenSource? previous = loadCancellation;
            CancellationTokenSource current = new();
            long generation = ++loadSequence;

            // Cancellation callbacks may start another load, so publish the new owner first.
            loadCancellation = current;
            loadOwner = generation;

            previous?.Cancel();

            return new LoadLease(this, generation, current);
        }

        private void EndLoad(LoadLease lease)
        {
            if (!Owns(lease))
            {
                return;
            }

            loadOwner = NoLoad;

            loadCompleted();
        }

        /// <summary>
        /// Runs a load under a generation lease. Superseded cancellation is swallowed, provider-approved
        /// synchronous fallback is applied only by the owning lease, and the load-owned source is disposed
        /// only after the body has exited.
        /// </summary>
        internal async Task<bool> RunLoadAsync(Func<LoadLease, Task> load,
            Action? synchronousFallback = null)
        {
            LoadLease lease = BeginLoad();
            bool applied = false;
            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            loadCompletion = completion.Task;

            try
            {
                await load(lease);
                applied = Owns(lease);
            }
            catch (OperationCanceledException) when (lease.Token.IsCancellationRequested)
            {
            }
            catch (Exception exception) when (executor.CanFallBackToSynchronous(exception))
            {
                if (Owns(lease))
                {
                    synchronousFallback?.Invoke();
                    applied = true;
                }
            }
            finally
            {
                EndLoad(lease);

                if (ReferenceEquals(loadCancellation, lease.Source))
                {
                    loadCancellation = null;
                }

                lease.Source.Dispose();

                if (ReferenceEquals(loadCompletion, completion.Task))
                {
                    loadCompletion = Task.CompletedTask;
                }

                completion.SetResult();
            }

            return applied;
        }

        /// <summary>
        /// Queues an uncommon provider callback after prior work. The keyboard lookup uses this because it
        /// must re-check request ownership only once its turn arrives.
        /// </summary>
        internal Task<T> QueueAsync<T>(Func<IAsyncQueryExecutor, Task<T>> query)
        {
            Task turn = providerTail;
            Task<T> current = turn.IsCompleted ? Invoke(query) : RunAfter(turn, executor, query);

            TrackTail(current);

            return current;
        }

        internal Task<T?> LookupAsync<T>(VirtualItems<T> items, long queryGeneration,
            IQueryable<T> view, int index)
        {
            long request = ++lookupSequence;

            return QueueAsync(async executor =>
            {
                if (!OwnsLookup())
                {
                    return default;
                }

                using var cancellation = new CancellationTokenSource();
                lookupCancellation = cancellation;

                try
                {
                    T? item;

                    try
                    {
                        item = (await executor.ToListAsync(view.Skip(index).Take(1), cancellation.Token))
                            .FirstOrDefault();
                    }
                    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                    {
                        return default;
                    }
                    catch (Exception exception) when (executor.CanFallBackToSynchronous(exception))
                    {
                        if (!OwnsLookup())
                        {
                            return default;
                        }

                        item = view.Skip(index).FirstOrDefault();
                    }

                    return OwnsLookup() ? item : default;
                }
                finally
                {
                    if (ReferenceEquals(lookupCancellation, cancellation))
                    {
                        lookupCancellation = null;
                    }
                }

                bool OwnsLookup() => !disposed && request == lookupSequence
                    && queryGeneration == items.QueryGeneration;
            });
        }

        /// <summary>Counts a query in provider order without a capturing delegate.</summary>
        internal Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
            QueueAsync((Query: query, Token: cancellationToken),
                static (executor, state) => executor.CountAsync(state.Query, state.Token));

        /// <summary>Materializes a query in provider order without a capturing delegate.</summary>
        internal Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
            QueueAsync((Query: query, Token: cancellationToken),
                static (executor, state) => executor.ToListAsync(state.Query, state.Token));

        /// <summary>
        /// Materializes a page and, when it is a subset, counts its source in the same provider turn.
        /// </summary>
        internal Task<(int Count, List<T> Items)> CountAndPageAsync<T>(IQueryable<T> source,
            IQueryable<T> page, bool pageIsSubset, CancellationToken cancellationToken)
            => CountAndPageAsync(source, page, pageIsSubset, cancellationToken, false);

        private Task<(int Count, List<T> Items)> CountAndPageWithFallbackAsync<T>(IQueryable<T> source,
            IQueryable<T> page, bool pageIsSubset, CancellationToken cancellationToken)
            => CountAndPageAsync(source, page, pageIsSubset, cancellationToken, true);

        private Task<(int Count, List<T> Items)> CountAndPageAsync<T>(IQueryable<T> source,
            IQueryable<T> page, bool pageIsSubset, CancellationToken cancellationToken,
            bool synchronousFallback)
            => QueueAsync((Source: source, Page: page, PageIsSubset: pageIsSubset,
                Token: cancellationToken, SynchronousFallback: synchronousFallback),
                static (executor, state) => RunCountAndPage(executor, state.Source, state.Page,
                    state.PageIsSubset, state.Token, state.SynchronousFallback));

        private Task<TResult> QueueAsync<TState, TResult>(TState state,
            Func<IAsyncQueryExecutor, TState, Task<TResult>> query)
        {
            Task turn = providerTail;
            Task<TResult> current = turn.IsCompleted
                ? Invoke(executor, state, query)
                : RunAfter(turn, executor, state, query);

            TrackTail(current);

            return current;
        }

        private void TrackTail(Task current)
        {
            providerTail = current.IsCompleted ? Task.CompletedTask : Settle(current);
        }

        private Task<T> Invoke<T>(Func<IAsyncQueryExecutor, Task<T>> query) => Invoke(executor, query);

        private static Task<T> Invoke<T>(IAsyncQueryExecutor executor,
            Func<IAsyncQueryExecutor, Task<T>> query)
        {
            try
            {
                return query(executor);
            }
            catch (Exception exception)
            {
                return Task.FromException<T>(exception);
            }
        }

        private static Task<TResult> Invoke<TState, TResult>(IAsyncQueryExecutor executor, TState state,
            Func<IAsyncQueryExecutor, TState, Task<TResult>> query)
        {
            try
            {
                return query(executor, state);
            }
            catch (Exception exception)
            {
                return Task.FromException<TResult>(exception);
            }
        }

        private static async Task<T> RunAfter<T>(Task turn, IAsyncQueryExecutor executor,
            Func<IAsyncQueryExecutor, Task<T>> query)
        {
            await turn;

            return await Invoke(executor, query);
        }

        private static async Task<TResult> RunAfter<TState, TResult>(Task turn,
            IAsyncQueryExecutor executor, TState state,
            Func<IAsyncQueryExecutor, TState, Task<TResult>> query)
        {
            await turn;

            return await Invoke(executor, state, query);
        }

        private static async Task<(int Count, List<T> Items)> RunCountAndPage<T>(
            IAsyncQueryExecutor executor, IQueryable<T> source, IQueryable<T> page, bool pageIsSubset,
            CancellationToken cancellationToken, bool synchronousFallback)
        {
            try
            {
                int count = pageIsSubset
                    ? await executor.CountAsync(source, cancellationToken)
                    : 0;
                List<T> items = await executor.ToListAsync(page, cancellationToken);

                return (pageIsSubset ? count : items.Count, items);
            }
            catch (Exception exception) when (synchronousFallback
                && executor.CanFallBackToSynchronous(exception))
            {
                int count = pageIsSubset ? source.Count() : 0;
                List<T> items = page.ToList();
                return (pageIsSubset ? count : items.Count, items);
            }
        }

        private static async Task Settle(Task query)
        {
            try
            {
                await query;
            }
            catch
            {
                // The caller observes the outcome; the queue records only settlement.
            }
        }

        public void Dispose()
        {
            disposed = true;
            loadCancellation?.Cancel();
            virtualRequest?.Cancel();
            lookupCancellation?.Cancel();
        }
    }
}
