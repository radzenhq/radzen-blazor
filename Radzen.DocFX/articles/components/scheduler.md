# Scheduler component
This article demonstrates how to use RadzenScheduler.

## Basic usage
RadzenScheduler can display appointments in three ways (a.k.a. views) - day view, week view and month view.
The following properties configure the scheduler.
- Data - specifies the data source which contains the appointments - a collection of objects.
- StartProperty - the name of the property which contains the start date of an appointment. The property should be of `DateTime` type.
- EndProperty - the name of the property which contains the end date of an appointment. The property should be of `DateTime` type.
- TextProperty - the name of the property which contains the text of an appointment - this is the message that the scheduler will display. The property should be of `String` type.

Here is a minimal example.
```
<RadzenScheduler Data="@data" TItem="DataItem" StartProperty="Start" EndProperty="End" TextProperty="Text">
  <RadzenMonthView />
</RadzenScheduler>
@code {
  class DataItem
  {
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Text { get; set; }
  }

  DataItem[] data = new DataItem[]
  {
      new DataItem
      {
        Start = DateTime.Today,
        End = DateTime.Today.AddDays(1),
        Text = "Birthday"
      },
  };
}
```
The `RadzenMonthView` tag is used to specify that the scheduler will display appointments in month view. The `Data` property specifies the data source. The scheduler will render an appointment for every `DataItem` instance from the `data` array and will display its `Text` property.

## CRUD
To support adding and editing of appointments you can handle the `SlotSelect` and `AppointmentSelect` events.
Check the [online example](/scheduler) for a complete implementation.

> [!IMPORTANT] 
> To make the scheduler display the latest changes you should do one of the following:
> - invoke the `Reload()` method of the scheduler
> - Reassign its `Data` property to a new `IEnumerable<T>` instance.

## Customize appointment appearance

To customize the appointment appearance use the `Template` setting of RadzenScheduler.
```
<RadzenScheduler Data="@data" TItem="DataItem" StartProperty="Start" EndProperty="End" TextProperty="Text">
  <Template Context="data">
    <strong>@data.Text</strong>
  </Template>
  <ChildContent>
    <RadzenMonthView />
  </ChildContent>
</RadzenScheduler>
```
The template context is the data item (of type `TItem`).
## Views
To add more views simply list them as child tags within RadzenScheduler.
```
<RadzenScheduler Data="@data" TItem="DataItem" StartProperty="Start" EndProperty="End" TextProperty="Text">
  <RadzenDayView />
  <RadzenWeekView />
  <RadzenMonthView />
</RadzenScheduler>
```
To set the initially selected view use the `SelectedIndex` property. By default RadzenScheduler displays the first view.
```
<RadzenScheduler SelectedIndex="1" Data="@data" TItem="DataItem" StartProperty="Start" EndProperty="End" TextProperty="Text">
  <RadzenDayView />
  <RadzenWeekView />
  <RadzenMonthView />
</RadzenScheduler>
```
## LoadData event
If you set the Data property of the scheduler to `IQueryable<T>` it will use expression trees that
Entity Framework core will automatically convert to SQL. This will ensure that only the appointments
required for the time period displayed by the current view are fetched from the database.

For custom implementations you can use the `LoadData` event. It provides the `Start` and `End` of the current period.
```
<RadzenScheduler LoadData="@OnLoadData" Data="@data" TItem="DataItem" StartProperty="Start" EndProperty="End" TextProperty="Text">
  <RadzenMonthView />
</RadzenScheduler>
@code {
  IEnumerable<DataItem> data;

  async Task OnLoadData(SchedulerLoadDataEventArgs args)
  {
    // Get the appointments for between the Start and End
    data = await MyAppointmentService.GetData(args.Start, args.End);
  }
}
```