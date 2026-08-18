# RadzenChip API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets custom child content rendered inside the chip. |
| ChipStyle | `BadgeStyle` | Gets or sets the chip semantic style. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether the chip is disabled. |
| Icon | `string?` | Gets or sets the material icon displayed before the text. |
| RemoveChipTitle | `string` | Gets or sets the title used by the close button for accessibility. |
| Selected | `bool` | Gets or sets a value indicating whether the chip is selected. |
| Shade | `Shade` | Gets or sets the chip color shade. |
| Size | `ChipSize` | Gets or sets the chip size. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab index of the chip. |
| Text | `string?` | Gets or sets the text content of the chip. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Variant | `Variant` | Gets or sets the chip design variant. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Click | `EventCallback<MouseEventArgs>` | Gets or sets the callback invoked when the chip is clicked. |
| Close | `EventCallback<MouseEventArgs>` | Gets or sets the callback invoked when the remove button is clicked. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

