using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Radzen
{
    /// <summary>
    /// Owns asynchronous query execution for one data-bound component: resolving the
    /// <see cref="IAsyncQueryExecutor" /> and the lifetime of the component's
    /// <see cref="AsyncQueryCoordinator" />. Only the data-bound base classes create one, so
    /// components without bound queries carry no query state.
    /// </summary>
    internal sealed class AsyncQueryHost : IDisposable
    {
        private readonly IServiceProvider? services;
        private readonly Action loadCompleted;
        private bool executorResolved;
        private IAsyncQueryExecutor? executor;
        private AsyncQueryCoordinator? coordinator;

        internal AsyncQueryHost(IServiceProvider? services, Action loadCompleted)
        {
            this.services = services;
            this.loadCompleted = loadCompleted;
        }

        private IAsyncQueryExecutor? Executor
        {
            get
            {
                if (!executorResolved)
                {
                    executorResolved = true;
                    executor = services?.GetService<IAsyncQueryExecutor>()
                        ?? (AsyncQueryExecutionDisabled ? null : AsyncEnumerableQueryExecutor.Instance);
                }

                return executor;
            }
        }

        internal bool HasExecutor => Executor != null;

        private static bool AsyncQueryExecutionDisabled =>
            AppContext.TryGetSwitch("Radzen.Blazor.DisableAsyncQueryExecution", out var disabled) && disabled;

        internal bool LoadPending => coordinator?.LoadPending == true;

        internal Task SupersedeLoad() => coordinator?.SupersedeAsyncLoad() ?? Task.CompletedTask;

        internal void CancelLookup() => coordinator?.CancelLookup();

        internal bool TryGetCoordinator<T>(IQueryable<T> query,
            [NotNullWhen(true)] out AsyncQueryCoordinator? coordinator)
        {
            var executor = Executor;

            if (executor != null && executor.IsSupported(query))
            {
                coordinator = this.coordinator ??= new AsyncQueryCoordinator(loadCompleted, executor);
                return true;
            }

            coordinator = null;
            return false;
        }

        public void Dispose() => coordinator?.Dispose();
    }
}
