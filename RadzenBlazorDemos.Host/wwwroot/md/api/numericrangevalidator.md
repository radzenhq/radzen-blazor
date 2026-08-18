# RadzenNumericRangeValidator API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowNull | `bool` | Gets or sets whether null values should be considered valid. When true, null values pass validation (useful for optional nullable fields). When false (default), null values fail validation. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Component | `string` | Specifies the component which this validator should validate. Must be set to the of an existing component. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Max | `IComparable?` | Gets or sets the maximum allowed value (inclusive). The component value must be less than or equal to this value. Can be null to only validate minimum. Works with any IComparable type (int, decimal, DateTime, etc.). |
| Min | `IComparable?` | Gets or sets the minimum allowed value (inclusive). The component value must be greater than or equal to this value. Can be null to only validate maximum. Works with any IComparable type (int, decimal, DateTime, etc.). |
| Popup | `bool` | Determines if the validator is displayed as a popup or not. Set to false by default. |
| Style | `string?` | Gets or sets the inline CSS style. |
| Text | `string` | Gets or sets the error message displayed when the value is outside the valid range. Customize to provide specific guidance (e.g., "Age must be between 18 and 65"). |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

