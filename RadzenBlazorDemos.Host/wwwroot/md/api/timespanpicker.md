# RadzenTimeSpanPicker API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowClear | `bool` | Specifies whether the value can be cleared. |
| AllowInput | `bool` | Specifies whether input in the input field is allowed. Set to true by default. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ClearAriaLabel | `string` | Gets or sets the clear button aria label text. |
| ConfirmationButtonText | `string` | Specifies the text of the confirmation button. Used only if is true. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| DaysStep | `string?` | Specifies the step of the days field in the picker panel. |
| DaysUnitText | `string` | Specifies the days label text. |
| Disabled | `bool` | Specifies whether the input field is disabled. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| FieldPrecision | `TimeSpanUnit` | Specifies the most precise time unit field in the picker panel. Set to by default. |
| HoursStep | `string?` | Specifies the step of the hours field in the picker panel. |
| HoursUnitText | `string` | Specifies the hours label text. |
| Inline | `bool` | Specifies whether the component is inline or shows a popup. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| InputClass | `string?` | Specifies the input CSS classes, separated with spaces. |
| InputSize | `InputSize` | Gets or sets the size of the component. |
| Max | `TimeSpan` | Specifies the maximum time span allowed. |
| MicrosecondsStep | `string?` | Specifies the step of the microseconds field in the picker panel. |
| MicrosecondsUnitText | `string` | Specifies the microseconds label text. |
| MillisecondsStep | `string?` | Specifies the step of the milliseconds field in the picker panel. |
| MillisecondsUnitText | `string` | Specifies the milliseconds label text. |
| Min | `TimeSpan` | Specifies the minimum time span allowed. |
| MinutesStep | `string?` | Specifies the step of the minutes field in the picker panel. |
| MinutesUnitText | `string` | Specifies the minutes label text. |
| Name | `string?` | Specifies the name of the input field. |
| NegativeButtonText | `string` | Specifies the text of the negative value button. |
| NegativeValueText | `string` | Specifies the text displayed next to the fields in the panel when the value is negative and there's no sign picker. |
| PadTimeValues | `bool` | Specifies whether the time fields in the panel, except for the days field, are padded with leading zeros. |
| ParseInput | `Func<string, TimeSpan?>?` | Specifies custom function to parse the input. If it's not defined or the function it returns null, a built-in parser us used instead. |
| Placeholder | `string?` | Specifies the input placeholder. |
| PopupAriaLabel | `string` | Specifies the aria label for the popup. |
| PopupButtonClass | `string?` | Specifies the popup toggle button CSS classes, separated with spaces. |
| PopupRenderMode | `PopupRenderMode` | Specifies the render mode of the popup. |
| PositiveButtonText | `string` | Specifies the text of the positive value button. |
| PositiveValueText | `string` | Specifies the text displayed next to the fields in the panel when the value is positive and there's no sign picker. |
| ReadOnly | `bool` | Specifies whether the input field is read only. |
| SecondsStep | `string?` | Specifies the step of the seconds field in the picker panel. |
| SecondsUnitText | `string` | Specifies the seconds label text. |
| ShowConfirmationButton | `bool` | Specifies whether to display the confirmation button in the panel to accept changes. |
| ShowPopupButton | `bool` | Specifies whether to display popup icon button in the input field. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Specifies the tab index. |
| TimeSpanFormat | `string?` | Specifies the time span format in the input field. For more details, see the documentation of standard and custom time span format strings. |
| TogglePopupAriaLabel | `string` | Specifies the aria label for the toggle popup button. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `TValue?` | Specifies the value of the component. |
| ValueExpression | `Expression<Func<TValue>>?` | Specifies the value expression used while creating the . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<TimeSpan?>` | Specifies the callback of the underlying nullable value. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ValueChanged | `EventCallback<TValue>` | Specifies the callback of the value change. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| Close() | `Task` | Closes this instance popup. |
| FocusAsync() | `ValueTask` |  |
| GetValue() | `object?` | Gets the value of the component. |

