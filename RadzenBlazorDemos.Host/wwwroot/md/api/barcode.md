# RadzenBarcode API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Background | `string` | Gets or sets the barcode background color. |
| BarHeight | `double` | Gets or sets the height of the bars in SVG units (viewBox units). Default is 50. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| FontSize | `double` | Gets or sets the font size for layout calculations of the value text (in SVG viewBox units). This is not automatically applied as an SVG attribute; use to style the text. |
| Foreground | `string` | Gets or sets the barcode bars color. |
| Height | `string` | Gets or sets the rendered height of the SVG. Accepts CSS units (e.g. "80px"). If is true, the text is drawn inside this height. |
| QuietZoneModules | `int` | Gets or sets the quiet zone in modules (left and right padding). |
| ShowChecksum | `bool` | Gets or sets whether to show the checksum (if applicable for the selected ) under the bars. |
| ShowValue | `bool` | Gets or sets whether to show the value as text under the bars. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TextMarginTop | `double` | Gets or sets the gap between bars and text in SVG units (viewBox units). |
| Type | `RadzenBarcodeType` | Gets or sets the barcode type. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `string` | Gets or sets the barcode value to encode. |
| ValueStyle | `string?` | Gets or sets the value inline CSS style. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |
| Width | `string` | Gets or sets the rendered width of the SVG. Accepts CSS units (e.g. "300px", "100%"). |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| HasChecksum(RadzenBarcodeType type) | `bool` | Gets whether the specified barcode produces a checksum value that can be displayed when is enabled. |
| ToPng(string fileName, int? width, int? height) | `Task` | Renders the barcode as a PNG image and downloads it in the browser. |
| ToPng(int? width, int? height) | `Task<byte[]>` | Renders the barcode as a PNG image and downloads it in the browser. |
| ToSvg() | `Task<string>` | Returns the SVG markup of the rendered barcode as a string. |

