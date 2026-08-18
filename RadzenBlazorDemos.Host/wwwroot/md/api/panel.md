# RadzenPanel API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowCollapse | `bool` | Gets or sets whether the panel can be collapsed by clicking its header. When enabled, a collapse/expand icon appears in the header, and clicking anywhere on the header toggles the panel state. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content |
| CollapseAriaLabel | `string?` | Gets or sets the aria-label attribute of the collapse button. |
| CollapseTitle | `string` | Gets or sets the title attribute of the collapse button. |
| Collapsed | `bool` | Gets or sets whether the panel is currently in a collapsed state. When collapsed, the main content is hidden and only the header (and optional SummaryTemplate) are visible. Use with @bind-Collapsed for two-way binding to programmatically control the panel state. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| ExpandAriaLabel | `string?` | Gets or sets the aria-label attribute of the expand button. |
| ExpandTitle | `string` | Gets or sets the title attribute of the expand button. |
| FooterTemplate | `RenderFragment?` | Gets or sets the footer content displayed at the bottom of the panel. This section appears below the main content and remains visible regardless of collapse state. |
| HeaderTemplate | `RenderFragment?` | Gets or sets the custom content for the panel header. When set, overrides the default header rendering (Text and Icon properties are ignored). Use this for complex headers with custom layouts, buttons, or other components. |
| Icon | `string?` | Gets or sets the Material icon name displayed in the panel header before the text. Use Material Symbols icon names (e.g., "settings", "info", "warning"). |
| IconColor | `string?` | Gets or sets a custom color for the header icon. Supports any valid CSS color value. If not set, uses the theme's default icon color. |
| Style | `string?` | Gets or sets the inline CSS style. |
| SummaryTemplate | `RenderFragment?` | Gets or sets the summary content displayed when the panel is collapsed. This optional content appears below the header in collapsed state, providing a preview or summary of the hidden content. When the panel is expanded, this content is not displayed. |
| Text | `string` | Gets or sets the text displayed in the panel header. This appears as the panel title. For more complex headers, use instead. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Collapse | `EventCallback` | Gets or sets the callback invoked when the panel is collapsed from an expanded state. Useful for cleanup operations or tracking panel state changes. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Expand | `EventCallback` | Gets or sets the callback invoked when the panel is expanded from a collapsed state. Useful for loading data on-demand or triggering animations when the panel opens. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

