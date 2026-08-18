# RadzenRow API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AlignItems | `AlignItems` | Gets or sets the items alignment. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Gap | `string?` | Gets or sets the spacing between columns within the row. Accepts CSS length values (e.g., "1rem", "16px", "2em") or unitless numbers (interpreted as pixels). This sets the horizontal gap between column elements. |
| JustifyContent | `JustifyContent` | Gets or sets the content justify. |
| RowGap | `string?` | Gets or sets the vertical spacing between wrapped rows when columns wrap to multiple lines. Accepts CSS length values (e.g., "1rem", "16px", "2em") or unitless numbers (interpreted as pixels). Only applicable when columns wrap due to exceeding the 12-column limit. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

