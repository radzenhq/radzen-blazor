# RadzenColorPicker API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AlphaAriaLabel | `string` | Gets or sets the aria label text of the alpha slider. |
| AlphaText | `string` | Gets or sets the alpha label text. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| BlueText | `string` | Gets or sets the blue color label text. |
| ButtonText | `string` | Gets or sets the button text. |
| ChildContent | `RenderFragment?` | Gets or sets the child content. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| GreenText | `string` | Gets or sets the green color label text. |
| HexText | `string` | Gets or sets the hexadecimal color label text. |
| HueAriaLabel | `string` | Gets or sets the aria label text of the hue slider. |
| Icon | `string?` | Gets or sets the icon. |
| IconColor | `string?` | Gets or sets the icon color. |
| InputSize | `InputSize` | Gets or sets the size of the component. |
| Name | `string?` | Gets or sets the unique name identifier for this form component. Used for validation association (linking with validators and labels) and for identifying the field in form submission. This name should be unique within the form and match the Component property of associated validators/labels. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| PopupAriaLabel | `string` | Gets or sets the popup aria label text. |
| PopupRenderMode | `PopupRenderMode` | Gets or sets the render mode. |
| RedText | `string` | Gets or sets the red color label text. |
| SaturationAriaLabel | `string` | Gets or sets the aria label text of the saturation and brightness area. |
| SaturationValueTextFormat | `string` | Gets or sets the format string used to build the aria-valuetext of the saturation and brightness area. The first argument is the saturation percent and the second one is the brightness percent. |
| ShowArrow | `bool` | Gets or sets a value indicating whether the dropdown arrow is shown. |
| ShowButton | `bool` | Gets or sets a value indicating whether button is shown. |
| ShowColors | `bool` | Gets or sets a value indicating whether colors are shown. |
| ShowHSV | `bool` | Gets or sets a value indicating whether HSV is shown. |
| ShowRGBA | `bool` | Gets or sets a value indicating whether RGBA is shown. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab order index for keyboard navigation. Controls the order in which fields receive focus when the user presses the Tab key. Lower values receive focus first. Use -1 to exclude from tab navigation. |
| ToggleAriaLabel | `string` | Gets or sets the toggle popup aria label text. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<T>` | Gets or sets the change. |
| Close | `EventCallback` | Gets or sets the close callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Open | `EventCallback` | Gets or sets the open callback. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

