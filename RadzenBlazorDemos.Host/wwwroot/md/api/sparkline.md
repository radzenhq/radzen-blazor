# RadzenSparkline API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowPan | `bool` | Gets or sets whether pan via scrollbar is enabled. |
| AllowSeriesHover | `bool` | Gets or sets a value indicating whether series highlight on hover is enabled. When true, hovering over a series or its legend item highlights the series and dims the others. |
| AllowZoom | `bool` | Gets or sets whether mouse wheel zoom is enabled. |
| Animate | `bool` | Gets or sets a value indicating whether series animate when the chart first renders. Series are revealed with a left-to-right wipe. The animation respects the user's reduced motion preference. |
| AnimateDataUpdates | `bool` | Gets or sets a value indicating whether line and area series morph smoothly when their data changes. Best suited for live dashboards where values update in place. Not applied while zooming or panning. Supported in Chromium and Firefox; other browsers update instantly. |
| AnimationDuration | `double` | Gets or sets the duration of the initial render animation in milliseconds. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content. Used to specify series and other configuration. |
| ClickTolerance | `int` | The minimum pixel distance from a data point to the mouse cursor required for the SeriesClick event to fire. Set to 25 by default. |
| ColorScheme | `ColorScheme` | Gets or sets the color scheme used to assign colors to chart series. Determines the palette of colors applied sequentially to each series when series-specific colors are not set. Available schemes include Pastel (default), Palette, Monochrome, and custom color schemes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| MouseMoveThrottle | `int?` | Gets or sets the minimum interval in milliseconds between mouse move notifications which drive the tooltip, crosshair and hover tracking. Mouse moves are coalesced to animation frames; this value imposes an additional delay between dispatches. Defaults to 0 (every animation frame) on WebAssembly and 50 on Blazor Server to limit SignalR traffic. |
| Style | `string?` | Gets or sets the inline CSS style. |
| SyncGroup | `string?` | Gets or sets the synchronization group of the chart. Charts which share the same group display a synchronized crosshair and active data points: hovering one chart highlights the same category in the others. Charts in a group should plot the same kind of category (e.g. the same dates). |
| TooltipTolerance | `int` | The minimum pixel distance from a data point to the mouse cursor required by the tooltip to show. Set to 25 by default. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| ViewEnd | `double` | Gets or sets the end of the visible range as a fraction (0-1) of the full category range. Supports two-way binding with @bind-ViewEnd. |
| ViewStart | `double` | Gets or sets the start of the visible range as a fraction (0-1) of the full category range. Supports two-way binding with @bind-ViewStart. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |
| Zoom | `double` | Gets or sets the zoom level as a percentage. A value of 100 means no zoom (full range visible). Higher values zoom in (e.g., 200 shows half the range, 400 shows a quarter). Set to 100 to reset zoom. Supports two-way binding with @bind-Zoom. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| LegendClick | `EventCallback<LegendClickEventArgs>` | Gets or sets the callback invoked when a user clicks on a legend item. Useful for implementing custom behaviors like toggling series visibility or filtering data. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| SeriesClick | `EventCallback<SeriesClickEventArgs>` | Gets or sets the callback invoked when a user clicks on a data point or segment in a chart series. Provides information about the clicked series, data item, and value in the event arguments. |
| ViewChange | `EventCallback<ChartViewChangeEventArgs>` | Gets or sets the callback invoked when the visible range changes due to zoom or pan. Provides the current zoom level and visible range fractions. |
| ViewEndChanged | `EventCallback<double>` | Gets or sets the callback invoked when the visible range end changes due to user interaction. Used for two-way binding with @bind-ViewEnd. |
| ViewStartChanged | `EventCallback<double>` | Gets or sets the callback invoked when the visible range start changes due to user interaction. Used for two-way binding with @bind-ViewStart. |
| ZoomChanged | `EventCallback<double>` | Gets or sets the callback invoked when the zoom level changes due to user interaction (mouse wheel or pan). Used for two-way binding with @bind-Zoom. |

