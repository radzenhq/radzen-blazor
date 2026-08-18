# RadzenTabs API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowReorder | `bool` | Gets or sets a value indicating whether the user can reorder tabs by dragging and dropping. When enabled, tab headers become draggable and can be rearranged by the user. |
| AriaLabel | `string?` | Gets or sets the accessible name applied to the tab list via aria-label. Use this to give assistive technologies a label describing the group of tabs. |
| AriaLabelledBy | `string?` | Gets or sets the id of the element that labels the tab list via aria-labelledby. Use this when a visible element already provides the accessible name for the group of tabs. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| RenderMode | `TabRenderMode` | Gets or sets the rendering mode that determines how tab content is rendered and switched. Server mode re-renders on the server when tabs change, while Client mode uses JavaScript for instant switching. |
| SelectedIndex | `int` | Gets or sets the zero-based index of the currently selected tab. Use with @bind-SelectedIndex for two-way binding to track and control the active tab. Set to -1 for no selection (though typically the first tab is selected automatically). |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabPosition | `TabPosition` | Gets or sets the visual position of the tab headers relative to the content panels. Controls the layout direction and can position tabs at Top, Bottom, Left, Right, TopRight, or BottomRight of the content. |
| Tabs | `RenderFragment?` | Gets or sets the render fragment containing RadzenTabsItem components that define the tabs. Each RadzenTabsItem represents one tab with its header and content. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<int>` | Gets or sets the callback invoked when the user switches to a different tab. Provides the index of the newly selected tab. Use this for side effects or logging. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Reorder | `EventCallback<TabsReorderEventArgs>` | Gets or sets the callback invoked when tabs are reordered via drag and drop. Provides a with the old and new index of the moved tab. |
| SelectedIndexChanged | `EventCallback<int>` | Gets or sets the callback invoked when the selected tab index changes. Used for two-way binding with @bind-SelectedIndex. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddTab(RadzenTabsItem tab) | `Task` | Adds the tab. |
| Reload() | `void` | Reloads this instance. |
| RemoveItem(RadzenTabsItem item) | `void` | Removes the item. |

