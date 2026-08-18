# RadzenLink API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets custom child content to render as the link content. When set, overrides the property for complex link content with custom markup. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets whether the link is disabled and cannot be clicked. When disabled, the link appears grayed out and does not navigate. |
| Icon | `string?` | Gets or sets the Material icon name to display before the link text. Use Material Symbols icon names (e.g., "home", "settings", "open_in_new"). |
| IconColor | `string?` | Gets or sets a custom color for the icon. Supports any valid CSS color value. If not set, icon inherits the link color. |
| Image | `string?` | Gets or sets a custom image URL to display before the link text instead of an icon. Alternative to using for custom graphics. |
| ImageAlternateText | `string` | Gets or sets the alternate text for the image when using the property. Provides accessibility text for screen readers when an image is used instead of an icon. |
| Match | `NavLinkMatch` | Gets or sets how the link's active state is determined by comparing the current URL to the link path. Prefix matches when URL starts with path, All requires exact match. |
| Path | `string` | Gets or sets the URL path for navigation. Can be a relative path for internal navigation (e.g., "/products") or an absolute URL for external sites. |
| Style | `string?` | Gets or sets the inline CSS style. |
| Target | `string?` | Gets or sets the target window or frame for the link navigation. Use "_blank" for new tab, "_self" for same window, or custom frame names. |
| Text | `string` | Gets or sets the link text to display. For simple text links, use this property. For complex content, use instead. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

