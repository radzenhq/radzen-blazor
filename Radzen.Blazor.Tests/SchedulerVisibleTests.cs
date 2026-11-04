using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SchedulerVisibleTests
    {
        class Appointment
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        static IRenderedComponent<RadzenScheduler<Appointment>> RenderScheduler(TestContext ctx, bool visible)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });

            return ctx.RenderComponent<RadzenScheduler<Appointment>>(p =>
            {
                p.Add(x => x.Visible, visible);
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
                p.Add(x => x.Date, new DateTime(2026, 11, 4));
                p.Add(x => x.Data, new List<Appointment>());
                p.Add(x => x.StartProperty, nameof(Appointment.Start));
                p.Add(x => x.EndProperty, nameof(Appointment.End));
                p.Add(x => x.TextProperty, nameof(Appointment.Text));
                p.AddChildContent<RadzenDayView>();
            });
        }

        [Fact]
        public void Scheduler_Visible_False_Renders_Nothing()
        {
            using var ctx = new TestContext();

            var component = RenderScheduler(ctx, false);

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Empty(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));
        }

        [Fact]
        public void Scheduler_Visible_Toggle_Recreates_Resizable()
        {
            using var ctx = new TestContext();

            var component = RenderScheduler(ctx, true);

            Assert.Contains("rz-scheduler", component.Markup);
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createResizable"));

            component.SetParametersAndRender(p => p.Add(x => x.Visible, false));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.disposeElement"));

            component.SetParametersAndRender(p => p.Add(x => x.Visible, true));

            Assert.Contains("rz-scheduler", component.Markup);
            Assert.Contains("rz-view", component.Markup);
            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createResizable"));
        }
    }
}
