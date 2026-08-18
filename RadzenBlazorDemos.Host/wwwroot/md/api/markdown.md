# RadzenMarkdown API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowHtml | `bool` | Gets or sets whether HTML tags within the markdown are rendered or escaped. When true (default), safe HTML tags are allowed. Dangerous tags (script, iframe, style, object) are always filtered. When false, all HTML is treated as plain text and displayed literally. |
| AllowedHtmlAttributes | `IEnumerable<string>?` | Gets or sets a whitelist of HTML attributes permitted on HTML tags when is true. If set, only these attributes are rendered; others are stripped. If not set, uses a default list of safe attributes. |
| AllowedHtmlTags | `IEnumerable<string>?` | Gets or sets a whitelist of HTML tags permitted in the markdown when is true. If set, only these tags will be rendered; others are stripped. If not set, uses a default list of safe tags. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| AutoLinkHeadingDepth | `int` | Gets or sets the maximum heading level (1-6) for which to automatically generate anchor links. For example, setting to 3 creates anchors for h1, h2, and h3 headings. Set to 0 to disable auto-linking. Auto-links enable table of contents navigation. |
| ChildContent | `RenderFragment?` | Gets or sets the markdown content as a render fragment. The markdown text should be placed directly inside the component tags. Overridden by if both are set. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Style | `string?` | Gets or sets the inline CSS style. |
| Text | `string?` | Gets or sets the markdown content as a string. When set, takes precedence over . Use this to bind markdown from a variable. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

