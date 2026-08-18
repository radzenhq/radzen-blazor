# RadzenSplitter API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChangeStateOnResize | `bool` | Value indicating if the splitter should call StateHasChanged on resizing. |
| ChildContent | `RenderFragment?` | Gets or sets the panes to display within the splitter. Each RadzenSplitterPane represents one resizable section of the splitter. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Orientation | `Orientation` | Gets or sets the layout direction of the splitter. Horizontal arranges panes side-by-side (resizable width), Vertical stacks panes top-to-bottom (resizable height). |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Collapse | `EventCallback<RadzenSplitterEventArgs>` | Gets or sets the collapse callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Expand | `EventCallback<RadzenSplitterEventArgs>` | Gets or sets the expand callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Resize | `EventCallback<RadzenSplitterResizeEventArgs>` | Gets or sets the resize callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddPane(RadzenSplitterPane pane) | `void` | Adds the pane. |
| OnPaneResized(int paneIndex, double sizeNew, int? paneNextIndex, double? sizeNextNew) | `Task` | Called when pane resized. |
| OnPaneResizing() | `Task` | Called on pane resizing. |
| Refresh() | `void` | Refreshes this instance. |
| RemovePane(RadzenSplitterPane pane) | `void` | Removes the pane. |

