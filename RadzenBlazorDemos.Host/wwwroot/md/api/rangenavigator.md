# RadzenRangeNavigator API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| AxisFormatString | `string?` | Gets or sets the format string for axis tick labels. Use standard .NET format strings, e.g. "{0:MMM yyyy}" for dates or "{0:N0}" for numbers. When not set, defaults to a short representation based on the data type. |
| ChildContent | `RenderFragment?` | Gets or sets the child content (navigator series). |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| End | `double` | Gets or sets the end of the selected range as a fraction (0-1). Supports two-way binding with @bind-End. |
| HandleLabelFormatString | `string?` | Gets or sets the format string for handle labels. Use standard .NET format strings, e.g. "{0:MMM dd, yyyy}" for dates. When not set, defaults to a short representation based on the data type. |
| Max | `object?` | Gets or sets the maximum value for the axis. Use when there are no child series to define the range. Supports and numeric types. |
| Min | `object?` | Gets or sets the minimum value for the axis. Use when there are no child series to define the range. Supports and numeric types. |
| ShowAxis | `bool` | Gets or sets whether an axis with tick labels is displayed below the navigator. |
| ShowHandleLabels | `bool` | Gets or sets whether labels are displayed above the selection handles showing the current range values. |
| Start | `double` | Gets or sets the start of the selected range as a fraction (0-1). Supports two-way binding with @bind-Start. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| EndChanged | `EventCallback<double>` | Gets or sets the callback invoked when the End value changes due to user interaction. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| StartChanged | `EventCallback<double>` | Gets or sets the callback invoked when the Start value changes due to user interaction. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| OnNavigatorDrag(double start, double end) | `Task` | Called from JS when the user drags the selection window or handles. |
| OnResize(double width, double height) | `void` | Called from JS when the navigator element is resized. |

