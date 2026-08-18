# RadzenAlert API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AlertStyle | `AlertStyle` | Gets or sets the semantic style/severity of the alert. Determines the color scheme and default icon: Info (blue), Success (green), Warning (orange), Danger (red), etc. |
| AllowClose | `bool` | Gets or sets whether the alert can be dismissed by showing a close button. When enabled, a small X button appears in the top-right corner allowing users to close the alert. Handle the event to perform actions when the alert is dismissed. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content |
| CloseAriaLabel | `string` | Gets or sets the aria-label of the close button. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Icon | `string?` | Gets or sets a custom Material icon name to display instead of the default contextual icon. Overrides the automatic icon selection based on . Use Material Symbols icon names (e.g., "info", "warning", "check_circle"). |
| IconColor | `string?` | Gets or sets a custom color for the alert icon. Supports any valid CSS color value. If not set, the icon color matches the alert's semantic style. |
| Shade | `Shade` | Gets or sets the color intensity shade for the alert. Works in combination with to adjust the color darkness/lightness. |
| ShowIcon | `bool` | Gets or sets whether to display the contextual icon based on the . When true, shows an appropriate icon (checkmark for Success, info icon for Info, warning for Warning, etc.). Set to false to hide the icon, or provide a custom icon via the property. |
| Size | `AlertSize` | Gets or sets the size of the alert component. Controls the padding, font size, and icon size within the alert. |
| Style | `string?` | Gets or sets the inline CSS style. |
| Text | `string?` | Gets or sets the body text of the alert. This appears below the title as the main alert message. Overridden by ChildContent if custom content is provided. |
| Title | `string?` | Gets or sets the title text displayed prominently at the top of the alert. Use this for the main alert heading, with additional details in or custom content via ChildContent. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Variant | `Variant` | Gets or sets the design variant that controls the alert's visual appearance. Options include Filled (solid background), Flat (subtle background), Outlined (border only), and Text (minimal styling). |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Close | `EventCallback` | Gets or sets the callback which is invoked when the alert is closed by the user. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| VisibleChanged | `EventCallback<bool>` | Gets or sets the callback which is invoked when the alert is shown or hidden. |

