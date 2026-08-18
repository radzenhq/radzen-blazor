# RadzenNumeric API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AriaLabel | `string?` | Gets or sets the accessible name applied to the spinbutton input via the aria-label attribute. When not set, the component falls back to so the spinbutton has an accessible name by default. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| AutoCompleteType | `AutoCompleteType` | Gets or sets the browser autocomplete behavior for this input field. Controls whether browsers should offer to autofill this field based on user history or saved data. Common values include On (enable), Off (disable), Username, CurrentPassword, Email, etc. |
| ConvertValue | `Func<string, TValue>?` | Gets or sets the function which returns TValue from string. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| DownAriaLabel | `string` | Gets or sets the down button aria-label attribute. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| Format | `string?` | Gets or sets the format. |
| Immediate | `bool` | Gets or sets whether the component should update the bound value immediately as the user types (oninput event), rather than waiting for the input to lose focus (onchange event). This enables real-time value updates but may trigger more frequent change events. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Gets or sets additional HTML attributes to be applied to the underlying input element. This allows passing custom attributes like data-* attributes, aria-* attributes, or other HTML attributes directly to the input. |
| InputSize | `InputSize` | Gets or sets the size of the component. |
| Max | `decimal?` | Determines the maximum value. |
| MaxLength | `long?` | Gets or sets the maximum allowed text length. |
| Min | `decimal?` | Determines the minimum value. |
| Name | `string?` | Gets or sets the unique name identifier for this form component. Used for validation association (linking with validators and labels) and for identifying the field in form submission. This name should be unique within the form and match the Component property of associated validators/labels. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| ReadOnly | `bool` | Gets or sets a value indicating whether is read only. |
| ShowUpDown | `bool` | Gets or sets a value indicating whether up down buttons are shown. |
| Step | `string?` | Gets or sets the step. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab order index for keyboard navigation. Controls the order in which fields receive focus when the user presses the Tab key. Lower values receive focus first. Use -1 to exclude from tab navigation. |
| TextAlign | `TextAlign` | Gets or sets the text align. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| UpAriaLabel | `string` | Gets or sets the up button aria-label attribute. |
| Value | `TValue?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<T>` | Gets or sets the change. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| KeyDown | `EventCallback<NumericKeyboardEventArgs>` | Gets or sets an event callback raised when a key is pressed while the input is focused. Call on the argument to suppress the built-in ArrowUp/ArrowDown increment/decrement behavior and allow custom key handling (e.g. navigating to the next/previous input). |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

