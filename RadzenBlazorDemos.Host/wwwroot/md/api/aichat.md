# RadzenAIChat API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| ApiKey | `string?` | Gets or sets the API key for authentication. |
| ApiKeyHeader | `string?` | Gets or sets the API key header name. |
| AssistantAvatarText | `string` | Gets or sets the text displayed in the assistant avatar. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| DateSeparatorFormat | `string` | Gets or sets the format used to render the date separator. Defaults to "D" (long date pattern). |
| DateSeparatorTemplate | `RenderFragment<DateTime>?` | Optional template to render the date separator shown between messages when the day changes. Receives the of the following message. |
| Disabled | `bool` | Gets or sets whether the chat is disabled. |
| EmptyMessage | `string` | Gets or sets the message displayed when there are no messages. |
| EmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when there are no messages. |
| Endpoint | `string?` | Gets or sets the endpoint URL for the AI service. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| MaxMessages | `int` | Gets or sets the maximum number of messages to keep in the chat. |
| MaxTokens | `int?` | Gets or sets the max tokens. |
| MessageTemplate | `RenderFragment<ChatMessage>?` | Gets or sets the message template. |
| Model | `string?` | Gets or sets the model name. |
| Placeholder | `string` | Gets or sets the placeholder text for the input field. |
| Proxy | `string?` | Gets or sets the proxy URL for the AI service. |
| ReadOnly | `bool` | Gets or sets whether the input is read-only. |
| SessionId | `string?` | Gets or sets the session ID for maintaining conversation memory. If null, a new session will be created. |
| ShowClearButton | `bool` | Gets or sets whether to show the clear chat button. |
| ShowDateSeparator | `bool` | Gets or sets whether a date separator is rendered between messages when the day changes. |
| Style | `string?` | Gets or sets the inline CSS style. |
| SystemPrompt | `string?` | Gets or sets the system prompt. |
| Temperature | `double?` | Gets or sets the temperature. |
| TimestampFormat | `string` | Gets or sets the format used to render the timestamp shown next to each message. Defaults to "HH:mm". |
| Title | `string?` | Gets or sets the title displayed in the chat header. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| UserAvatarText | `string` | Gets or sets the text displayed in the user avatar. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ChatCleared | `EventCallback` | Event callback that is invoked when the chat is cleared. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MessageAdded | `EventCallback<ChatMessage>` | Event callback that is invoked when a new message is added. |
| MessageSent | `EventCallback<string>` | Event callback that is invoked when a message is sent. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ResponseReceived | `EventCallback<string>` | Event callback that is invoked when the AI response is received. |
| SessionIdChanged | `EventCallback<string>` | Event callback that is invoked when a session ID is created or retrieved. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddMessage(string content, bool isUser) | `ChatMessage` | Adds a message to the chat. |
| ClearChat() | `Task` | Clears all messages from the chat. |
| GetMessages() | `IReadOnlyList<ChatMessage>` | Gets the current list of messages. |
| GetSessionId() | `string?` | Gets the current session ID. |
| LoadConversationHistory() | `Task` | Loads conversation history from the AI service session. |
| LoadMessages(IEnumerable<ChatMessage> messages) | `Task` | Loads messages into the chat, replacing any existing ones. Preserves the timestamp of each message — use this to restore conversation history. |
| SendMessage(string content) | `Task` | Sends a message programmatically. |
| SendMessage(string content, string? model, string? systemPrompt, double? temperature, int? maxTokens, string? endpoint, string? proxy, string? apiKey, string? apiKeyHeader) | `Task` | Sends a message programmatically. |

