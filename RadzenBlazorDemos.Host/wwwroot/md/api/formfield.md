# RadzenFormField API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowFloatingLabel | `bool` | Gets or sets a value indicating whether the label is floating or fixed on top. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the input component to wrap. Place the input component (RadzenTextBox, RadzenDropDown, etc.) here. The form field automatically integrates with the input for labels and validation. |
| Component | `string?` | Gets or sets the name of the form field. Used to associate the label with a component. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| End | `RenderFragment?` | Gets or sets content to render after (trailing position of) the input field. Typically used for icons like visibility toggle, clear button, or suffix text like units. Appears inside the form field border, after the input element. |
| Helper | `RenderFragment?` | Gets or sets content to render below the input field. Used for helper text, hints, character counters, or validation messages. Validators placed here are automatically displayed when validation fails. |
| Start | `RenderFragment?` | Gets or sets content to render before (leading position of) the input field. Typically used for icons like search, email, lock, or prefix text like currency symbols. Appears inside the form field border, before the input element. |
| Style | `string?` | Gets or sets the inline CSS style. |
| Text | `string?` | Gets or sets the label text. |
| TextTemplate | `RenderFragment?` | Gets or sets the custom content for the label using a Razor template. When provided, this template will be rendered instead of the plain text specified in the Text parameter. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Variant | `Variant` | Gets or sets the design variant of the form field. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

