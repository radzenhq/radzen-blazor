# RadzenAccordion API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AriaLevel | `int` | Gets or sets the ARIA heading level applied to each accordion header. The header button is wrapped in an element with role="heading" and this aria-level so screen-reader users can navigate the accordion by heading, as required by the WAI-ARIA Accordion pattern. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Items | `RenderFragment?` | Gets or sets the render fragment containing RadzenAccordionItem components that define the accordion panels. Each RadzenAccordionItem represents one expandable panel with its header and content. |
| Multiple | `bool` | Gets or sets whether multiple accordion items can be expanded simultaneously. When false (default), expanding one item automatically collapses others. When true, users can expand multiple items independently. |
| RenderMode | `AccordionRenderMode` | Gets or sets the render mode of the accordion. When set to (default), the component re-renders on every expand/collapse. When set to , all items are rendered and expand/collapse is handled with JavaScript. |
| SelectedIndex | `int` | Gets or sets the zero-based index of the currently expanded item. Use with @bind-SelectedIndex for two-way binding to programmatically control which item is expanded. In multiple expand mode, this represents the last expanded item. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Collapse | `EventCallback<int>` | Gets or sets the callback invoked when an accordion item is collapsed. Receives the index of the collapsed item as a parameter. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Expand | `EventCallback<int>` | Gets or sets the callback invoked when an accordion item is expanded. Receives the index of the expanded item as a parameter. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| SelectedIndexChanged | `EventCallback<int>` | Gets or sets the callback invoked when the selected index changes. Used for two-way binding with @bind-SelectedIndex. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddItem(RadzenAccordionItem item) | `void` | Adds the item. |
| CollapseAll() | `Task` | Collapses all accordion items. |
| ExpandAll() | `Task` | Expands all accordion items. |
| Refresh() | `void` | Refreshes this instance. |
| RemoveItem(RadzenAccordionItem item) | `void` | Removes the item. |

