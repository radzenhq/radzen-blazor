# RadzenSpeechToTextButton API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ButtonStyle | `ButtonStyle` | Gets or sets the button style. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Icon | `string` | Gets or sets the icon displayed while not recording. |
| IconColor | `string?` | Gets or sets the icon color. |
| Language | `string?` | Gets or sets the icon displayed while recording. |
| StopIcon | `string` | Gets or sets the icon displayed while recording. |
| StopTitle | `string` | Gets or sets the message displayed when user hovers the button and it is recording. |
| Style | `string?` | Gets or sets the inline CSS style. |
| Title | `string` | Gets or sets the message displayed when user hovers the button and it is not recording. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<string>` | Callback which provides results from the speech recognition API. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| OnResult(string result) | `void` | Provides interface for javascript to pass speech results back to this component. |
| StopRecording() | `void` | Provides interface for javascript to stop speech to text recording on this component if another component starts recording. |

