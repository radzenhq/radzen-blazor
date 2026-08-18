# RadzenTileLayout API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowMove | `bool` | Gets or sets a value indicating whether tiles can be moved while in . |
| AllowResize | `bool` | Gets or sets a value indicating whether tiles can be resized while in . |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the tiles to display. Should contain components. |
| Columns | `int` | Gets or sets the number of columns in the grid. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| EditMode | `bool` | Gets or sets a value indicating whether tiles can be moved and resized. When false the layout is read-only. |
| Gap | `double` | Gets or sets the gap between cells, in pixels. |
| RowHeight | `double` | Gets or sets the height of a single row, in pixels. |
| Rows | `int` | Gets or sets the number of rows in the grid. When 0 the grid grows automatically to fit its tiles. |
| ShowGrid | `bool` | Gets or sets a value indicating whether to render a grid overlay. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<RadzenTileLayoutItem>` | Gets or sets the callback raised after a tile has been moved or resized. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

