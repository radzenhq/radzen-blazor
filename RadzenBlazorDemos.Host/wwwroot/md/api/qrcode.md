# RadzenQRCode API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Background | `string` | Gets or sets the background color of the QR code. Should contrast well with the foreground color for reliable scanning. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Ecc | `RadzenQREcc` | Gets or sets the error correction level determining how much damage the QR code can sustain while remaining scannable. Higher levels add more redundancy but reduce data capacity. Use High or Quartile when embedding logos. |
| EyeColor | `string?` | Optional color for eyes; if empty, falls back to Foreground. |
| EyeColorBottomLeft | `string?` | Optional color for bottom right eye; if empty, falls back to EyeColor. |
| EyeColorTopLeft | `string?` | Optional color for top left eye; if empty, falls back to EyeColor. |
| EyeColorTopRight | `string?` | Optional color for top right eye; if empty, falls back to EyeColor. |
| EyeShape | `QRCodeEyeShape` | Shape for finder eyes (the 3 corner boxes). |
| EyeShapeBottomLeft | `QRCodeEyeShape?` | Shape for top bottom finder eye. |
| EyeShapeTopLeft | `QRCodeEyeShape?` | Shape for top left finder eye. |
| EyeShapeTopRight | `QRCodeEyeShape?` | Shape for top right finder eye. |
| Foreground | `string` | Gets or sets the color of the QR code modules (the dark squares/dots). Supports any valid CSS color. Use high contrast with background for best scanability. |
| Image | `string?` | URL, data: URI, or raw base64 (will be prefixed) to render in the center. |
| ImageBackground | `string` | Background color under the logo (usually white). |
| ImageBackgroundOpacity | `double` | Background opacity under the logo (0..1). Default 1. |
| ImageCornerRadius | `double` | Rounded-corner radius for the logo cutout in module units. Default 0.75. |
| ImagePaddingModules | `double` | Extra white padding around the logo in module units. Default 1. |
| ImageSizePercent | `double` | Logo box size as % of the inner QR (without quiet zone). Safe range 5�60%. Default 20. |
| ModuleShape | `QRCodeModuleShape` | Gets or sets the visual shape of the QR code modules (data squares). Square creates standard QR codes, Rounded creates softer corners, Circle creates dot-based codes. |
| QuietZone | `int` | Gets or sets the quiet zone (margin) size in QR code modules around the QR code. The quiet zone helps scanners detect the QR code boundaries. The QR standard recommends 4 modules. Set to 0 to remove the margin entirely. |
| Size | `string` | Gets or sets the rendered size (both width and height) of the QR code SVG. Accepts CSS units (e.g., "200px", "100%", "10rem"). Use percentage for responsive sizing. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `string` | Gets or sets the text or data to encode in the QR code. Can be plain text, URLs, contact information (vCard), WiFi credentials, or any string data. |
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
| ToPng(string fileName, int? width, int? height) | `Task` | Renders the QR code as a PNG image and downloads it in the browser. |
| ToPng(int? width, int? height) | `Task<byte[]>` | Renders the QR code as a PNG image and downloads it in the browser. |
| ToSvg() | `Task<string>` | Returns the SVG markup of the rendered QR code as a string. |

