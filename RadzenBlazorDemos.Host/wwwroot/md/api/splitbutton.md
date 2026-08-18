# RadzenSplitButton API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AlwaysOpenPopup | `bool` | Gets or sets the value indication behaviour to always open popup with item on click and not invoke event. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| BusyText | `string` | Gets or sets the busy text. |
| ButtonAriaLabel | `string` | Gets or sets the add button aria-label attribute. |
| ButtonContent | `RenderFragment?` | Gets or sets the child content. |
| ButtonStyle | `ButtonStyle` | Gets or sets the button style. |
| ButtonType | `ButtonType` | Gets or sets the type of the button. |
| ChildContent | `RenderFragment?` | Gets or sets the child content |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| DropDownIcon | `string` | Gets or sets the icon of the drop down. |
| Icon | `string?` | Gets or sets the icon. |
| IconColor | `string?` | Gets or sets the icon color. |
| Image | `string?` | Gets or sets the image. |
| ImageAlternateText | `string` | Gets or sets the text. |
| IsBusy | `bool` | Gets or sets a value indicating whether this instance busy text is shown. |
| OpenAriaLabel | `string` | Gets or sets the open button aria-label attribute. |
| Shade | `Shade` | Gets or sets the color shade of the button. |
| Size | `ButtonSize` | Gets or sets the size. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the index of the tab. |
| Text | `string` | Gets or sets the text. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Variant | `Variant` | Gets or sets the design variant of the button. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Click | `EventCallback<RadzenSplitButtonItem>` | Gets or sets the click callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddItem(RadzenSplitButtonItem item) | `void` | Adds the item. |
| Close() | `void` | Closes this instance popup. |
| OnClick(MouseEventArgs args) | `System.Threading.Tasks.Task` | Handles the click event. |
| RemoveItem(RadzenSplitButtonItem item) | `void` | Removes the item. |

