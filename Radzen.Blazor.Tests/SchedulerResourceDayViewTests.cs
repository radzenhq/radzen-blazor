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
    public class SchedulerResourceDayViewTests
    {
        class Booking
        {
            public int RoomId { get; set; }
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

        static IRenderedComponent<RadzenScheduler<Booking>> Render(TestContext ctx, List<Booking> bookings, Action<ComponentParameterCollectionBuilder<RadzenResourceDayView>>? configureView = null, Action<ComponentParameterCollectionBuilder<RadzenScheduler<Booking>>>? configureScheduler = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });

            return ctx.RenderComponent<RadzenScheduler<Booking>>(p =>
            {
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
                p.Add(x => x.Date, Today);
                p.Add(x => x.Data, bookings);
                p.Add(x => x.StartProperty, nameof(Booking.Start));
                p.Add(x => x.EndProperty, nameof(Booking.End));
                p.Add(x => x.TextProperty, nameof(Booking.Text));
                configureScheduler?.Invoke(p);
                p.AddChildContent<RadzenResourceDayView>(v =>
                {
                    v.Add(x => x.Data, Rooms);
                    v.Add(x => x.TextProperty, nameof(Room.Name));
                    v.Add(x => x.ValueProperty, nameof(Room.Id));
                    v.Add(x => x.ResourceProperty, nameof(Booking.RoomId));
                    configureView?.Invoke(v);
                });
            });
        }

        [Fact]
        public void ResourceDayView_Renders_Header_For_Every_Resource()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, new List<Booking>());

            var headers = cut.FindAll(".rz-resource-view .rz-view-header:not(.rz-resource-all-day) .rz-slot-header");

            Assert.Equal(3, headers.Count);
            Assert.Equal(new[] { "Room A", "Room B", "Room C" }, headers.Select(h => h.TextContent.Trim()));
        }

        [Fact]
        public void ResourceDayView_Renders_Column_For_Every_Resource()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, new List<Booking>());

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Equal(3, columns.Count);
        }

        [Fact]
        public void ResourceDayView_Renders_Appointment_In_Matching_Resource_Column()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 2, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render(ctx, bookings);

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Empty(columns[0].QuerySelectorAll(".rz-event"));
            Assert.Single(columns[1].QuerySelectorAll(".rz-event"));
            Assert.Empty(columns[2].QuerySelectorAll(".rz-event"));
            Assert.Contains("Standup", columns[1].QuerySelector(".rz-event-content")!.TextContent);
        }

        [Fact]
        public void ResourceDayView_Does_Not_Render_Appointment_Without_Matching_Resource()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 42, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Orphan" }
            };

            var cut = Render(ctx, bookings);

            Assert.Empty(cut.FindAll(".rz-resource-view .rz-event"));
        }

        [Fact]
        public void ResourceDayView_Renders_AllDay_Appointment_In_AllDay_Row()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today, End = Today.AddDays(1), Text = "Off-site" },
                new() { RoomId = 1, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render(ctx, bookings);

            var allDaySlots = cut.FindAll(".rz-resource-all-day .rz-resource-all-day-slot");

            Assert.Equal(3, allDaySlots.Count);
            Assert.Contains("Off-site", allDaySlots[0].TextContent);

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");
            var columnEvents = columns[0].QuerySelectorAll(".rz-event");

            Assert.Single(columnEvents);
            Assert.Contains("Standup", columnEvents[0].TextContent);
        }

        [Fact]
        public void ResourceDayView_Renders_AllDay_Appointment_In_Grid_When_ShowAllDay_Is_False()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today, End = Today.AddDays(1), Text = "Off-site" }
            };

            var cut = Render(ctx, bookings, v => v.Add(x => x.ShowAllDay, false));

            Assert.Empty(cut.FindAll(".rz-resource-all-day"));

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Single(columns[0].QuerySelectorAll(".rz-event"));
        }

        [Fact]
        public void ResourceDayView_SlotSelect_Provides_Resource()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var cut = Render(ctx, new List<Booking>(),
                configureScheduler: p => p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; }));

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");
            var slot = columns[1].QuerySelectorAll(".rz-slot").First();

            slot.Click();

            Assert.NotNull(slotSelectArgs);
            var room = Assert.IsType<Room>(slotSelectArgs!.Resource);
            Assert.Equal(2, room.Id);
        }

        [Fact]
        public void ResourceDayView_AllDaySlot_Click_Selects_Whole_Day_With_Resource()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var cut = Render(ctx, new List<Booking>(),
                configureScheduler: p => p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; }));

            var allDaySlots = cut.FindAll(".rz-resource-all-day .rz-resource-all-day-slot");

            allDaySlots[2].Click();

            Assert.NotNull(slotSelectArgs);
            Assert.Equal(Today, slotSelectArgs!.Start);
            Assert.Equal(Today.AddDays(1), slotSelectArgs.End);
            var room = Assert.IsType<Room>(slotSelectArgs.Resource);
            Assert.Equal(3, room.Id);
        }

        [Fact]
        public void ResourceDayView_SlotRender_Provides_Resource()
        {
            using var ctx = new TestContext();

            var resources = new HashSet<int>();

            Render(ctx, new List<Booking>(),
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
        public void ResourceDayView_AppointmentSelect_Fires_For_AllDay_Appointment()
        {
            using var ctx = new TestContext();

            Booking? selected = null;

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today, End = Today.AddDays(1), Text = "Off-site" }
            };

            var cut = Render(ctx, bookings,
                configureScheduler: p => p.Add(x => x.AppointmentSelect, args => { selected = args.Data; }));

            var appointment = cut.Find(".rz-resource-all-day .rz-event");

            appointment.Click();

            Assert.NotNull(selected);
            Assert.Equal("Off-site", selected!.Text);
        }

        [Fact]
        public void ResourceDayView_Has_Default_Localized_Text_And_AllDay_Label()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, new List<Booking>());

            Assert.Contains(cut.FindAll(".rz-scheduler-nav-views .rz-button"), b => b.TextContent.Contains("Resources"));
            Assert.Contains("All day", cut.Find(".rz-resource-all-day-label").TextContent);
        }

        [Fact]
        public void ResourceDayView_Text_And_AllDayText_Can_Be_Overridden()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, new List<Booking>(), v =>
            {
                v.Add(x => x.Text, "Rooms");
                v.Add(x => x.AllDayText, "Whole day");
            });

            Assert.Contains(cut.FindAll(".rz-scheduler-nav-views .rz-button"), b => b.TextContent.Contains("Rooms"));
            Assert.Contains("Whole day", cut.Find(".rz-resource-all-day-label").TextContent);
        }

        [Fact]
        public void ResourceDayView_Supports_Collection_ResourceProperty()
        {
            using var ctx = new TestContext();

            var bookings = new List<SharedBooking>
            {
                new() { RoomIds = new List<int> { 1, 3 }, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Shared" }
            };

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });

            var cut = ctx.RenderComponent<RadzenScheduler<SharedBooking>>(p =>
            {
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
                p.Add(x => x.Date, Today);
                p.Add(x => x.Data, bookings);
                p.Add(x => x.StartProperty, nameof(SharedBooking.Start));
                p.Add(x => x.EndProperty, nameof(SharedBooking.End));
                p.Add(x => x.TextProperty, nameof(SharedBooking.Text));
                p.AddChildContent<RadzenResourceDayView>(v =>
                {
                    v.Add(x => x.Data, Rooms);
                    v.Add(x => x.TextProperty, nameof(Room.Name));
                    v.Add(x => x.ValueProperty, nameof(Room.Id));
                    v.Add(x => x.ResourceProperty, nameof(SharedBooking.RoomIds));
                });
            });

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Single(columns[0].QuerySelectorAll(".rz-event"));
            Assert.Empty(columns[1].QuerySelectorAll(".rz-event"));
            Assert.Single(columns[2].QuerySelectorAll(".rz-event"));
        }

        class SharedBooking
        {
            public List<int> RoomIds { get; set; } = new();
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Text { get; set; } = "";
        }

        [Fact]
        public async System.Threading.Tasks.Task ResourceDayView_AppointmentMove_Provides_Target_Resource()
        {
            using var ctx = new TestContext();

            SchedulerAppointmentMoveEventArgs? moveArgs = null;

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render(ctx, bookings,
                configureScheduler: p => p.Add(x => x.AppointmentMove, args => { moveArgs = args; }));

            var view = cut.FindComponent<ResourceDayView>();
            var appointment = cut.Instance.GetAppointmentsInRange(Today.AddHours(8), Today.AddHours(24)).First();

            await cut.InvokeAsync(() => view.Instance.OnAppointmentDragStart(appointment));

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");
            var targetSlot = columns[2].QuerySelectorAll(".rz-slot").First();

            await targetSlot.DropAsync(new Microsoft.AspNetCore.Components.Web.DragEventArgs());

            Assert.NotNull(moveArgs);
            var room = Assert.IsType<Room>(moveArgs!.Resource);
            Assert.Equal(3, room.Id);
        }
    }
}
