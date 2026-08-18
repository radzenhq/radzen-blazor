# RadzenPopup API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| AutoFocusFirstElement | `bool` | Specifies whether the first element in the popup should be automatically focused. |
| ChildContent | `RenderFragment` | Gets or sets the content to be rendered inside the popup. |
| CloseOnClickOutside | `bool` | Specifies whether the popup should close when clicking outside of it. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Lazy | `bool` | Determines whether the popup content is rendered only when open. |
| PreventDefault | `bool` | Specifies whether to prevent the default action on mouse down. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Close | `EventCallback` | Event callback triggered when the popup is closed. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Open | `EventCallback` | Event callback triggered when the popup is opened. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| CloseAsync(ElementReference target) | `Task` | Closes the popup and sets the target element. |
| CloseAsync() | `Task` | Closes the popup and sets the target element. |
| OnClose() | `Task` | Invoked from JavaScript to close the popup. |
| ToggleAsync(ElementReference target, bool disableSmartPosition, bool syncWidth) | `Task` | Toggles the popup open or closed. |

