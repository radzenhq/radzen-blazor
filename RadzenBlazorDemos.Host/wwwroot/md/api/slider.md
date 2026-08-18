# RadzenSlider API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| HandleLabel | `string` | Gets or sets the accessible name (aria-label) for the handle of a single-value slider. |
| Max | `decimal` | Determines the maximum value. |
| MaxHandleLabel | `string` | Gets or sets the accessible name (aria-label) for the maximum handle of a range slider. |
| Min | `decimal` | Determines the minimum value. |
| MinHandleLabel | `string` | Gets or sets the accessible name (aria-label) for the minimum handle of a range slider. |
| Name | `string?` | Gets or sets the unique name identifier for this form component. Used for validation association (linking with validators and labels) and for identifying the field in form submission. This name should be unique within the form and match the Component property of associated validators/labels. |
| Orientation | `Orientation` | Specifies the orientation. Set to Orientation.Horizontal by default. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| Range | `bool` | Gets or sets a value indicating whether this is range. |
| Step | `string` | Gets or sets the step. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab order index for keyboard navigation. Controls the order in which fields receive focus when the user presses the Tab key. Lower values receive focus first. Use -1 to exclude from tab navigation. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `TValue?` | Gets or sets the value. |
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

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| OnValueChange(decimal value, bool isMin) | `System.Threading.Tasks.Task` | Called when value changed. |

