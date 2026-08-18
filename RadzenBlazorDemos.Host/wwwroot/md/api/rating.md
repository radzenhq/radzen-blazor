# RadzenRating API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AriaLabel | `string` | Gets or sets the accessible label text for the rating radio group. Used by screen readers to announce the purpose of the component. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ClearAriaLabel | `string` | Gets or sets the accessible label text for the clear rating action. Used by screen readers to announce the clear/reset rating button functionality. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| Name | `string?` | Gets or sets the unique name identifier for this form component. Used for validation association (linking with validators and labels) and for identifying the field in form submission. This name should be unique within the form and match the Component property of associated validators/labels. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| RateAriaLabel | `string` | Gets or sets the accessible label text template for rating actions. Used by screen readers to announce each star's rating value (e.g., "Rate 3 stars"). |
| ReadOnly | `bool` | Gets or sets whether the rating is read-only and cannot be changed by user interaction. When true, the stars display the current rating but cannot be clicked or modified. Useful for displaying ratings without allowing users to change them (e.g., showing product ratings). |
| Stars | `int` | Gets or sets the total number of stars to display in the rating component. The value can range from 0 to this number. Common values are 5 or 10. |
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

