# RadzenTemplateForm API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Action | `string?` | Specifies the form action attribute. When set the form submits to the specified URL. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment<EditContext>?` | Gets or sets the child content. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `TItem?` | Specifies the model of the form. Required to support validation. |
| EditContext | `EditContext?` | Gets or sets the edit context. |
| Method | `string?` | Specifies the form method attribute. Used together with . |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| InvalidSubmit | `EventCallback<FormInvalidSubmitEventArgs>` | A callback that will be invoked when the user submits the form and is false. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| OnInvalidSubmit | `EventCallback<FormInvalidSubmitEventArgs>` | Obsolete. Use instead. |
| Submit | `EventCallback<TItem>` | A callback that will be invoked when the user submits the form and is true. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddComponent(IRadzenFormComponent component) | `void` |  |
| FindComponent(string name) | `IRadzenFormComponent` |  |
| RemoveComponent(IRadzenFormComponent component) | `void` |  |

