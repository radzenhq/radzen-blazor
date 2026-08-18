# RadzenChat API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| CurrentUserId | `string` | Gets or sets the current user's participant ID. |
| DateSeparatorFormat | `string` | Gets or sets the format used to render the date separator. Defaults to "D" (long date pattern). |
| DateSeparatorTemplate | `RenderFragment<DateTime>?` | Optional template to render the date separator shown between messages when the day changes. Receives the of the following message. |
| Disabled | `bool` | Gets or sets whether the chat is disabled. |
| EmptyMessage | `string` | Gets or sets the message displayed when there are no messages. |
| EmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when there are no messages. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| MaxMessages | `int` | Gets or sets the maximum number of messages to keep in the chat. |
| MaxVisibleUsers | `int` | Gets or sets the maximum number of users to show in the header. |
| MentionCharacter | `char?` | Gets or sets the character that triggers the mention search popup (e.g., '@'). When null, mention feature is disabled. |
| MentionDisplaySize | `int` | Gets or sets the number of mention users to display per page. Defaults to 10. |
| MentionDisplayTemplate | `RenderFragment<string>?` | Gets or sets the template for rendering a stored mention in the chat message. Receives the user ID string (from @[userid] format) and should render how the mention is displayed (e.g., as a badge). |
| MentionItemTemplate | `RenderFragment<MentionUserContext>?` | Gets or sets the template for rendering individual items in the mention search popup. Receives a containing the user ID, name, and chat status. |
| MentionMaxResults | `int` | Gets or sets the maximum number of mention search results to display in the popup. Defaults to 10. Kept for backward compatibility. Use instead. |
| MentionUsers | `IEnumerable<MentionUserContext>` | Gets or sets the mention users displayed in the mention popup. Populate this property in the parent component from the callback. |
| MentionUsersCount | `int?` | Gets or sets the total mention users count for paging scenarios. When null, mention popup paging is disabled and only the provided page is displayed. |
| MessageTemplate | `RenderFragment<ChatMessage>?` | Gets or sets the message template. |
| Messages | `IEnumerable<ChatMessage>` | Gets or sets the list of chat messages. |
| MultipleUsersTypingFormat | `string?` | Gets or sets the multiple users typing format. has preference over this property. |
| NewMessagesText | `string` | Gets or sets the text displayed on the new messages indicator button. |
| Placeholder | `string` | Gets or sets the placeholder text for the input field. |
| ReadOnly | `bool` | Gets or sets whether the input is read-only. |
| ShowClearButton | `bool` | Gets or sets whether to show the clear chat button. |
| ShowDateSeparator | `bool` | Gets or sets whether a date separator is rendered between messages when the day changes. |
| ShowTypingIndicator | `bool` | Gets or sets whether to show a typing indicator in the message list. |
| ShowUserNames | `bool` | Gets or sets whether to show participant names above messages. |
| ShowUsers | `bool` | Gets or sets whether to show users in the header. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TimestampFormat | `string` | Gets or sets the format used to render the timestamp shown next to each message. Defaults to "HH:mm". |
| Title | `string?` | Gets or sets the title displayed in the chat header. has preference over this property. |
| TitleContent | `RenderFragment?` | Gets or sets the custom title content rendered in the chat header. |
| TwoUsersTypingFormat | `string?` | Gets or sets the two users typing format. has preference over this property. |
| TypingFormat | `string?` | Gets or sets the single user typing format. has preference over this property. |
| TypingTemplate | `RenderFragment<IReadOnlyList<ChatUser>>?` | Optional template to render typing indicator content. Receives the typing list (excluding current user by default). |
| TypingTimeout | `int` | Gets or sets the debounce timeout (in milliseconds) after the last keystroke before the current user is considered "not typing". |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Users | `IEnumerable<ChatUser>` | Gets or sets the list of chat users. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ChatCleared | `EventCallback` | Event callback that is invoked when the chat is cleared. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MentionSearch | `EventCallback<MentionSearchArgs>` | Event callback that is invoked when a mention search is triggered. The callback receives a with the search filter and pagination info. The app should populate the search results which will be displayed in the mention popup. |
| MessageAdded | `EventCallback<ChatMessage>` | Event callback that is invoked when a new message is added. |
| MessageSent | `EventCallback<ChatMessage>` | Event callback that is invoked when a message is sent. |
| MessagesChanged | `EventCallback<IEnumerable<ChatMessage>>` | Event callback that is invoked when the messages list changes. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| TypingChanged | `EventCallback<ChatTypingEventArgs>` | Raised when the current user's typing state changes (true/false). Use this to broadcast typing state via SignalR etc. |
| UserAdded | `EventCallback<ChatUser>` | Event callback that is invoked when a participant is added. |
| UserRemoved | `EventCallback<ChatUser>` | Event callback that is invoked when a participant is removed. |
| UsersChanged | `EventCallback<IEnumerable<ChatUser>>` | Event callback that is invoked when the users list changes. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddMessage(string content, string userId) | `Task<ChatMessage>` | Adds a message to the chat. |
| AddUser(ChatUser participant) | `Task` | Adds a participant to the chat. |
| ClearChat() | `Task` | Clears all messages from the chat. |
| GetMessages() | `IReadOnlyList<ChatMessage>` | Gets the current list of messages. |
| GetUser(string userId) | `ChatUser?` | Gets the current list of users. |
| GetUsers() | `IReadOnlyList<ChatUser>` | Gets the current list of users. |
| LoadMessages(IEnumerable<ChatMessage> messages) | `Task` | Loads messages from an external source. |
| RemoveUser(string userId) | `Task` | Removes a participant from the chat. |
| ScrollToBottom() | `Task` | Scrolls to the bottom of the message list and dismisses the new messages indicator. |
| SendMessage(string content, string? userId) | `Task` | Sends a message programmatically. |
| SetUserTyping(string userId, bool isTyping) | `Task` | Sets a participant typing state. Use this for remote users (e.g. SignalR updates). |
| UpdateUserStatus(string userId, bool isOnline) | `Task` | Updates a participant's online status. |

