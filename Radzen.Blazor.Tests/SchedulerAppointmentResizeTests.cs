using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SchedulerAppointmentResizeTests
    {
        class Appointment
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });
            return ctx;
        }

        static IRenderedComponent<RadzenScheduler<Appointment>> RenderScheduler(TestContext ctx, IList<Appointment> appointments, Action<SchedulerAppointmentResizeEventArgs> onResize = null)
        {
            return ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.Date, DateTime.Today);
                p.Add(x => x.Data, appointments);
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.Add(x => x.SelectedIndex, 0);

                if (onResize != null)
                {
                    p.Add(x => x.AppointmentResize, onResize);
                }

                p.AddChildContent<RadzenDayView>();
            });
        }

        [Fact]
        public void DayView_RendersResizeHandles_WhenAppointmentResizeHasDelegate()
        {
            using var ctx = CreateContext();

            var appointments = new List<Appointment>
            {
                new() { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Meeting" }
            };

            var cut = RenderScheduler(ctx, appointments, args => { });

            Assert.Single(cut.FindAll(".rz-event-resize-start"));
            Assert.Single(cut.FindAll(".rz-event-resize-end"));
        }

        [Fact]
        public void DayView_DoesNotRenderResizeHandles_WithoutAppointmentResizeDelegate()
        {
            using var ctx = CreateContext();

            var appointments = new List<Appointment>
            {
                new() { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Meeting" }
            };

            var cut = RenderScheduler(ctx, appointments);

            Assert.Empty(cut.FindAll(".rz-event-resize"));
        }

        [Fact]
        public void DayView_DoesNotRenderHandlesOnClippedEdges_OfMultiDayAppointment()
        {
            using var ctx = CreateContext();

            var appointments = new List<Appointment>
            {
                new() { Start = DateTime.Today.AddDays(-1), End = DateTime.Today.AddDays(2), Text = "Vacation" }
            };

            var cut = RenderScheduler(ctx, appointments, args => { });

            Assert.Empty(cut.FindAll(".rz-event-resize"));
        }

        [Fact]
        public async Task OnResize_RaisesAppointmentResize_WithNewBoundaries()
        {
            using var ctx = CreateContext();

            var appointment = new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Meeting" };

            SchedulerAppointmentResizeEventArgs raised = null;

            var cut = RenderScheduler(ctx, new List<Appointment> { appointment }, args => raised = args);

            var rendered = cut.FindComponents<Radzen.Blazor.Rendering.Appointment>().Single(c => c.Instance.Data?.Data == appointment);

            await cut.InvokeAsync(() => rendered.Instance.OnResizeAsync(0, 30));

            Assert.NotNull(raised);
            Assert.Equal(appointment.Start, raised.Start);
            Assert.Equal(appointment.End.AddMinutes(30), raised.End);
            Assert.Same(appointment, raised.Appointment.Data);
        }

        [Fact]
        public async Task OnResize_DoesNotRaiseAppointmentResize_WhenResultIsEmptyRange()
        {
            using var ctx = CreateContext();

            var appointment = new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Meeting" };

            SchedulerAppointmentResizeEventArgs raised = null;

            var cut = RenderScheduler(ctx, new List<Appointment> { appointment }, args => raised = args);

            var rendered = cut.FindComponents<Radzen.Blazor.Rendering.Appointment>().Single(c => c.Instance.Data?.Data == appointment);

            await cut.InvokeAsync(() => rendered.Instance.OnResizeAsync(0, -180));

            Assert.Null(raised);
        }
    }
}
