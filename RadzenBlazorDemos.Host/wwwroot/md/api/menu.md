# RadzenMenu API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AriaLabel | `string` | Gets or sets the menu aria label text. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content |
| ClickToOpen | `bool` | Gets or sets the interaction mode for opening submenus. When true, submenus open on click. When false, submenus open on hover (desktop) and click (touch devices). |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Flyout | `bool` | Gets or sets whether nested submenus should fly out horizontally to the side instead of expanding vertically inline. When enabled, 2nd level and deeper submenus appear as cascading flyout menus positioned to the right of their parent item. |
| IsContextMenu | `bool` | Gets or sets a value indicating whether this menu is rendered as a context menu popup. When enabled, the root element uses role="menu" with vertical orientation instead of a horizontal menubar. |
| Responsive | `bool` | Gets or sets whether the menu should automatically collapse to a hamburger menu on small screens. When enabled, displays a toggle button that expands/collapses the menu on mobile devices. |
| Style | `string?` | Gets or sets the inline CSS style. |
| ToggleAriaLabel | `string` | Gets or sets the add button aria-label attribute. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Click | `EventCallback<MenuItemEventArgs>` | Gets or sets the click callback. |
| Close | `EventCallback` | Gets or sets a callback invoked when the menu requests to be dismissed, such as pressing Escape at the root of a context menu. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddItem(RadzenMenuItem item) | `void` | Adds the item. |

