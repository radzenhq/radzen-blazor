# RadzenScheduler API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AppointmentRender | `Action<SchedulerAppointmentRenderEventArgs<TItem>>?` | An action that will be invoked when the current view renders an appointment. Never call StateHasChanged when handling AppointmentRender. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content of the scheduler. Use to specify what views to render. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable<TItem>?` | Gets or sets the data of RadzenScheduler. It will display an appointment for every item of the collection which is within the current view date range. |
| Date | `DateTime` | Gets or sets the initial date displayed by the selected view. Set to DateTime.Today by default. |
| EndProperty | `string?` | Specifies the property of which will set . |
| NavigationTemplate | `RenderFragment?` | Gets or sets the additional content to be rendered in place of the default navigation buttons in the scheduler. This property allows for complete customization of the navigation controls, replacing the native date navigation buttons (such as year, month, and day) with user-defined content or buttons. Use this to add custom controls or interactive elements that better suit your application's requirements. This requires that the ShowHeader parameter to be set to true (enabled by default). |
| NextText | `string` | Gets or sets the text of the next button. Set to Next by default. |
| PrevText | `string` | Gets or sets the text of the previous button. Set to Previous by default. |
| SelectedIndex | `int` | Specifies the initially selected view. |
| ShowDateTitle | `bool` | Gets or sets a value indicating whether the date title is visible. Set to true by default. |
| ShowHeader | `bool` | Specifies whether to Show or Hide the Scheduler Header. Defaults to true />. |
| ShowNavigationButtons | `bool` | Gets or sets a value indicating whether the previous and next navigation buttons are visible. Set to true by default. |
| ShowTodayButton | `bool` | Gets or sets a value indicating whether the today button is visible. Set to true by default. |
| SlotRender | `Action<SchedulerSlotRenderEventArgs>?` | An action that will be invoked when the current view renders an slot. Never call StateHasChanged when handling SlotRender. |
| StartProperty | `string?` | Specifies the property of which will set . |
| Style | `string?` | Gets or sets the inline CSS style. |
| Template | `RenderFragment<TItem>?` | Gets or sets the template used to render appointments. |
| TextProperty | `string?` | Specifies the property of which will set . |
| TodayText | `string` | Gets or sets the text of the today button. Set to Today by default. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| AppointmentMouseEnter | `EventCallback<SchedulerAppointmentMouseEventArgs<TItem>>` | A callback that will be invoked when the user moves the mouse over an appointment in the current view. |
| AppointmentMouseLeave | `EventCallback<SchedulerAppointmentMouseEventArgs<TItem>>` | A callback that will be invoked when the user moves the mouse out of an appointment in the current view. |
| AppointmentMove | `EventCallback<SchedulerAppointmentMoveEventArgs>` | A callback that will be invoked when an appointment is being dragged and then dropped on a different slot. Commonly used to change it to a different timeslot. |
| AppointmentSelect | `EventCallback<SchedulerAppointmentSelectEventArgs<TItem>>` | A callback that will be invoked when the user clicks an appointment in the current view. Commonly used to edit existing appointments. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| DaySelect | `EventCallback<SchedulerDaySelectEventArgs>` | A callback that will be invoked when the user clicks a day header button or the day number in a MonthView. |
| LoadData | `EventCallback<SchedulerLoadDataEventArgs>` | A callback that will be invoked when the scheduler needs data for the current view. Commonly used to filter the data assigned to . |
| MonthSelect | `EventCallback<SchedulerMonthSelectEventArgs>` | A callback that will be invoked when the user clicks a month header button. |
| MoreSelect | `EventCallback<SchedulerMoreSelectEventArgs>` | A callback that will be invoked when the user clicks the more text in the current view. Commonly used to view additional appointments. Invoke the method to prevent the default action (showing the additional appointments). |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| SlotSelect | `EventCallback<SchedulerSlotSelectEventArgs>` | A callback that will be invoked when the user clicks a slot in the current view. Commonly used to add new appointments. |
| TodaySelect | `EventCallback<SchedulerTodaySelectEventArgs>` | A callback that will be invoked when the user clicks the Today button. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddView(ISchedulerView view) | `Task` |  |
| GetAppointmentAttributes(AppointmentData item) | `IDictionary<string, object>` |  |
| GetAppointmentsInRange(DateTime start, DateTime end) | `IEnumerable<AppointmentData>` |  |
| GetSlotAttributes(DateTime start, DateTime end, Func<IEnumerable<AppointmentData>> getAppointments) | `IDictionary<string, object>` |  |
| IsAppointmentInRange(AppointmentData item, DateTime start, DateTime end) | `bool` |  |
| IsSelected(ISchedulerView view) | `bool` |  |
| Reload() | `Task` | Causes the current scheduler view to render. Enumerates the items of and creates instances of to display in the current view. Use it when has changed. |
| RemoveView(ISchedulerView view) | `void` |  |
| RenderAppointment(AppointmentData item) | `RenderFragment` |  |
| Resize(double width, double height) | `void` | Invoked from client-side via interop when the scheduler size changes. |
| SelectAppointment(AppointmentData data) | `Task` |  |
| SelectDay(DateTime day, IEnumerable<AppointmentData> appointments) | `Task` |  |
| SelectMonth(DateTime monthStart, IEnumerable<AppointmentData> appointments) | `Task` |  |
| SelectMore(DateTime start, DateTime end, IEnumerable<AppointmentData> appointments) | `Task<bool>` |  |
| SelectSlot(DateTime start, DateTime end) | `Task` |  |
| SelectSlot(DateTime start, DateTime end, IEnumerable<AppointmentData> appointments) | `Task<bool>` |  |
| SelectView(ISchedulerView view) | `Task` | Selects the specified . The view must already be present in this scheduler. If the specified view is already selected, no action will be performed. |

