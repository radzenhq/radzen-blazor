# RadzenDatePicker API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowClear | `bool` | Gets or sets a value indicating whether value can be cleared. |
| AllowInput | `bool` | Gets or sets a value indicating whether input is allowed. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ButtonClass | `string` | Gets or sets the button CSS class. |
| CalendarWeekTitle | `string` | Gets or sets the previous month aria label text. |
| ClearAriaLabel | `string` | Gets or sets the clear button aria label text. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| DateFormat | `string` | Gets or sets the date format. |
| DateRender | `Action<DateRenderEventArgs>?` | Gets or sets the date render callback. Use it to set attributes. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| DisabledAriaLabel | `string` | Gets or sets the suffix appended to a day cell's aria label when the date is disabled. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| FooterTemplate | `RenderFragment?` | Gets or sets the footer template. |
| HourAriaLabel | `string` | Gets or sets the hour input aria label text. |
| HourFormat | `string` | Gets or sets the hour format. |
| HoursStep | `string` | Gets or sets the hours step. |
| Immediate | `bool` | Gets or sets a value indicating whether the component should update its value on every input event rather than waiting for the input to lose focus (onchange event). When enabled, the bound value is updated as the user types, provided the input can be parsed as a valid date. Invalid intermediate input is ignored to avoid clearing the value while the user is still typing. |
| InitialViewDate | `DateTime?` | Gets or sets the Initial Date/Month View. |
| Inline | `bool` | Gets or sets a value indicating whether this is inline - only Calender. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| InputClass | `string` | Gets or sets the input CSS class. |
| InputSize | `InputSize` | Gets or sets the size of the component. |
| Kind | `DateTimeKind` | Gets or sets the kind of DateTime bind to control |
| Max | `DateTime?` | Gets or sets the Maximum Selectable Date. |
| Min | `DateTime?` | Gets or sets the Minimum Selectable Date. |
| MinutesAriaLabel | `string` | Gets or sets the minutes input aria label text. |
| MinutesStep | `string` | Gets or sets the minutes step. |
| Multiple | `bool` | Gets or sets whether multiple dates can be selected. When enabled, users can select multiple dates from the calendar, and the value will be a collection of DateTimes. |
| Name | `string?` | Gets or sets the name of the form component. |
| NextMonthAriaLabel | `string` | Gets or sets the next month aria label text. |
| OkAriaLabel | `string` | Gets or sets the OK button aria label text. |
| PadHours | `bool` | Gets or sets a value indicating whether the hour picker is padded with a leading zero. |
| PadMinutes | `bool` | Gets or sets a value indicating whether the minute picker is padded with a leading zero. |
| PadSeconds | `bool` | Gets or sets a value indicating whether the second picker is padded with a leading zero. |
| ParseInput | `Func<string, DateTime?>?` | Parse the input using an function outside the Radzen-library |
| Placeholder | `string` | Gets or sets the input placeholder. |
| PopupAriaLabel | `string` | Gets or sets the popup aria label text. |
| PopupRenderMode | `PopupRenderMode` | Gets or sets the render mode. |
| PrevMonthAriaLabel | `string` | Gets or sets the previous month aria label text. |
| ReadOnly | `bool` | Gets or sets a value indicating whether read only. |
| SecondsAriaLabel | `string` | Gets or sets the seconds input aria label text. |
| SecondsStep | `string` | Gets or sets the seconds step. |
| SelectedAriaLabel | `string` | Gets or sets the suffix appended to a day cell's aria label when the date is selected. |
| ShowButton | `bool` | Gets or sets a value indicating whether popup datepicker button is shown. |
| ShowCalendarWeek | `bool` | Gets or sets whether the calendar week number column should be displayed in the calendar popup. When enabled, each week row shows its corresponding week number according to ISO 8601. |
| ShowDays | `bool` | Gets or sets a value indicating whether days part is shown. |
| ShowHour | `bool` | Gets or sets a value indicating whether hour is shown. |
| ShowInput | `bool` | Gets or sets a value indicating whether the input box is shown. Ignored if ShowButton is false. |
| ShowMinutes | `bool` | Gets or sets a value indicating whether minutes are shown. |
| ShowSeconds | `bool` | Gets or sets a value indicating whether seconds are shown. |
| ShowTime | `bool` | Gets or sets a value indicating whether time part is shown. |
| ShowTimeOkButton | `bool` | Gets or sets a value indicating whether time ok button is shown. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab index. |
| TimeOnly | `bool` | Gets or sets a value indicating whether time only can be set. |
| TodayAriaLabel | `string` | Gets or sets the suffix appended to a day cell's aria label when the date is today. |
| ToggleAmPmAriaLabel | `string` | Gets or sets the toggle Am/Pm aria label text. |
| ToggleAriaLabel | `string` | Gets or sets the toggle popup aria label text. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `object?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<TValue>>?` | Gets or sets the value expression. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |
| YearFormat | `string` | Gets ot sets the year format. Set to yyyy by default. |
| YearFormatter | `Func<int, string>?` | Gets or sets the year formatter. Set to FormatYear by default. If set, this function will take precedence over . |
| YearRange | `string` | Gets or sets the year range. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<DateTime?>` | Gets or sets the change callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| CurrentDateChanged | `EventCallback<DateTime>` | Gets or set the current date changed callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| OkClick | `EventCallback<DateTime?>` | Gets or sets the OK click callback. Fires only when the user clicks the OK button (visible when is true), allowing developers to distinguish between intermediate day-selection changes and the final user confirmation. |
| ValueChanged | `EventCallback<TValue>` | Gets or sets the value changed callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| Close() | `void` | Closes this instance popup. |
| FocusAsync() | `ValueTask` |  |
| GetValue() | `object?` | Gets the value. |
| OnPopupClose() | `void` | Called from JavaScript when the popup is closed (e.g. by clicking outside) in Initial render mode. |

