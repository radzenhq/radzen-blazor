# RadzenSankeyDiagram API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Animated | `bool` | Gets or sets whether to animate the flow in the links. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ColorScheme | `ColorScheme` | Gets or sets the color scheme of the chart. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable<TItem>?` | Gets or sets the data. Each item represents a link/flow in the diagram. |
| FlowText | `string` | Gets or sets the tooltip text for "Flow". |
| IncomingText | `string` | Gets or sets the tooltip text for "Incoming". |
| LinkFills | `IList<string>?` | Gets or sets the link fill colors. If not specified, inherits from source node. |
| NodeAlignment | `SankeyAlignment` | Gets or sets the node alignment. |
| NodeFills | `IList<string>?` | Gets or sets the node fill colors. If not specified, uses color scheme. |
| NodePadding | `double` | Gets or sets the node padding. |
| NodeWidth | `double` | Gets or sets the node width. |
| OutgoingText | `string` | Gets or sets the tooltip text for "Outgoing". |
| SourceLabelProperty | `string?` | Specifies the property of which provides the source node label. |
| SourceProperty | `string?` | Specifies the property of which provides the source node ID. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TargetLabelProperty | `string?` | Specifies the property of which provides the target node label. |
| TargetProperty | `string?` | Specifies the property of which provides the target node ID. |
| TooltipStyle | `string?` | Gets or sets the CSS style of the tooltip. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| ValueFormatter | `Func<double, string>?` | Gets or sets the value formatter for tooltip display. |
| ValueProperty | `string?` | Specifies the property of which provides the flow value. |
| ValueText | `string` | Gets or sets the tooltip text for "Value". |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| Reload() | `void` | Causes the component to re-render. Use it when has changed. |
| Resize(double width, double height) | `Task` | Called by JavaScript when the chart container is resized. |

