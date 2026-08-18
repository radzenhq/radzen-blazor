# RadzenBadge API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| BadgeStyle | `BadgeStyle` | Gets or sets the semantic color style of the badge. Determines the badge's color based on its purpose (Primary, Success, Danger, Warning, etc.). |
| ChildContent | `RenderFragment?` | Gets or sets the custom child content to render inside the badge. When set, overrides the property for displaying custom markup. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| IsPill | `bool` | Gets or sets whether the badge should have rounded pill-shaped ends instead of rectangular corners. Pill badges have a more modern, capsule-like appearance and are often used for tags or status indicators. |
| Shade | `Shade` | Gets or sets the color intensity shade for the badge. Works in combination with to adjust the color darkness/lightness. |
| Style | `string?` | Gets or sets the inline CSS style. |
| Text | `string?` | Gets or sets the text content displayed in the badge. Typically used for short text like numbers, single words, or abbreviations. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Variant | `Variant` | Gets or sets the design variant that controls the badge's visual appearance. Options include Filled (solid background), Flat (subtle background), Outlined (border only), and Text (minimal styling). |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

