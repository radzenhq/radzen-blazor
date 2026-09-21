using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.JSInterop;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DisposeUnobservedJsExceptionTests
    {
        class DisconnectedJSObjectReference : IJSObjectReference
        {
            static ValueTask<T> Faulted<T>() => new ValueTask<T>(Task.FromException<T>(new JSDisconnectedException("circuit disconnected")));

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => Faulted<TValue>();

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => Faulted<TValue>();

            public ValueTask DisposeAsync() => new ValueTask(Task.FromException(new JSDisconnectedException("circuit disconnected")));
        }

        static int CollectUnobserved(Action act)
        {
            var count = 0;
            EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, e) =>
            {
                if (e.Exception.InnerException is JSDisconnectedException)
                {
                    Interlocked.Increment(ref count);
                    e.SetObserved();
                }
            };

            TaskScheduler.UnobservedTaskException += handler;
            try
            {
                act();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= handler;
            }

            return count;
        }

        public static IEnumerable<object[]> Components()
        {
            yield return new object[] { typeof(RadzenNumeric<int>) };
            yield return new object[] { typeof(RadzenMask) };
            yield return new object[] { typeof(RadzenSlider<int>) };
            yield return new object[] { typeof(RadzenSecurityCode) };
            yield return new object[] { typeof(RadzenSignaturePad) };
            yield return new object[] { typeof(RadzenUpload) };
            yield return new object[] { typeof(RadzenFileInput<string>) };
            yield return new object[] { typeof(RadzenMenu) };
            yield return new object[] { typeof(RadzenDropDown<int>) };
            yield return new object[] { typeof(RadzenDataGrid<object>) };
        }

        static void RenderAndDisposeWithDisconnectedJs<TComponent>() where TComponent : Microsoft.AspNetCore.Components.IComponent, IDisposable
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var cut = ctx.RenderComponent<TComponent>();
            var instance = cut.Instance;

            FieldInfo? field = null;
            for (var type = typeof(TComponent); type != null && field == null; type = type.BaseType)
            {
                field = type.GetField("_jsRef", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            Assert.NotNull(field);
            field!.SetValue(instance, new DisconnectedJSObjectReference());

            instance.Dispose();
        }

        [Theory]
        [MemberData(nameof(Components))]
        public void Dispose_Does_Not_Leak_Unobserved_JSDisconnectedException(Type componentType)
        {
            var method = typeof(DisposeUnobservedJsExceptionTests)
                .GetMethod(nameof(RenderAndDisposeWithDisconnectedJs), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(componentType);

            var unobserved = CollectUnobserved(() => method.Invoke(null, null));

            Assert.Equal(0, unobserved);
        }

        public static IEnumerable<object[]> RuntimeComponents()
        {
            yield return new object[] { typeof(RadzenMediaQuery), "Radzen.mediaQuery", "initialized" };
            yield return new object[] { typeof(RadzenProfileMenu), "Radzen.unregisterProfileMenuClickAway", "clickAwayRegistered" };
            yield return new object[] { typeof(RadzenRangeNavigator), "Radzen.disposeElement", null! };
        }

        static void RenderAndDisposeWithDisconnectedRuntime<TComponent>(string identifier, string? flagField) where TComponent : Microsoft.AspNetCore.Components.IComponent, IDisposable
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var cut = ctx.RenderComponent<TComponent>();
            var instance = cut.Instance;

            ctx.JSInterop.SetupVoid(identifier, _ => true).SetException(new JSDisconnectedException("circuit disconnected"));

            if (flagField != null)
            {
                FieldInfo? field = null;
                for (var type = typeof(TComponent); type != null && field == null; type = type.BaseType)
                {
                    field = type.GetField(flagField, BindingFlags.Instance | BindingFlags.NonPublic);
                }

                Assert.NotNull(field);
                field!.SetValue(instance, true);
            }

            instance.Dispose();
        }

        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void Dispose_Does_Not_Leak_Unobserved_JSDisconnectedException_From_JSRuntime(Type componentType, string identifier, string? flagField)
        {
            var method = typeof(DisposeUnobservedJsExceptionTests)
                .GetMethod(nameof(RenderAndDisposeWithDisconnectedRuntime), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(componentType);

            var unobserved = CollectUnobserved(() => method.Invoke(null, new object?[] { identifier, flagField }));

            Assert.Equal(0, unobserved);
        }

        [Fact]
        public void Unawaited_Faulted_Interop_Is_Reported_As_Unobserved()
        {
            var unobserved = CollectUnobserved(() =>
            {
                IJSObjectReference jsRef = new DisconnectedJSObjectReference();
                jsRef.InvokeVoidAsync("dispose");
                jsRef.DisposeAsync();
            });

            Assert.True(unobserved > 0);
        }
    }
}
