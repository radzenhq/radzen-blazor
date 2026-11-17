using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SchedulerAppointmentDisposeTests
    {
        class Appointment
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        class FakeJSObjectReference : IJSObjectReference
        {
            public List<string> Invocations { get; } = new();

            public bool Disposed { get; private set; }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
            {
                Invocations.Add(identifier);
                return new ValueTask<TValue>(default(TValue));
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

            public ValueTask DisposeAsync()
            {
                Disposed = true;
                return ValueTask.CompletedTask;
            }
        }

        class FakeJSRuntime : IJSRuntime
        {
            public TaskCompletionSource<IJSObjectReference> Draggable { get; } = new();

            public List<string> Invocations { get; } = new();

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
            {
                Invocations.Add(identifier);

                foreach (var arg in args ?? Array.Empty<object>())
                {
                    if (arg is DotNetObjectReference<Radzen.Blazor.Rendering.Appointment> reference)
                    {
                        _ = reference.Value;
                    }
                }

                if (identifier == "Radzen.createDraggable")
                {
                    return new ValueTask<TValue>(Await<TValue>(Draggable.Task));
                }

                if (typeof(TValue) == typeof(Rect))
                {
                    return new ValueTask<TValue>((TValue)(object)new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });
                }

                if (typeof(TValue) == typeof(IJSObjectReference))
                {
                    return new ValueTask<TValue>((TValue)(object)new FakeJSObjectReference());
                }

                return new ValueTask<TValue>(default(TValue));
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

            static async Task<TValue> Await<TValue>(Task<IJSObjectReference> task) => (TValue)await task;
        }

        [Fact]
        public async Task Appointment_DisposedWhileCreatingDraggable_DoesNotUseDisposedReference()
        {
            using var ctx = new TestContext();
            var js = new FakeJSRuntime();
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddSingleton<IJSRuntime>(js);

            var appointments = new List<Appointment>
            {
                new() { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Meeting" }
            };

            var cut = ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.Date, DateTime.Today);
                p.Add(x => x.Data, appointments);
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.Add(x => x.SelectedIndex, 0);
                p.Add(x => x.AppointmentMove, (SchedulerAppointmentMoveEventArgs args) => { });
                p.Add(x => x.AppointmentResize, (SchedulerAppointmentResizeEventArgs args) => { });
                p.AddChildContent<RadzenDayView>();
            });

            Assert.Contains("Radzen.createDraggable", js.Invocations);

            cut.SetParametersAndRender(p => p.Add(x => x.Data, new List<Appointment>()));

            Assert.Empty(cut.FindAll(".rz-event"));

            var draggable = new FakeJSObjectReference();
            js.Draggable.SetResult(draggable);

            await Task.Delay(100);

            Assert.False(ctx.Renderer.UnhandledException.IsCompleted, ctx.Renderer.UnhandledException.IsCompleted ? ctx.Renderer.UnhandledException.Result.ToString() : null);
            Assert.DoesNotContain("Radzen.createSchedulerResizable", js.Invocations);
            Assert.Contains("dispose", draggable.Invocations);
            Assert.True(draggable.Disposed);
        }
    }
}
