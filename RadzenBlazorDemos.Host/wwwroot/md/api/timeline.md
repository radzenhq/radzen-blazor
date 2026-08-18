# RadzenTimeline API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AlignItems | `AlignItems` | Gets or sets the cross-axis alignment of timeline item content (label, point, and child content). Controls vertical alignment for horizontal timelines, or horizontal alignment for vertical timelines. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Items | `RenderFragment?` | Gets or sets the render fragment containing RadzenTimelineItem components that define the timeline events. Each RadzenTimelineItem represents one event or milestone in the timeline. |
| LinePosition | `LinePosition` | Gets or sets where the connecting line appears relative to the timeline items. Options include Center (line between content), Start/End (line on side), or Alternate (zigzag pattern). |
| Orientation | `Orientation` | Gets or sets the layout direction of the timeline. Vertical displays events top-to-bottom, Horizontal displays events left-to-right. |
| Reverse | `bool` | Gets or sets whether to reverse the timeline order visually (but not in markup). When true with vertical orientation, items flow bottom-to-top. With horizontal, items flow right-to-left. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

