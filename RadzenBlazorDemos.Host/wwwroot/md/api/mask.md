# RadzenMask API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| AutoCompleteType | `AutoCompleteType` | Gets or sets the browser autocomplete behavior for this input field. Controls whether browsers should offer to autofill this field based on user history or saved data. Common values include On (enable), Off (disable), Username, CurrentPassword, Email, etc. |
| CharacterPattern | `string?` | Gets or sets a regular expression pattern specifying which characters are valid for user input. Only characters matching this pattern are accepted as the user types. If both and CharacterPattern are set, CharacterPattern takes precedence. Example: "[0-9]" allows only digit characters. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| Immediate | `bool` | Gets or sets whether the component should update the bound value immediately as the user types (oninput event), rather than waiting for the input to lose focus (onchange event). This enables real-time value updates but may trigger more frequent change events. |
| InputSize | `InputSize` | Gets or sets the size of the component. |
| Mask | `string?` | Gets or sets the mask pattern that defines the input format. Use asterisks (*) for user input positions and literal characters for formatting. Example: "(***) ***-****" creates a phone number format like "(555) 123-4567". |
| MaxLength | `long?` | Gets or sets the maximum number of characters that can be entered. Typically matches the mask length, but can be set for additional constraints. |
| Name | `string?` | Gets or sets the unique name identifier for this form component. Used for validation association (linking with validators and labels) and for identifying the field in form submission. This name should be unique within the form and match the Component property of associated validators/labels. |
| Pattern | `string?` | Gets or sets a regular expression pattern for removing invalid characters from user input. Characters matching this pattern are stripped out as the user types. Example: "[^0-9]" removes all non-digit characters for numeric-only input. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| ReadOnly | `bool` | Gets or sets whether the masked input is read-only and cannot be edited. When true, displays the formatted value but prevents user input. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab order index for keyboard navigation. Controls the order in which fields receive focus when the user presses the Tab key. Lower values receive focus first. Use -1 to exclude from tab navigation. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<T>` | Gets or sets the change. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

