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
            public int EmployeeId { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public bool AllDay { get; set; }
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

        class Employee
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

        static List<Employee> Employees => new()
        {
            new() { Id = 1, Name = "Nancy" },
            new() { Id = 2, Name = "Andrew" },
        };

        static void Setup(TestContext ctx)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.JSInterop.Setup<Rect>("Radzen.createResizable", _ => true)
                .SetResult(new Rect { Left = 0, Top = 0, Width = 800, Height = 600 });
        }

        static void AddRoomResource<TItem>(ComponentParameterCollectionBuilder<RadzenScheduler<TItem>> p, string property = nameof(Booking.RoomId))
        {
            p.AddChildContent<RadzenSchedulerResource>(r =>
            {
                r.Add(x => x.Name, "Room");
                r.Add(x => x.Data, Rooms);
                r.Add(x => x.Property, property);
                r.Add(x => x.TextProperty, nameof(Room.Name));
                r.Add(x => x.ValueProperty, nameof(Room.Id));
            });
        }

        static void AddEmployeeResource<TItem>(ComponentParameterCollectionBuilder<RadzenScheduler<TItem>> p)
        {
            p.AddChildContent<RadzenSchedulerResource>(r =>
            {
                r.Add(x => x.Name, "Employee");
                r.Add(x => x.Data, Employees);
                r.Add(x => x.Property, nameof(Booking.EmployeeId));
                r.Add(x => x.TextProperty, nameof(Employee.Name));
                r.Add(x => x.ValueProperty, nameof(Employee.Id));
            });
        }

        static IRenderedComponent<RadzenScheduler<TItem>> Render<TItem, TView>(TestContext ctx, List<TItem> bookings,
            Action<ComponentParameterCollectionBuilder<RadzenScheduler<TItem>>> addResources,
            bool groupByResource = true,
            Action<ComponentParameterCollectionBuilder<TView>>? configureView = null,
            Action<ComponentParameterCollectionBuilder<RadzenScheduler<TItem>>>? configureScheduler = null) where TView : SchedulerViewBase
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
                configureScheduler?.Invoke(p);
                addResources(p);
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

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), p => AddRoomResource(p));

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

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, p => AddRoomResource(p));

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Empty(columns[0].QuerySelectorAll(".rz-event"));
            Assert.Single(columns[1].QuerySelectorAll(".rz-event"));
            Assert.Empty(columns[2].QuerySelectorAll(".rz-event"));
        }

        [Fact]
        public void DayView_Grouped_By_Two_Resource_Types_Renders_Hierarchical_Columns()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 2, EmployeeId = 1, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, p => { AddRoomResource(p); AddEmployeeResource(p); });

            var headerRows = cut.FindAll(".rz-resource-view .rz-view-header:not(.rz-resource-all-day)");

            Assert.Equal(2, headerRows.Count);
            Assert.Equal(3, headerRows[0].QuerySelectorAll(".rz-slot-header").Length);
            Assert.Equal(6, headerRows[1].QuerySelectorAll(".rz-slot-header").Length);

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Equal(6, columns.Count);

            for (var i = 0; i < columns.Count; i++)
            {
                var events = columns[i].QuerySelectorAll(".rz-event");

                if (i == 2)
                {
                    Assert.Single(events);
                }
                else
                {
                    Assert.Empty(events);
                }
            }
        }

        [Fact]
        public void DayView_Grouped_SlotSelect_Provides_Resources_Path()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), p => { AddRoomResource(p); AddEmployeeResource(p); },
                configureScheduler: p => p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; }));

            cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots")[3].QuerySelectorAll(".rz-slot").First().Click();

            Assert.NotNull(slotSelectArgs);
            Assert.NotNull(slotSelectArgs!.Resources);
            Assert.Equal(2, Assert.IsType<Room>(slotSelectArgs.Resources!["Room"]).Id);
            Assert.Equal(2, Assert.IsType<Employee>(slotSelectArgs.Resources["Employee"]).Id);
            Assert.Same(slotSelectArgs.Resources["Employee"], slotSelectArgs.Resource);
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

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, p => AddRoomResource(p));

            var allDaySlots = cut.FindAll(".rz-resource-all-day .rz-resource-all-day-slot");

            Assert.Equal(3, allDaySlots.Count);
            Assert.Contains("Off-site", allDaySlots[0].TextContent);

            var columnEvents = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots")[0].QuerySelectorAll(".rz-event");

            Assert.Single(columnEvents);
            Assert.Contains("Standup", columnEvents[0].TextContent);
        }

        [Fact]
        public void DayView_Grouped_Vertical_Orientation_Renders_Stacked_Day_Views()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), p => AddRoomResource(p),
                configureView: v => v.Add(x => x.GroupOrientation, Orientation.Vertical));

            Assert.Empty(cut.FindAll(".rz-resource-view"));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            Assert.Equal(3, groups.Count);
            Assert.All(groups, group => Assert.NotNull(group.QuerySelector(".rz-day-view")));
        }

        [Fact]
        public void DayView_Grouped_Supports_Collection_ResourceProperty()
        {
            using var ctx = new TestContext();

            var bookings = new List<SharedBooking>
            {
                new() { RoomIds = new List<int> { 1, 3 }, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Shared" }
            };

            var cut = Render<SharedBooking, RadzenDayView>(ctx, bookings, p => AddRoomResource(p, nameof(SharedBooking.RoomIds)));

            var columns = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots");

            Assert.Single(columns[0].QuerySelectorAll(".rz-event"));
            Assert.Empty(columns[1].QuerySelectorAll(".rz-event"));
            Assert.Single(columns[2].QuerySelectorAll(".rz-event"));
        }

        [Fact]
        public void DayView_Not_Grouped_Renders_Default_Day_View()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), p => AddRoomResource(p), groupByResource: false);

            Assert.Empty(cut.FindAll(".rz-resource-view"));
            Assert.Single(cut.FindAll(".rz-day-view"));
        }

        [Fact]
        public void DayView_Grouped_Without_Resources_Renders_Default_Day_View()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), p => { });

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

            var cut = Render<Booking, RadzenWeekView>(ctx, bookings, p => AddRoomResource(p));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            Assert.Equal(3, groups.Count);
            Assert.Equal(new[] { "Room A", "Room B", "Room C" }, groups.Select(g => g.QuerySelector(".rz-resource-group-header")!.TextContent.Trim()));

            Assert.Contains("Standup", groups[0].QuerySelector(".rz-event-content")?.TextContent);
            Assert.Null(groups[1].QuerySelector(".rz-event-content"));
            Assert.Contains("Review", groups[2].QuerySelector(".rz-event-content")?.TextContent);
        }

        [Fact]
        public void WeekView_Grouped_By_Two_Resource_Types_Renders_Nested_Sections()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), p => { AddRoomResource(p); AddEmployeeResource(p); });

            var headers = cut.FindAll(".rz-resource-groups .rz-resource-group-header");

            Assert.Equal(3 + 3 * 2, headers.Count);

            var nested = cut.FindAll(".rz-resource-groups .rz-resource-group .rz-resource-group-children .rz-resource-group");

            Assert.Equal(6, nested.Count);
        }

        [Fact]
        public void WeekView_GroupBy_Renders_Sections_For_Specified_Resource_Type_Only()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), p => { AddRoomResource(p); AddEmployeeResource(p); },
                configureView: v => v.Add(x => x.GroupBy, "Employee"));

            var headers = cut.FindAll(".rz-resource-groups .rz-resource-group-header");

            Assert.Equal(new[] { "Nancy", "Andrew" }, headers.Select(h => h.TextContent.Trim()));
        }

        [Fact]
        public void WeekView_Grouped_Horizontal_Orientation_Renders_Groups_Side_By_Side()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), p => AddRoomResource(p),
                configureView: v => v.Add(x => x.GroupOrientation, Orientation.Horizontal));

            Assert.Single(cut.FindAll(".rz-resource-groups.rz-resource-groups-horizontal"));
        }

        [Fact]
        public void DayView_Grouped_AllDayProperty_Places_Appointment_In_AllDay_Row()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, AllDay = true, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Off-site" }
            };

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, p => AddRoomResource(p),
                configureScheduler: p => p.Add(x => x.AllDayProperty, nameof(Booking.AllDay)));

            Assert.Contains("Off-site", cut.FindAll(".rz-resource-all-day .rz-resource-all-day-slot")[0].TextContent);
            Assert.Empty(cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots")[0].QuerySelectorAll(".rz-event"));
        }

        [Fact]
        public void DayView_Grouped_Empty_AllDay_Cell_Click_Raises_SlotSelect()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, AllDay = true, Start = Today, End = Today.AddDays(1), Text = "Off-site" }
            };

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, p => AddRoomResource(p),
                configureScheduler: p =>
                {
                    p.Add(x => x.AllDayProperty, nameof(Booking.AllDay));
                    p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; });
                });

            var cell = cut.FindAll(".rz-resource-all-day .rz-resource-all-day-slot")[0];

            cell.QuerySelector(".rz-events")!.Click();

            Assert.NotNull(slotSelectArgs);
            Assert.Equal(Today, slotSelectArgs!.Start);
            Assert.Equal(Today.AddDays(1), slotSelectArgs.End);
        }

        [Fact]
        public void WeekView_Grouped_Renders_AllDay_Row_With_Cell_Per_Day()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, AllDay = true, Start = Today, End = Today.AddDays(1), Text = "Off-site" },
                new() { RoomId = 1, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenWeekView>(ctx, bookings, p => AddRoomResource(p),
                configureScheduler: p => p.Add(x => x.AllDayProperty, nameof(Booking.AllDay)));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");
            var allDayCells = groups[0].QuerySelectorAll(".rz-resource-all-day .rz-resource-all-day-slot");

            Assert.Equal(7, allDayCells.Length);
            Assert.Contains("Off-site", string.Join("", allDayCells.Select(c => c.TextContent)));
            Assert.DoesNotContain("Off-site", string.Join("", groups[0].QuerySelectorAll(".rz-slots .rz-event").Select(e => e.TextContent)));
            Assert.Contains("Standup", string.Join("", groups[0].QuerySelectorAll(".rz-slots .rz-event").Select(e => e.TextContent)));
        }

        [Fact]
        public void WeekView_Grouped_AllDay_Cell_Click_Selects_Day_With_Resource()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), p => AddRoomResource(p),
                configureScheduler: p => p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; }));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");
            var cell = groups[1].QuerySelectorAll(".rz-resource-all-day .rz-resource-all-day-slot")[1];

            cell.Click();

            Assert.NotNull(slotSelectArgs);
            Assert.Equal(slotSelectArgs!.Start.Date, slotSelectArgs.Start);
            Assert.Equal(slotSelectArgs.Start.AddDays(1), slotSelectArgs.End);
            Assert.Equal(2, Assert.IsType<Room>(slotSelectArgs.Resource).Id);
        }

        [Fact]
        public void WeekView_Grouped_Hides_AllDay_Row_When_ShowAllDay_Is_False()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), p => AddRoomResource(p),
                configureView: v => v.Add(x => x.ShowAllDay, false));

            Assert.Empty(cut.FindAll(".rz-resource-all-day"));
        }

        [Fact]
        public void WeekView_Grouped_Horizontal_Renders_Hours_Only_In_First_Group()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), p => AddRoomResource(p),
                configureView: v => v.Add(x => x.GroupOrientation, Orientation.Horizontal));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            Assert.NotNull(groups[0].QuerySelector(".rz-slot-hours"));
            Assert.Null(groups[1].QuerySelector(".rz-slot-hours"));
            Assert.Null(groups[2].QuerySelector(".rz-slot-hours"));
        }

        [Fact]
        public void WeekView_Grouped_Vertical_Renders_Hours_In_Every_Group()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), p => AddRoomResource(p),
                configureView: v => v.Add(x => x.GroupOrientation, Orientation.Vertical));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            Assert.All(groups, group => Assert.NotNull(group.QuerySelector(".rz-slot-hours")));
        }

        [Fact]
        public void WeekView_Grouped_SlotSelect_Provides_Resources()
        {
            using var ctx = new TestContext();

            SchedulerSlotSelectEventArgs? slotSelectArgs = null;

            var cut = Render<Booking, RadzenWeekView>(ctx, new List<Booking>(), p => AddRoomResource(p),
                configureScheduler: p => p.Add(x => x.SlotSelect, args => { slotSelectArgs = args; }));

            var groups = cut.FindAll(".rz-resource-groups .rz-resource-group");

            groups[1].QuerySelectorAll(".rz-slot").First().Click();

            Assert.NotNull(slotSelectArgs);
            Assert.Equal(2, Assert.IsType<Room>(slotSelectArgs!.Resource).Id);
            Assert.Equal(2, Assert.IsType<Room>(slotSelectArgs.Resources!["Room"]).Id);
        }

        [Fact]
        public void MonthView_Grouped_SlotRender_Provides_Resources()
        {
            using var ctx = new TestContext();

            var resources = new HashSet<int>();

            Render<Booking, RadzenMonthView>(ctx, new List<Booking>(), p => AddRoomResource(p),
                configureScheduler: p => p.Add(x => x.SlotRender, args =>
                {
                    if (args.Resources?.TryGetValue("Room", out var room) == true && room is Room typed)
                    {
                        resources.Add(typed.Id);
                    }
                }));

            Assert.Equal(new HashSet<int> { 1, 2, 3 }, resources);
        }

        [Fact]
        public async System.Threading.Tasks.Task DayView_Grouped_AppointmentMove_Provides_Target_Resources()
        {
            using var ctx = new TestContext();

            SchedulerAppointmentMoveEventArgs? moveArgs = null;

            var bookings = new List<Booking>
            {
                new() { RoomId = 1, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenDayView>(ctx, bookings, p => AddRoomResource(p),
                configureScheduler: p => p.Add(x => x.AppointmentMove, args => { moveArgs = args; }));

            var view = cut.FindComponent<ResourceDayView>();
            var appointment = cut.Instance.GetAppointmentsInRange(Today.AddHours(8), Today.AddHours(24)).First();

            await cut.InvokeAsync(() => view.Instance.OnAppointmentDragStart(appointment));

            var targetSlot = cut.FindAll(".rz-resource-view .rz-resource-view-content .rz-slots")[2].QuerySelectorAll(".rz-slot").First();

            await targetSlot.DropAsync(new Microsoft.AspNetCore.Components.Web.DragEventArgs());

            Assert.NotNull(moveArgs);
            Assert.Equal(3, Assert.IsType<Room>(moveArgs!.Resource).Id);
            Assert.Equal(3, Assert.IsType<Room>(moveArgs.Resources!["Room"]).Id);
        }

        [Fact]
        public void ResourceType_HeaderTemplate_Is_Used_For_Group_Headers()
        {
            using var ctx = new TestContext();

            Microsoft.AspNetCore.Components.RenderFragment<object> template = resource => builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddContent(1, $"#{((Room)resource).Id}");
                builder.CloseElement();
            };

            Setup(ctx);

            var cut = ctx.RenderComponent<RadzenScheduler<Booking>>(p =>
            {
                p.Add(x => x.Culture, CultureInfo.InvariantCulture);
                p.Add(x => x.Date, Today);
                p.Add(x => x.Data, new List<Booking>());
                p.Add(x => x.StartProperty, "Start");
                p.Add(x => x.EndProperty, "End");
                p.Add(x => x.TextProperty, "Text");
                p.AddChildContent<RadzenSchedulerResource>(r =>
                {
                    r.Add(x => x.Name, "Room");
                    r.Add(x => x.Data, Rooms);
                    r.Add(x => x.Property, nameof(Booking.RoomId));
                    r.Add(x => x.TextProperty, nameof(Room.Name));
                    r.Add(x => x.ValueProperty, nameof(Room.Id));
                    r.Add(x => x.HeaderTemplate, template);
                });
                p.AddChildContent<RadzenWeekView>(v => v.Add(x => x.GroupByResource, true));
            });

            var headers = cut.FindAll(".rz-resource-group-header strong");

            Assert.Equal(3, headers.Count);
            Assert.Equal("#1", headers[0].TextContent);
        }

        [Fact]
        public void DayView_Grouped_Has_Localized_AllDay_Label()
        {
            using var ctx = new TestContext();

            var cut = Render<Booking, RadzenDayView>(ctx, new List<Booking>(), p => AddRoomResource(p));

            Assert.Contains("All day", cut.Find(".rz-resource-all-day-label").TextContent);
        }

        [Fact]
        public void Existing_Views_Render_Unchanged_When_Scheduler_Has_Resources_But_View_Does_Not_Group()
        {
            using var ctx = new TestContext();

            var bookings = new List<Booking>
            {
                new() { RoomId = 2, Start = Today.AddHours(10), End = Today.AddHours(11), Text = "Standup" }
            };

            var cut = Render<Booking, RadzenWeekView>(ctx, bookings, p => AddRoomResource(p), groupByResource: false);

            Assert.Empty(cut.FindAll(".rz-resource-groups"));
            Assert.Empty(cut.FindAll(".rz-resource-all-day"));
            Assert.Single(cut.FindAll(".rz-week-view"));
            Assert.Single(cut.FindAll(".rz-event"));
        }
    }
}
