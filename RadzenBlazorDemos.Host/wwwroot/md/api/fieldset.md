# RadzenFieldset API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowCollapse | `bool` | Gets or sets a value indicating whether collapsing is allowed. Set to false by default. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content. |
| CollapseAriaLabel | `string?` | Gets or sets the aria-label attribute of the collapse button. |
| CollapseTitle | `string?` | Gets or sets the title attribute of the collapse button. |
| Collapsed | `bool` | Gets or sets a value indicating whether this is collapsed. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| ExpandAriaLabel | `string?` | Gets or sets the aria-label attribute of the expand button. |
| ExpandTitle | `string?` | Gets or sets the title attribute of the expand button. |
| HeaderTemplate | `RenderFragment?` | Gets or sets the header template. |
| Icon | `string?` | Gets or sets the icon. |
| IconColor | `string?` | Gets or sets the icon color. |
| Style | `string?` | Gets or sets the inline CSS style. |
| SummaryTemplate | `RenderFragment?` | Gets or sets the summary template. |
| Text | `string` | Gets or sets the text. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Collapse | `EventCallback` | Gets or sets the collapse callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Expand | `EventCallback` | Gets or sets the expand callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

