# Scheduler

The Blazor Scheduler is a calendar that shows appointments in day, week, month, year planner, and timeline views, with event editing, tooltips, and multi-day layouts.

Keywords: scheduler, calendar, event, appointment

> API reference: [RadzenScheduler API](https://blazor.radzen.com/api/scheduler.md)

## Examples

## Blazor Scheduler

The Blazor Scheduler is a calendar component with day, week, month, year planner, and timeline views, appointment editing, and tooltips.

### Day, week and month views


```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);height:4rem;">
    <RadzenLabel Text="Show Header:  " />
    <RadzenSwitch @bind-Value=@showHeader />
</RadzenStack>

<RadzenScheduler @ref=@scheduler SlotRender=@OnSlotRender style="height: 768px;" TItem="Appointment" Data=@appointments StartProperty="Start" EndProperty="End" ShowHeader=@showHeader
                 TextProperty="Text" SelectedIndex="2"
    SlotSelect=@OnSlotSelect AppointmentSelect=@OnAppointmentSelect AppointmentRender=@OnAppointmentRender DaySelect="@OnDaySelect"
    AppointmentMove=@OnAppointmentMove >
    <RadzenDayView />
    <RadzenWeekView />
    <RadzenMonthView />
</RadzenScheduler>

<EventConsole @ref=@console />

@code {
    RadzenScheduler<Appointment> scheduler;
    EventConsole console;
    Dictionary<DateTime, string> events = new Dictionary<DateTime, string>();

    bool showHeader = true;

    IList<Appointment> appointments = new List<Appointment>
    {
        new Appointment { Start = DateTime.Today.AddDays(-2), End = DateTime.Today.AddDays(-2), Text = "Birthday" },
        new Appointment { Start = DateTime.Today.AddDays(-11), End = DateTime.Today.AddDays(-10), Text = "Day off" },
        new Appointment { Start = DateTime.Today.AddDays(-10), End = DateTime.Today.AddDays(-8), Text = "Work from home" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Online meeting" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(13), Text = "Skype call" },
        new Appointment { Start = DateTime.Today.AddHours(14), End = DateTime.Today.AddHours(14).AddMinutes(30), Text = "Dentist appointment" },
        new Appointment { Start = DateTime.Today.AddHours(15), End = DateTime.Today.AddHours(16), Text = "Team standup" },
        new Appointment { Start = DateTime.Today.AddHours(16), End = DateTime.Today.AddHours(17), Text = "Code review" },
        new Appointment { Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(12), Text = "Vacation" },
    };

    void OnDaySelect(SchedulerDaySelectEventArgs args)
    {
        console.Log($"DaySelect: Day={args.Day} AppointmentCount={args.Appointments.Count()}");
    }

    void OnSlotRender(SchedulerSlotRenderEventArgs args)
    {
        // Highlight today in month view
        if (args.View.Text == "Month" && args.Start.Date == DateTime.Today)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }

        // Highlight working hours (9-18)
        if ((args.View.Text == "Week" || args.View.Text == "Day") && args.Start.Hour > 8 && args.Start.Hour < 19)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }
    }

    async Task OnSlotSelect(SchedulerSlotSelectEventArgs args)
    {
        console.Log($"SlotSelect: Start={args.Start} End={args.End}");

        if (args.View.Text != "Year")
        {
            Appointment data = await DialogService.OpenAsync<AddAppointmentPage>("Add Appointment",
                new Dictionary<string, object> { { "Start", args.Start }, { "End", args.End } });

            if (data != null)
            {
                appointments.Add(data);
                // Either call the Reload method or reassign the Data property of the Scheduler
                await scheduler.Reload();
            }
        }
    }

    async Task OnAppointmentSelect(SchedulerAppointmentSelectEventArgs<Appointment> args)
    {
        console.Log($"AppointmentSelect: Appointment={args.Data.Text}");

        var copy = new Appointment
        {
            Start = args.Data.Start,
            End = args.Data.End,
            Text = args.Data.Text
        };

        var data = await DialogService.OpenAsync<EditAppointmentPage>("Edit Appointment", new Dictionary<string, object> { { "Appointment", copy } });

        if (data != null)
        {
            // Update the appointment
            args.Data.Start = data.Start;
            args.Data.End = data.End;
            args.Data.Text = data.Text;
        }

        await scheduler.Reload();
    }

    void OnAppointmentRender(SchedulerAppointmentRenderEventArgs<Appointment> args)
    {
        // Never call StateHasChanged in AppointmentRender - would lead to infinite loop

        if (args.Data.Text == "Birthday")
        {
            args.Attributes["style"] = "background: red";
        }
    }

    async Task OnAppointmentMove(SchedulerAppointmentMoveEventArgs args)
    {
        var draggedAppointment = appointments.FirstOrDefault(x => x == args.Appointment.Data);

        if (draggedAppointment != null)
        {
            var duration = draggedAppointment.End - draggedAppointment.Start;

            if (args.SlotDate.TimeOfDay == TimeSpan.Zero)
            {
                draggedAppointment.Start = args.SlotDate.Date.Add(draggedAppointment.Start.TimeOfDay);
            }
            else
            {
                draggedAppointment.Start = args.SlotDate;
            }

            draggedAppointment.End = draggedAppointment.Start.Add(duration);

            await scheduler.Reload();
        }
    }
}
```


### Year Planner and Timeline views

Plan out the whole year with `&lt;RadzenYearPlannerView /&gt;` and `&lt;RadzenYearTimelineView /&gt;`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
    <RadzenLabel Text="Schedule Start Month:" />
    <RadzenSelectBar @bind-Value="@startMonth" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Month)).Cast<Month>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-xl-flex" />
</RadzenStack>

<RadzenScheduler @ref=@scheduler SlotRender=@OnSlotRender style="height: 768px;" TItem="Appointment" Data=@appointments StartProperty="Start" EndProperty="End"
TextProperty="Text" SelectedIndex="1"
SlotSelect=@OnSlotSelect AppointmentSelect=@OnAppointmentSelect AppointmentRender=@OnAppointmentRender MonthSelect=@OnMonthSelect>
    <RadzenMonthView />
    <RadzenYearPlannerView StartMonth="@startMonth" />
    <RadzenYearTimelineView StartMonth="@startMonth" />
    <RadzenYearView StartMonth="@startMonth" />
</RadzenScheduler>

<EventConsole @ref=@console />

@code {
    RadzenScheduler<Appointment> scheduler;
    EventConsole console;
    Dictionary<DateTime, string> events = new Dictionary<DateTime, string>();
    Month startMonth = Month.January;

    IList<Appointment> appointments = new List<Appointment>
    {
        new Appointment { Start = DateTime.Today.AddDays(-2), End = DateTime.Today.AddDays(-2), Text = "Birthday" },
        new Appointment { Start = DateTime.Today.AddDays(-11), End = DateTime.Today.AddDays(-10), Text = "Day off" },
        new Appointment { Start = DateTime.Today.AddDays(-10), End = DateTime.Today.AddDays(-8), Text = "Work from home" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Online meeting" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(13), Text = "Skype call" },
        new Appointment { Start = DateTime.Today.AddHours(14), End = DateTime.Today.AddHours(14).AddMinutes(30), Text = "Dentist appointment" },
        new Appointment { Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(7), Text = "Vacation" },
        new Appointment { Start = DateTime.Today.AddDays(21).AddHours(10), End = DateTime.Today.AddDays(21).AddHours(11), Text = "Client #1" },
        new Appointment { Start = DateTime.Today.AddDays(21).AddHours(11), End = DateTime.Today.AddDays(21).AddHours(12), Text = "Client #2" },
        new Appointment { Start = DateTime.Today.AddDays(21).AddHours(12), End = DateTime.Today.AddDays(21).AddHours(13), Text = "Client #3" },
        new Appointment { Start = DateTime.Today.AddDays(21).AddHours(13), End = DateTime.Today.AddDays(21).AddHours(14), Text = "Client #4" },
        new Appointment { Start = DateTime.Today.AddDays(17).AddHours(10), End = DateTime.Today.AddDays(17).AddHours(11), Text = "Client #1" },
        new Appointment { Start = DateTime.Today.AddDays(17).AddHours(11), End = DateTime.Today.AddDays(17).AddHours(12), Text = "Client #2" },
        new Appointment { Start = DateTime.Today.AddDays(18).AddHours(10), End = DateTime.Today.AddDays(18).AddHours(11), Text = "Client #1" },
        new Appointment { Start = DateTime.Today.AddDays(18).AddHours(11), End = DateTime.Today.AddDays(18).AddHours(12), Text = "Client #2" },
        new Appointment { Start = DateTime.Today.AddDays(18).AddHours(12), End = DateTime.Today.AddDays(18).AddHours(13), Text = "Client #3" },
        new Appointment { Start = DateTime.Today.AddDays(18).AddHours(13), End = DateTime.Today.AddDays(18).AddHours(14), Text = "Client #4" },
        new Appointment { Start = DateTime.Today.AddDays(18).AddHours(14), End = DateTime.Today.AddDays(23).AddHours(15), Text = "Client #5" },
        new Appointment { Start = DateTime.Today.AddDays(6).AddHours(10), End = DateTime.Today.AddDays(6).AddHours(11), Text = "Client #1" },
        new Appointment { Start = DateTime.Today.AddDays(6).AddHours(11), End = DateTime.Today.AddDays(6).AddHours(12), Text = "Client #2" },
        new Appointment { Start = DateTime.Today.AddDays(6).AddHours(12), End = DateTime.Today.AddDays(6).AddHours(13), Text = "Client #3" },
        new Appointment { Start = DateTime.Today.AddDays(6).AddHours(13), End = DateTime.Today.AddDays(6).AddHours(14), Text = "Client #4" },
        new Appointment { Start = DateTime.Today.AddDays(7).AddHours(10), End = DateTime.Today.AddDays(7).AddHours(11), Text = "Client #1" },
        new Appointment { Start = DateTime.Today.AddDays(7).AddHours(11), End = DateTime.Today.AddDays(7).AddHours(12), Text = "Client #2" },
        new Appointment { Start = DateTime.Today.AddDays(8).AddHours(10), End = DateTime.Today.AddDays(8).AddHours(11), Text = "Client #1" },
        new Appointment { Start = DateTime.Today.AddDays(8).AddHours(11), End = DateTime.Today.AddDays(8).AddHours(12), Text = "Client #2" },
        new Appointment { Start = DateTime.Today.AddDays(9).AddHours(10), End = DateTime.Today.AddDays(9).AddHours(11), Text = "Client #1" },
        new Appointment { Start = DateTime.Today.AddDays(9).AddHours(11), End = DateTime.Today.AddDays(9).AddHours(12), Text = "Client #2" },
        new Appointment { Start = DateTime.Today.AddDays(13).AddHours(10), End = DateTime.Today.AddDays(16).AddHours(11), Text = "Client #1" },
        new Appointment { Start = DateTime.Today.AddDays(13).AddHours(11), End = DateTime.Today.AddDays(16).AddHours(12), Text = "Client #2" },
        new Appointment { Start = DateTime.Today.AddDays(13).AddHours(12), End = DateTime.Today.AddDays(13).AddHours(13), Text = "Client #3" },
        new Appointment { Start = DateTime.Today.AddDays(13).AddHours(13), End = DateTime.Today.AddDays(16).AddHours(14), Text = "Client #4" },
        new Appointment { Start = DateTime.Today.AddDays(13).AddHours(14), End = DateTime.Today.AddDays(16).AddHours(15), Text = "Client #5" },
    };

    void OnSlotRender(SchedulerSlotRenderEventArgs args)
    {
        // Highlight today in month view
        if (args.View.Text == "Month" && args.Start.Date == DateTime.Today)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }

        // Draw a line for new year if start month is not January
        if ((args.View.Text == "Planner" || args.View.Text == "Timeline") && args.Start.Month == 12 && startMonth != Month.January)
        {
            args.Attributes["style"] = "border-bottom: thick double var(--rz-base-600);";
        }

        // Color the slot depending on number of appointments for that day
        if (args.Appointments.Count() > 4)
        {
            args.Attributes["style"] = "background: #ffabab;";
        }
        else if (args.Appointments.Count() > 2)
        {
            args.Attributes["style"] = "background: #ffc988;";
        }
        else if (args.Appointments.Count() > 0)
        {
            args.Attributes["style"] = "background: #c3ffc3;";
        }
    }

    async Task OnSlotSelect(SchedulerSlotSelectEventArgs args)
    {
        console.Log($"SlotSelect: Start={args.Start} End={args.End}");

        if(args.View.Text != "Year")
        {            
            Appointment data = await DialogService.OpenAsync<AddAppointmentPage>("Add Appointment",
                new Dictionary<string, object> { { "Start", args.Start }, { "End", args.End } });

            if (data != null)
            {
                appointments.Add(data);
                // Either call the Reload method or reassign the Data property of the Scheduler
                await scheduler.Reload();
            }
        }
    }

    async Task OnMonthSelect(SchedulerMonthSelectEventArgs args)
    {
        console.Log($"MonthSelect: MonthStart={args.MonthStart} AppointmentCount={args.Appointments.Count()}");
        await Task.CompletedTask;
    }

    async Task OnAppointmentSelect(SchedulerAppointmentSelectEventArgs<Appointment> args)
    {
        console.Log($"AppointmentSelect: Appointment={args.Data.Text}");

        await DialogService.OpenAsync<EditAppointmentPage>("Edit Appointment", new Dictionary<string, object> { { "Appointment", args.Data } });

        await scheduler.Reload();
    }

    void OnAppointmentRender(SchedulerAppointmentRenderEventArgs<Appointment> args)
    {
        // Never call StateHasChanged in AppointmentRender - would lead to infinite loop

        if (args.Data.Text == "Birthday")
        {
            args.Attributes["style"] = "background: red";
        }
    }
}
```


### Display additional content when the user hovers an appointment

Use the `SlotRender` event and templates to display tooltips with additional appointment details on hover.

```razor
<RadzenScheduler style="height: 768px;" TItem="Appointment" Data=@appointments StartProperty="Start" EndProperty="End"
    TextProperty="Text" SelectedIndex="2" AppointmentMouseEnter=@OnAppointmentMouseEnter AppointmentMouseLeave=@OnAppointmentMouseLeave>
    <RadzenDayView />
    <RadzenWeekView />
    <RadzenMonthView />
    <RadzenYearView />
</RadzenScheduler>

@code {
    IList<Appointment> appointments = new List<Appointment>
    {
        new Appointment { Start = DateTime.Today.AddDays(-2), End = DateTime.Today.AddDays(-2), Text = "Birthday" },
        new Appointment { Start = DateTime.Today.AddDays(-11), End = DateTime.Today.AddDays(-10), Text = "Day off" },
        new Appointment { Start = DateTime.Today.AddDays(-10), End = DateTime.Today.AddDays(-8), Text = "Work from home" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Online meeting" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(13), Text = "Skype call" },
        new Appointment { Start = DateTime.Today.AddHours(14), End = DateTime.Today.AddHours(14).AddMinutes(30), Text = "Dentist appointment" },
        new Appointment { Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(12), Text = "Vacation" },
    };

    void OnAppointmentMouseEnter(SchedulerAppointmentMouseEventArgs<Appointment> args)
    {
TooltipService.Open(args.Element, ts =>
@<RadzenStack Orientation="Orientation.Vertical" Gap="0" class="rz-p-6" Style="min-width: 250px; max-width: 500px;">
    <RadzenText TextStyle="TextStyle.H4" TagName="TagName.P" class="rz-mb-4" Style="color: var(--rz-tooltip-color);">
        @args.Data.Text
    </RadzenText>
    <RadzenStack Orientation="Orientation.Horizontal" Gap="4px">
        <RadzenText TextStyle="TextStyle.Body2" Style="color: var(--rz-tooltip-color); width: 44px;">
            <strong>Start</strong>
        </RadzenText>
        <RadzenText TextStyle="TextStyle.Body2" Style="color: var(--rz-tooltip-color);">
            @args.Data.Start.ToString("hh:mm ⋅ dddd, MMMM d")
        </RadzenText>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Gap="4px">
        <RadzenText TextStyle="TextStyle.Body2" Style="color: var(--rz-tooltip-color); width: 44px;">
            <strong>End</strong>
        </RadzenText>
        <RadzenText TextStyle="TextStyle.Body2" Style="color: var(--rz-tooltip-color);">
            @args.Data.End.ToString("hh:mm ⋅ dddd, MMMM d")
        </RadzenText>
    </RadzenStack>
</RadzenStack>, new TooltipOptions { Position = TooltipPosition.Right, Duration = null });
    }

    void OnAppointmentMouseLeave(SchedulerAppointmentMouseEventArgs<Appointment> args)
    {
        TooltipService.Close();
    }
}
```


### Display any number of days side-by-side

Configure the scheduler to display multiple days simultaneously for better week or multi-day planning views.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);height:4rem;">
    <RadzenLabel Text="Number of days to view:" />
    <RadzenSlider @bind-Value=@sliderNumberOfDays TValue="int" Min="1" Max="14" Style="margin-left:1.5rem;margin-right:1.5rem;" Change="NumberOfDaysChange" />
    <RadzenLabel Text="@($"{sliderNumberOfDays}")" />
    @if(sliderNumberOfDays==2)
    {
        <RadzenText TextStyle="TextStyle.Caption" Text="@($"(default)")" Style="margin-left:0.1rem;margin-top:0.5rem;" />
    }
    <div class="rz-w-25" />
    <RadzenLabel Text="Advance days:" />
    <RadzenSlider @bind-Value=@sliderAdvanceDays TValue="int" Min="1" Max="14" Style="margin-left:1.5rem;margin-right:1.5rem;" />
    <RadzenLabel Text="@($"{sliderAdvanceDays}")" />
    @if (sliderAdvanceDays == 1)
    {
        <RadzenText TextStyle="TextStyle.Caption" Text="@($"(default)")" Style="margin-left:0.1rem;margin-top:0.5rem;" />
    }
</RadzenStack>

<RadzenScheduler @ref=@scheduler SlotRender=@OnSlotRender style="height: 768px;" TItem="Appointment" Data=@appointments StartProperty="Start" EndProperty="End"
TextProperty="Text" SelectedIndex="2"
SlotSelect=@OnSlotSelect AppointmentSelect=@OnAppointmentSelect AppointmentRender=@OnAppointmentRender
AppointmentMove=@OnAppointmentMove >
    <RadzenDayView />
    <RadzenWeekView />
    <RadzenMultiDayView NumberOfDays="@sliderNumberOfDays" AdvanceDays="@sliderAdvanceDays" />
    <RadzenMonthView />
</RadzenScheduler>

<EventConsole @ref=@console />

@code {
    RadzenScheduler<Appointment> scheduler;
    EventConsole console;
    Dictionary<DateTime, string> events = new Dictionary<DateTime, string>();

    int sliderNumberOfDays { get; set; } = 2;
    int sliderAdvanceDays { get; set; } = 1;

    IList<Appointment> appointments = new List<Appointment>
    {
        new Appointment { Start = DateTime.Today.AddDays(-2), End = DateTime.Today.AddDays(-2), Text = "Birthday" },
        new Appointment { Start = DateTime.Today.AddDays(-11), End = DateTime.Today.AddDays(-10), Text = "Day off" },
        new Appointment { Start = DateTime.Today.AddDays(-10), End = DateTime.Today.AddDays(-8), Text = "Work from home" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Online meeting" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(13), Text = "Skype call" },
        new Appointment { Start = DateTime.Today.AddHours(14), End = DateTime.Today.AddHours(14).AddMinutes(30), Text = "Dentist appointment" },
        new Appointment { Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(12), Text = "Vacation" },
    };

    void NumberOfDaysChange()
    {
        StateHasChanged();
    }

    void OnSlotRender(SchedulerSlotRenderEventArgs args)
    {
        // Highlight today in month view
        if (args.View.Text == "Month" && args.Start.Date == DateTime.Today)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }

        // Highlight working hours (9-18)
        if ((args.View.Text == "Week" || args.View.Text == "Day") && args.Start.Hour > 8 && args.Start.Hour < 19)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }
    }

    async Task OnSlotSelect(SchedulerSlotSelectEventArgs args)
    {
        console.Log($"SlotSelect: Start={args.Start} End={args.End}");

        if (args.View.Text != "Year")
        {
            Appointment data = await DialogService.OpenAsync<AddAppointmentPage>("Add Appointment",
                new Dictionary<string, object> { { "Start", args.Start }, { "End", args.End } });

            if (data != null)
            {
                appointments.Add(data);
                // Either call the Reload method or reassign the Data property of the Scheduler
                await scheduler.Reload();
            }
        }
    }

    async Task OnAppointmentSelect(SchedulerAppointmentSelectEventArgs<Appointment> args)
    {
        console.Log($"AppointmentSelect: Appointment={args.Data.Text}");

        var copy = new Appointment
        {
            Start = args.Data.Start,
            End = args.Data.End,
            Text = args.Data.Text
        };

        var data = await DialogService.OpenAsync<EditAppointmentPage>("Edit Appointment", new Dictionary<string, object> { { "Appointment", copy } });

        if (data != null)
        {
            // Update the appointment
            args.Data.Start = data.Start;
            args.Data.End = data.End;
            args.Data.Text = data.Text;
        }

        await scheduler.Reload();
    }

    void OnAppointmentRender(SchedulerAppointmentRenderEventArgs<Appointment> args)
    {
        // Never call StateHasChanged in AppointmentRender - would lead to infinite loop

        if (args.Data.Text == "Birthday")
        {
            args.Attributes["style"] = "background: red";
        }
    }

    async Task OnAppointmentMove(SchedulerAppointmentMoveEventArgs args)
    {
        var draggedAppointment = appointments.FirstOrDefault(x => x == args.Appointment.Data);

        if (draggedAppointment != null)
        {
            draggedAppointment.Start = draggedAppointment.Start + args.TimeSpan;

            draggedAppointment.End = draggedAppointment.End + args.TimeSpan;

            await scheduler.Reload();
        }
    }
}
```


### Display scheduler as an agenda

Configure the scheduler to display appointments in an agenda view for better overview of upcoming events.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);height:4rem;">
    <RadzenLabel Text="Number of days to view:" />
    <RadzenSlider @bind-Value=@sliderNumberOfDays TValue="int" Min="1" Max="7" Style="margin-left:1.5rem;margin-right:1.5rem;" Change="NumberOfDaysChange" />
    <RadzenLabel Text="@($"{sliderNumberOfDays}")" />
    @if(sliderNumberOfDays==7)
    {
        <RadzenText TextStyle="TextStyle.Caption" Text="@($"(default)")" Style="margin-left:0.1rem;margin-top:0.5rem;" />
    }
</RadzenStack>

<RadzenScheduler @ref=@scheduler SlotRender=@OnSlotRender style="height: 768px;" TItem="Appointment" Data=@appointments StartProperty="Start" EndProperty="End"
TextProperty="Text" SelectedIndex="0"
SlotSelect=@OnSlotSelect AppointmentSelect=@OnAppointmentSelect AppointmentRender=@OnAppointmentRender
AppointmentMove=@OnAppointmentMove >
    <RadzenAgendaView NumberOfDays="@sliderNumberOfDays" MultiDayText="Spans more than one day" />
    <RadzenDayView />
    <RadzenWeekView />
    <RadzenMonthView />
</RadzenScheduler>

<EventConsole @ref=@console />

@code {
    RadzenScheduler<Appointment> scheduler;
    EventConsole console;
    Dictionary<DateTime, string> events = new Dictionary<DateTime, string>();

    int sliderNumberOfDays { get; set; } = 7;

    IList<Appointment> appointments = new List<Appointment>
    {
        new Appointment { Start = DateTime.Today.AddDays(-2), End = DateTime.Today.AddDays(-2), Text = "Birthday" },
        new Appointment { Start = DateTime.Today.AddDays(-11), End = DateTime.Today.AddDays(-10), Text = "Day off" },
        new Appointment { Start = DateTime.Today.AddDays(-10), End = DateTime.Today.AddDays(-8), Text = "Work from home" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(12), Text = "Online meeting" },
        new Appointment { Start = DateTime.Today.AddHours(10), End = DateTime.Today.AddHours(13), Text = "Skype call" },
        new Appointment { Start = DateTime.Today.AddHours(14), End = DateTime.Today.AddHours(14).AddMinutes(30), Text = "Dentist appointment" },
        new Appointment { Start = DateTime.Today.AddHours(15), End = DateTime.Today.AddHours(16), Text = "Team standup" },
        new Appointment { Start = DateTime.Today.AddHours(16), End = DateTime.Today.AddHours(17), Text = "Code review" },
        new Appointment { Start = DateTime.Today.AddDays(2).AddHours(9), End = DateTime.Today.AddDays(2).AddHours(14), Text = "Work from home" },
        new Appointment { Start = DateTime.Today.AddDays(3).AddHours(12), End = DateTime.Today.AddDays(3).AddHours(16), Text = "Donally Contract" },
        new Appointment { Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(12), Text = "Vacation" },
    };

    void NumberOfDaysChange()
    {
        StateHasChanged();
    }

    void OnSlotRender(SchedulerSlotRenderEventArgs args)
    {
        // Highlight today in month view
        if (args.View.Text == "Month" && args.Start.Date == DateTime.Today)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }

        // Highlight working hours (9-18)
        if ((args.View.Text == "Week" || args.View.Text == "Day") && args.Start.Hour > 8 && args.Start.Hour < 19)
        {
            args.Attributes["style"] = "background: var(--rz-scheduler-highlight-background-color, rgba(255,220,40,.2));";
        }
    }

    async Task OnSlotSelect(SchedulerSlotSelectEventArgs args)
    {
        console.Log($"SlotSelect: Start={args.Start} End={args.End}");

        if (args.View.Text != "Year")
        {
            Appointment data = await DialogService.OpenAsync<AddAppointmentPage>("Add Appointment",
                new Dictionary<string, object> { { "Start", args.Start }, { "End", args.End } });

            if (data != null)
            {
                appointments.Add(data);
                // Either call the Reload method or reassign the Data property of the Scheduler
                await scheduler.Reload();
            }
        }
    }

    async Task OnAppointmentSelect(SchedulerAppointmentSelectEventArgs<Appointment> args)
    {
        console.Log($"AppointmentSelect: Appointment={args.Data.Text}");

        var copy = new Appointment
        {
            Start = args.Data.Start,
            End = args.Data.End,
            Text = args.Data.Text
        };

        var data = await DialogService.OpenAsync<EditAppointmentPage>("Edit Appointment", new Dictionary<string, object> { { "Appointment", copy } });

        if (data != null)
        {
            // Update the appointment
            args.Data.Start = data.Start;
            args.Data.End = data.End;
            args.Data.Text = data.Text;
        }

        await scheduler.Reload();
    }

    void OnAppointmentRender(SchedulerAppointmentRenderEventArgs<Appointment> args)
    {
        // Never call StateHasChanged in AppointmentRender - would lead to infinite loop

        if (args.Data.Text == "Birthday")
        {
            args.Attributes["style"] = "background: red";
        }
    }

    async Task OnAppointmentMove(SchedulerAppointmentMoveEventArgs args)
    {
        var draggedAppointment = appointments.FirstOrDefault(x => x == args.Appointment.Data);

        if (draggedAppointment != null)
        {
            draggedAppointment.Start = draggedAppointment.Start + args.TimeSpan;

            draggedAppointment.End = draggedAppointment.End + args.TimeSpan;

            await scheduler.Reload();
        }
    }
}
```
