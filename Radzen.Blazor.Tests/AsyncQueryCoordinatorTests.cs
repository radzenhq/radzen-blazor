using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AsyncQueryCoordinatorTests
    {
        sealed class CoordinatorOwner
        {
            public int Notifications { get; private set; }

            public void Notify() => Notifications++;
        }

        sealed class Executor : IAsyncQueryExecutor
        {
            public Func<IQueryable<int>, CancellationToken, Task<int>> Count { get; set; } =
                (query, token) => Task.FromResult(query.Count());

            public Func<IQueryable<int>, CancellationToken, Task<List<int>>> ToList { get; set; } =
                (query, token) => Task.FromResult(query.ToList());

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default) =>
                Count((IQueryable<int>)(object)queryable, cancellationToken);

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable,
                CancellationToken cancellationToken = default) =>
                (Task<List<T>>)(object)ToList((IQueryable<int>)(object)queryable, cancellationToken);
        }

        static AsyncQueryCoordinator Coordinator(Executor executor, out CoordinatorOwner owner)
        {
            owner = new CoordinatorOwner();

            return new AsyncQueryCoordinator(owner.Notify, executor);
        }

        static AsyncQueryCoordinator Coordinator(out CoordinatorOwner owner) => Coordinator(new(), out owner);

        static IQueryable<int> Query(params int[] items) => items.AsQueryable();

        [Fact]
        public async Task AnUncontendedQueryStartsAndCompletesInline()
        {
            bool invoked = false;
            Executor executor = new()
            {
                Count = (query, token) =>
                {
                    invoked = true;
                    return Task.FromResult(3);
                }
            };
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);

            Task<int> result = coordinator.CountAsync(Query(1, 2, 3), default);

            Assert.True(invoked);
            Assert.True(result.IsCompletedSuccessfully);
            Assert.Equal(3, await result);
        }

        [Fact]
        public async Task ContendedQueriesRunInFifoOrder()
        {
            TaskCompletionSource<int> first = new(TaskCreationOptions.RunContinuationsAsynchronously);
            List<string> calls = new();
            int countCalls = 0;
            Executor executor = new()
            {
                Count = (query, token) =>
                {
                    calls.Add($"count-{++countCalls}");
                    return countCalls == 1 ? first.Task : Task.FromResult(query.Count());
                },
                ToList = (query, token) =>
                {
                    calls.Add("list");
                    return Task.FromResult(query.ToList());
                }
            };
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);

            Task<int> firstQuery = coordinator.CountAsync(Query(1), default);
            Task<List<int>> secondQuery = coordinator.ToListAsync(Query(2), default);
            Task<int> thirdQuery = coordinator.CountAsync(Query(3), default);

            Assert.Equal(new[] { "count-1" }, calls);

            first.SetResult(1);
            await Task.WhenAll(firstQuery, secondQuery, thirdQuery);

            Assert.Equal(new[] { "count-1", "list", "count-2" }, calls);
        }

        [Fact]
        public async Task AFaultDoesNotPoisonTheQueueTail()
        {
            TaskCompletionSource<int> first = new(TaskCreationOptions.RunContinuationsAsynchronously);
            bool secondStarted = false;
            Executor executor = new()
            {
                Count = (query, token) => first.Task,
                ToList = (query, token) =>
                {
                    secondStarted = true;
                    return Task.FromResult(query.ToList());
                }
            };
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);

            Task<int> failed = coordinator.CountAsync(Query(1), default);
            Task<List<int>> next = coordinator.ToListAsync(Query(2), default);

            first.SetException(new InvalidOperationException("first failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => failed);
            Assert.Equal(new[] { 2 }, await next);
            Assert.True(secondStarted);
        }

        [Fact]
        public async Task ASynchronousExecutorThrowBecomesAFaultedTask()
        {
            Executor executor = new()
            {
                Count = (query, token) => throw new InvalidOperationException("synchronous"),
                ToList = (query, token) => Task.FromResult(query.ToList())
            };
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);

            Task<int> failed = coordinator.CountAsync(Query(1), default);

            Assert.True(failed.IsFaulted);
            await Assert.ThrowsAsync<InvalidOperationException>(() => failed);
            Assert.Equal(new[] { 2 }, await coordinator.ToListAsync(Query(2), default));
        }

        [Fact]
        public async Task CountAndPageKeepsItsRoundTripsInOneTurn()
        {
            TaskCompletionSource<int> count = new(TaskCreationOptions.RunContinuationsAsynchronously);
            List<string> calls = new();
            Executor executor = new()
            {
                ToList = (query, token) =>
                {
                    calls.Add(query.Count() == 1 ? "other" : "page");
                    return Task.FromResult(query.ToList());
                },
                Count = (query, token) =>
                {
                    calls.Add("count");
                    return count.Task;
                }
            };
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);

            Task<(int Count, List<int> Items)> pair = coordinator.CountAndPageAsync(
                Query(1, 2, 3), Query(1, 2), true, default);
            Task<List<int>> other = coordinator.ToListAsync(Query(9), default);

            Assert.Equal(new[] { "count" }, calls);

            count.SetResult(3);

            (int total, List<int> items) = await pair;
            Assert.Equal(3, total);
            Assert.Equal(new[] { 1, 2 }, items);
            Assert.Equal(new[] { 9 }, await other);
            Assert.Equal(new[] { "count", "page", "other" }, calls);
        }

        [Fact]
        public async Task WholeSourceCountComesFromTheMaterializedItems()
        {
            int countCalls = 0;
            Executor executor = new()
            {
                Count = (query, token) =>
                {
                    countCalls++;
                    return Task.FromResult(query.Count());
                }
            };
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);

            (int count, List<int> items) = await coordinator.CountAndPageAsync(
                Query(1, 2, 3), Query(1, 2, 3), false, default);

            Assert.Equal(3, count);
            Assert.Equal(new[] { 1, 2, 3 }, items);
            Assert.Equal(0, countCalls);
        }

        [Fact]
        public async Task VirtualCountAndPageAppliesApprovedSynchronousFallback()
        {
            Executor executor = new()
            {
                ToList = (query, token) => Task.FromException<List<int>>(
                    new InvalidOperationException("cannot be translated"))
            };
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);
            using var request = coordinator.TrackVirtualRequest(default);

            var result = await request.CountAndPageAsync(Query(1, 2, 3), Query(2, 3), true);

            Assert.True(result.HasValue);
            Assert.Equal(3, result.Value.Count);
            Assert.Equal(new[] { 2, 3 }, result.Value.Items);
        }

        [Fact]
        public async Task SupersedeWaitsForTheWholeLoadWhenCancellationIsAdvisory()
        {
            Executor executor = new();
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);
            TaskCompletionSource first = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource continueLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource second = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int calls = 0;

            executor.ToList = async (query, token) =>
            {
                if (++calls == 1)
                {
                    await first.Task;
                }
                else
                {
                    await second.Task;
                }

                return query.ToList();
            };

            Task<bool> load = coordinator.RunLoadAsync(async lease =>
            {
                await coordinator.ToListAsync(Query(1), lease.Token);
                await continueLoad.Task;
                await coordinator.ToListAsync(Query(2), lease.Token);
            });

            Task supersede = coordinator.SupersedeAsyncLoad();

            Assert.True(coordinator.LoadPending);

            first.SetResult();
            continueLoad.SetResult();

            while (Volatile.Read(ref calls) < 2)
            {
                await Task.Yield();
            }

            Assert.False(supersede.IsCompleted);

            second.SetResult();

            Assert.False(await load);
            await supersede;
            Assert.False(coordinator.LoadPending);
        }

        [Fact]
        public async Task SimultaneousSupersedingWaitsKeepPendingSuppressed()
        {
            Executor executor = new();
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);
            TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<bool> load = coordinator.RunLoadAsync(async lease => await gate.Task);

            Task first = coordinator.SupersedeAsyncLoad();
            Task second = coordinator.SupersedeAsyncLoad();

            Assert.True(coordinator.LoadPending);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);

            gate.SetResult();

            await Task.WhenAll(load, first, second);
            Assert.False(coordinator.LoadPending);
        }

        [Fact]
        public async Task ReplacingAVirtualLeaseDoesNotLetTheOldLeaseClearTheNewOne()
        {
            AsyncQueryCoordinator coordinator = Coordinator(out _);
            AsyncQueryCoordinator.VirtualRequestLease first = coordinator.TrackVirtualRequest(default);
            AsyncQueryCoordinator.VirtualRequestLease second = coordinator.TrackVirtualRequest(default);

            Assert.True(first.Token.IsCancellationRequested);
            Assert.False(second.Token.IsCancellationRequested);

            first.Dispose();
            await coordinator.SupersedeAsyncLoad();

            Assert.True(second.Token.IsCancellationRequested);
            second.Dispose();
        }

        [Fact]
        public async Task CancellationCallbackReentrancyDoesNotTransferLoadOwnership()
        {
            AsyncQueryCoordinator coordinator = Coordinator(out CoordinatorOwner owner);
            TaskCompletionSource firstGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource callbackGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<bool> callbackLoad = null!;

            Task<bool> first = coordinator.RunLoadAsync(async lease =>
            {
                using CancellationTokenRegistration registration = lease.Token.Register(() =>
                    callbackLoad = coordinator.RunLoadAsync(async callbackLease => await callbackGate.Task));
                await firstGate.Task;
            });

            Task<bool> superseded = coordinator.RunLoadAsync(lease =>
            {
                lease.ThrowIfSuperseded();
                return Task.CompletedTask;
            });

            Assert.NotNull(callbackLoad);
            Assert.False(await superseded);

            callbackGate.SetResult();
            Assert.True(await callbackLoad);

            firstGate.SetResult();
            Assert.False(await first);
            Assert.Equal(1, owner.Notifications);
        }

        [Fact]
        public async Task VirtualRequestDisposalIsDeferredUntilTheLeaseExits()
        {
            AsyncQueryCoordinator coordinator = Coordinator(out _);
            AsyncQueryCoordinator.VirtualRequestLease lease = coordinator.TrackVirtualRequest(default);

            coordinator.Dispose();

            Assert.True(lease.Token.IsCancellationRequested);
            Assert.NotNull(lease.Token.WaitHandle);

            lease.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { _ = lease.Token.WaitHandle; });
            await coordinator.SupersedeAsyncLoad();
        }

        [Fact]
        public async Task ComponentDisposalDoesNotDisposeALoadTokenStillInUse()
        {
            Executor executor = new();
            AsyncQueryCoordinator coordinator = Coordinator(executor, out _);
            TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            bool waitHandleWasSafe = false;

            Task<bool> load = coordinator.RunLoadAsync(async lease =>
            {
                await gate.Task;
                waitHandleWasSafe = lease.Token.WaitHandle != null;
            });

            coordinator.Dispose();
            gate.SetResult();

            Assert.True(await load);
            Assert.True(waitHandleWasSafe);
        }

        [Fact]
        public async Task OnlyTheOwningLeaseNotifiesItsComponentWhenItEnds()
        {
            Executor executor = new();
            AsyncQueryCoordinator coordinator = Coordinator(executor, out CoordinatorOwner owner);
            TaskCompletionSource firstGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource secondGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

            Task<bool> first = coordinator.RunLoadAsync(async lease => await firstGate.Task);
            Task<bool> second = coordinator.RunLoadAsync(async lease => await secondGate.Task);

            firstGate.SetResult();

            Assert.False(await first);
            Assert.Equal(0, owner.Notifications);

            secondGate.SetResult();

            Assert.True(await second);
            Assert.Equal(1, owner.Notifications);
        }
    }
}
