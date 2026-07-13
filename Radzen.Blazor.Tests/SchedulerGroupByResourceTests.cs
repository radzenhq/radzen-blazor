using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using Radzen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SchedulerGroupByResourceTests
    {
        class Booking
        {
            public int RoomId { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        class SharedBooking
        {
            public List<int> RoomIds { get; set; } = new();
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        class Room
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        static readonly DateTime Today = new DateTime(2024, 12, 2);

        static List<Room> Rooms => new()
        {
            new() { Id = 1, Name = "Room A" },
            new() { Id = 2, Name = "Room B" },
            new() { Id = 3, Name = "Room C" },
        };

        static void Setup(TestContext ctx)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });
        }

        static IRenderedComponent<RadzenScheduler<TItem>> Render<TItem, TView>(TestContext ctx, List<TItem> bookings, string resourceProperty, bool groupByResource = true, Action<ComponentParameterCollectionBuilder<TView>>? configureView = null, Action<ComponentParameterCollectionBuilder<RadzenScheduler<TItem>>>? configureScheduler = null) where TView : SchedulerViewBase
        {
            Setup(ctx);

            return ctx.RenderComponent<RadzenScheduler<TItem>>(p =>
            {
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
                p.Add(x => x.Date, Today);
                p.Add(x => x.Data, bookings);
                p.Add(x => x.StartProperty, "Start");
                p.Add(x => x.EndProperty, "End");
                p.Add(x => x.TextProperty, "Text");
                p.Add(x => x.Resources, Rooms);
                p.Add(x => x.ResourceTextProperty, nameof(Room.Name));
                p.Add(x => x.ResourceValueProperty, nameof(Room.Id));
                p.Add(x => x.ResourceProperty, resourceProperty);
                configureScheduler?.Invoke(p);
                p.AddChildContent<TView>(v =>
                {
                    v.Add(x => x.GroupByResource, groupByResource);
                    configureView?.Invoke(v);
                });
            });
        }

        [Fact]
        public void DayView_Grouped_Renders_Header_And_Column_For_Every_Resource()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), nameof(Booking.RoomId));

            var headers = cut.FindAll(".rz-resource-view .rz-view-header:not(.rz-resource-all-day) .rz-slot-header");

            Assert.Equal(3, headers.Count);
            Assert.Equal(new[] { "Room A", "Room B", "Room C" }, headers.Select(h => h.TextContent.Trim()));

            Assert.Equal(3, cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots").Count);
        }

        [Fact]
        public void DayView_Grouped_Renders_Appointment_In_Matching_Resource_Column()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 2, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, nameof(Booking.RoomId));

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Empty(columns[0].QuerySelectorAll(".rz-event"));
            Assert.Single(columns[1].QuerySelectorAll(".rz-event"));
            Assert.Empty(columns[2].QuerySelectorAll(".rz-event"));
        }

        [Fact]
        public void DayView_Grouped_Renders_AllDay_Appointment_In_AllDay_Row()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today, End = Today.AddDays(1), Text = "Off-site" },
                new() { RoomId = 1, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, nameof(Booking.RoomId));

            var allDaySlots = cut.FindAll(".rz-resource-all-day .rz-resource-all-day-slot");

            Assert.Equal(3, allDaySlots.Count);
            Assert.Contains("Off-site", allDaySlots[0].TextContent);

            var columnEvents = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots")[0].QuerySelectorAll(".rz-event");

            Assert.Single(columnEvents);
            Assert.Contains("Standup", columnEvents[0].TextContent);
        }

        [Fact]
        public void DayView_Grouped_Renders_AllDay_Appointment_In_Grid_When_ShowAllDay_Is_False()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today, End = Today.AddDays(1), Text = "Off-site" }
            };

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, nameof(Booking.RoomId), configureView: v => v.Add(x => x.ShowAllDay, false));

            Assert.Empty(cut.FindAll(".rz-resource-all-day"));
            Assert.Single(cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots")[0].QuerySelectorAll(".rz-event"));
        }

        [Fact]
        public void DayView_Grouped_SlotSelect_Provides_Resource()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), nameof(Booking.RoomId),
                configureScheduler: p => p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; }));

            cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots")[1].QuerySelectorAll(".rz-slot").First().Click();

            Assert.NotNull(slotSelectArgs);
            var room = Assert.IsType<Room>(slotSelectArgs!.Resource);
            Assert.Equal(2, room.Id);
        }

        [Fact]
        public void DayView_Grouped_AllDaySlot_Click_Selects_Whole_Day_With_Resource()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), nameof(Booking.RoomId),
                configureScheduler: p => p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; }));

            cut.FindAll(".rz-resource-all-day .rz-resource-all-day-slot")[2].Click();

            Assert.NotNull(slotSelectArgs);
            Assert.Equal(Today, slotSelectArgs!.Start);
            Assert.Equal(Today.AddDays(1), slotSelectArgs.End);
            Assert.Equal(3, Assert.IsType<Room>(slotSelectArgs.Resource).Id);
        }

        [Fact]
        public void DayView_Grouped_Supports_Collection_ResourceProperty()
        {
            using var ctx = new TestContext();

            var bookings = new List<SharedBooking>
            {
                new() { RoomIds = new List<int> { 1, 3 }, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Shared" }
            };

            var cut = Render<SharedBooking, RadzenDayView>(ctx, bookings, nameof(SharedBooking.RoomIds));

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Single(columns[0].QuerySelectorAll(".rz-event"));
            Assert.Empty(columns[1].QuerySelectorAll(".rz-event"));
            Assert.Single(columns[2].QuerySelectorAll(".rz-event"));
        }

        [Fact]
        public void DayView_Grouped_Has_Localized_AllDay_Label_Which_Can_Be_Overridden()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), nameof(Booking.RoomId));

            Assert.Contains("All day", cut.Find(".rz-resource-all-day-label").TextContent);

            using var overriddenCtx = new TestContext();

            var overridden = Render<Booking, RadzenDayView>(overriddenCtx, new List<Booking>(), nameof(Booking.RoomId), configureView: v => v.Add(x => x.AllDayText, "Whole day"));

            Assert.Contains("Whole day", overridden.Find(".rz-resource-all-day-label").TextContent);
        }

        [Fact]
        public async System.Threading.Tasks.Task DayView_Grouped_AppointmentMove_Provides_Target_Resource()
        {
            using var ctx = new TestContext();

            SchedulerAppointmentMoveEventArgs? moveArgs = null;

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, nameof(Booking.RoomId),
                configureScheduler: p => p.Add(x => x.AppointmentMove, args => { moveArgs = args; }));

            var view = cut.FindComponent<ResourceDayView>();
            var appointment = cut.Instance.GetAppointmentsInRange(Today.AddHours(8), Today.AddHours(24)).First();

            await cut.InvokeAsync(() => view.Instance.OnAppointmentDragStart(appointment));

            var targetSlot = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots")[2].QuerySelectorAll(".rz-slot").First();

            await targetSlot.DropAsync(new Microsoft.AspNetCore.Components.Web.DragEventArgs());

            Assert.NotNull(moveArgs);
            Assert.Equal(3, Assert.IsType<Room>(moveArgs!.Resource).Id);
        }

        [Fact]
        public void DayView_Not_Grouped_Renders_Default_Day_View()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), nameof(Booking.RoomId), groupByResource: false);

            Assert.Empty(cut.FindAll(".rz-resource-view"));
            Assert.Single(cut.FindAll(".rz-day-view"));
        }

        [Fact]
        public void DayView_Grouped_Without_Resources_Renders_Default_Day_View()
        {
            using var ctx = new TestContext();
            Setup(ctx);

            var cut = ctx.RenderComponent<RadzenScheduler<Booking>>(p =>
            {
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
                p.Add(x => x.Date, Today);
                p.Add(x => x.Data, new List<Booking>());
                p.Add(x => x.StartProperty, "Start");
                p.Add(x => x.EndProperty, "End");
                p.Add(x => x.TextProperty, "Text");
                p.AddChildContent<RadzenDayView>(v => v.Add(x => x.GroupByResource, true));
            });

            Assert.Empty(cut.FindAll(".rz-resource-view"));
            Assert.Single(cut.FindAll(".rz-day-view"));
        }

        [Fact]
        public void WeekView_Grouped_Renders_Section_Per_Resource_With_Filtered_Appointments()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" },
                new() { RoomId = 3, Start = Today.AddHours(12), End = Today.AddHours(13), Text = "Review" }
            };

            var cut = Render<Booking, RadzenWeekView>(ctx, bookings, nameof(Booking.RoomId));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            Assert.Equal(3, groups.Count);
            Assert.Equal(new[] { "Room A", "Room B", "Room C" }, groups.Select(g => g.QuerySelector(".rz-resource-group-header")!.TextContent.Trim()));

            Assert.Contains("Standup", groups[0].QuerySelector(".rz-event-content")?.TextContent);
            Assert.Null(groups[1].QuerySelector(".rz-event-content"));
            Assert.Contains("Review", groups[2].QuerySelector(".rz-event-content")?.TextContent);
        }

        [Fact]
        public void WeekView_Grouped_SlotSelect_Provides_Resource()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), nameof(Booking.RoomId),
                configureScheduler: p => p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; }));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            groups[1].QuerySelectorAll(".rz-slot").First().Click();

            Assert.NotNull(slotSelectArgs);
            Assert.Equal(2, Assert.IsType<Room>(slotSelectArgs!.Resource).Id);
        }

        [Fact]
        public void MonthView_Grouped_Renders_Section_Per_Resource()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 2, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenMonthView>(ctx, bookings, nameof(Booking.RoomId));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            Assert.Equal(3, groups.Count);
            Assert.Null(groups[0].QuerySelector(".rz-event-content"));
            Assert.Contains("Standup", groups[1].QuerySelector(".rz-event-content")?.TextContent);
        }

        [Fact]
        public void MonthView_Grouped_SlotRender_Provides_Resource()
        {
            using var ctx = new TestContext();

            var resources = new HashSet<int>();

            Render<Booking, RadzenMonthView>(ctx, new List<Booking>(), nameof(Booking.RoomId),
                configureScheduler: p => p.Add(x => x.SlotRender, args =>
                {
                    if (args.Resource is Room room)
                    {
                        resources.Add(room.Id);
                    }
                }));

            Assert.Equal(new HashSet<int> { 1, 2, 3 }, resources);
        }

        [Fact]
        public void AgendaView_Grouped_Renders_Section_Per_Resource()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 3, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Review" }
            };

            var cut = Render<Booking, RadzenAgendaView>(ctx, bookings, nameof(Booking.RoomId));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            Assert.Equal(3, groups.Count);
            Assert.Contains("Review", groups[2].TextContent);
        }

        [Fact]
        public void YearPlannerView_Grouped_Renders_Section_Per_Resource()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenYearPlannerView>(ctx, new List<Booking>(), nameof(Booking.RoomId));

            Assert.Equal(3, cut.FindAll(".rz-resource-groups .rz-resource-group").Count);
        }

        [Fact]
        public void ResourceHeaderTemplate_Is_Used_For_Group_Headers()
        {
            using var ctx = new TestContext();

            Microsoft.AspNetCore.Components.RenderFragment<object> template = resource => builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddContent(1, $"#{((Room)resource).Id}");
                builder.CloseElement();
            };

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), nameof(Booking.RoomId),
                configureScheduler: p => p.Add(x => x.ResourceHeaderTemplate, template));

            var headers = cut.FindAll(".rz-resource-group-header strong");

            Assert.Equal(3, headers.Count);
            Assert.Equal("#1", headers[0].TextContent);
        }

        [Fact]
        public void Existing_Views_Render_Unchanged_When_Scheduler_Has_Resources_But_View_Does_Not_Group()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 2, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenWeekView>(ctx, bookings, nameof(Booking.RoomId), groupByResource: false);

            Assert.Empty(cut.FindAll(".rz-resource-groups"));
            Assert.Single(cut.FindAll(".rz-week-view"));
            Assert.Single(cut.FindAll(".rz-event"));
        }
    }
}
