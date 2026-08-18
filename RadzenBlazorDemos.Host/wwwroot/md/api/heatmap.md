# RadzenHeatmap API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| CellPadding | `double` | Gets or sets the padding between cells in pixels. |
| ChildContent | `RenderFragment?` | Gets or sets the child content (unused, for future extensibility). |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable<object>?` | Gets or sets the data items. |
| FormatString | `string?` | Gets or sets the format string for cell values. |
| MaxColor | `string` | Gets or sets the color for the maximum value. |
| MinColor | `string` | Gets or sets the color for the minimum value. |
| ShowValues | `bool` | Gets or sets whether to show values inside cells. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| ValueProperty | `string?` | Gets or sets the property name for cell values (determines color intensity). |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |
| XProperty | `string?` | Gets or sets the property name for X-axis categories. |
| YProperty | `string?` | Gets or sets the property name for Y-axis categories. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| Resize(double width, double height) | `void` | Resizes the heatmap. |

