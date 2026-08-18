# RadzenFabMenu API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AriaLabel | `string?` | Gets or sets the aria-label for the toggle button. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ButtonClass | `string?` | Gets or sets the button class. |
| ButtonStyle | `ButtonStyle` | Gets or sets the button style. |
| ButtonStyleCss | `string?` | Gets or sets the button style CSS. |
| ChildContent | `RenderFragment?` | Gets or sets the child content. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Direction | `FabMenuDirection` | Gets or sets the direction in which the menu items expand. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| Gap | `string` | Gets or sets the gap. |
| Icon | `string` | Gets or sets the icon. |
| IsOpen | `bool` | Gets or sets a value indicating whether the menu is open. |
| ItemsStyle | `string?` | Gets or sets the items style. |
| Shade | `Shade` | Gets or sets the shade. |
| Size | `ButtonSize` | Gets or sets the size. |
| Style | `string?` | Gets or sets the inline CSS style. |
| ToggleButtonStyle | `ButtonStyle` | Gets or sets the button toggled style. |
| ToggleIcon | `string` | Gets or sets the toggle icon. |
| ToggleShade | `Shade` | Gets or sets the button toggled shade. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Variant | `Variant` | Gets or sets the variant. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| IsOpenChanged | `EventCallback<bool>` | Gets or sets the IsOpen changed callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| CloseAsync() | `Task` | Closes the menu. |
| ToggleAsync() | `Task` | Toggles the menu open/closed state. |

