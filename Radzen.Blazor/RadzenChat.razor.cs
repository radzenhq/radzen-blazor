using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Provides information about a typing state change in <see cref="RadzenChat"/>.
    /// </summary>
    public class ChatTypingEventArgs
    {
        /// <summary>
        /// Gets or sets the participant ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the participant is typing.
        /// </summary>
        public bool IsTyping { get; set; }
    }

    /// <summary>
    /// Represents a chat participant in the RadzenChat component.
    /// </summary>
    public class ChatUser
    {
        /// <summary>
        /// Gets or sets the unique identifier for the participant.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the display name of the participant.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the avatar URL for the participant.
        /// </summary>
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the color theme for the participant's messages.
        /// </summary>
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the participant is currently online.
        /// </summary>
        public bool IsOnline { get; set; } = true;

        /// <summary>
        /// Gets or sets additional metadata for the participant.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets the initials for the participant based on their name.
        /// </summary>
        /// <returns>The initials string.</returns>
        public string GetInitials()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return "?";
            }

            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper(CultureInfo.InvariantCulture);
            }

            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpper(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// A chat interface component for displaying and sending messages in multi-participant conversations.
    /// RadzenChat provides a complete chat UI with message history, user avatars, typing indicators, and message composition.
    /// Creates a messaging interface similar to modern chat applications, ideal for customer support chats, team collaboration, messaging features, or AI chatbots.
    /// Features multi-user support displaying messages from multiple participants with avatars and names, chronological message list with sender identification,
    /// user avatars showing photos or initials with customizable colors, optional "User is typing..." feedback, text input with Send button for new messages,
    /// customizable templates for message rendering/empty state/typing indicator, automatic scrolling to newest messages, and message send time stamps.
    /// Provide a list of ChatUser objects for participants and ChatMessage objects for message history. Set CurrentUserId to distinguish the current user's messages (typically right-aligned) from others (left-aligned).
    /// Handle MessageSent to process new messages (save to database, send to server, etc.).
    /// </summary>
    /// <example>
    /// Basic chat:
    /// <code>
    /// &lt;RadzenChat CurrentUserId=@currentUserId Users=@users Messages=@messages MessageSent=@OnMessageSent /&gt;
    /// @code {
    ///     string currentUserId = "user1";
    ///     List&lt;ChatUser&gt; users = new() { new ChatUser { Id = "user1", Name = "John" }, new ChatUser { Id = "user2", Name = "Jane" } };
    ///     List&lt;ChatMessage&gt; messages = new();
    ///     
    ///     async Task OnMessageSent(ChatMessage message)
    ///     {
    ///         messages.Add(message);
    ///         await SaveMessage(message);
    ///     }
    /// }
    /// </code>
    /// </example>
    public partial class RadzenChat : RadzenComponent
    {
        private List<ChatMessage> internalMessages { get; set; } = new();
        private string CurrentInput { get; set; } = string.Empty;
        private bool IsLoading { get; set; }
        private bool preventDefault;
        private ElementReference inputElement;
        private ElementReference messagesContainer;
        private bool hasNewMessages;
        private int previousMessageCount;

        private readonly HashSet<string> typingUsers = new();
        private bool currentUserIsTyping;
        private CancellationTokenSource? typingCts;

        // Mention-related fields
        private List<MentionUserContext> mentionSearchResults = new();
        private bool isMentionPopupOpen;
        private string mentionSearchText = string.Empty;
        private int selectedMentionIndex = -1;
        private int mentionStartPosition = -1;
        private CancellationTokenSource? mentionSearchCts;
        private readonly List<MentionInputSegment> mentionInputSegments = new();

        private sealed class MentionInputSegment
        {
            public int Start { get; set; }
            public int Length { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string DisplayText { get; set; } = string.Empty;

            public int End => Start + Length;
        }

        /// <summary>
        /// Gets or sets the message template.
        /// </summary>
        /// <value>The message template.</value>
        [Parameter]
        public RenderFragment<ChatMessage>? MessageTemplate { get; set; }

        /// <summary>
        /// Gets or sets the empty template shown when there are no messages.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment? EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets the current user's participant ID.
        /// </summary>
        [Parameter]
        public string CurrentUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of chat users.
        /// </summary>
        [Parameter]
        public IEnumerable<ChatUser> Users { get; set; } = new List<ChatUser>();

        /// <summary>
        /// Event callback that is invoked when the users list changes.
        /// </summary>
        [Parameter]
        public EventCallback<IEnumerable<ChatUser>> UsersChanged { get; set; }

        /// <summary>
        /// Gets or sets the list of chat messages.
        /// </summary>
        [Parameter]
        public IEnumerable<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Event callback that is invoked when the messages list changes.
        /// </summary>
        [Parameter]
        public EventCallback<IEnumerable<ChatMessage>> MessagesChanged { get; set; }

        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object>? InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets whether to show a typing indicator in the message list.
        /// </summary>
        [Parameter]
        public bool ShowTypingIndicator { get; set; } = false;

        /// <summary>
        /// Gets or sets the debounce timeout (in milliseconds) after the last keystroke before the current user is considered "not typing".
        /// </summary>
        [Parameter]
        public int TypingTimeout { get; set; } = 1500;

        /// <summary>
        /// Raised when the current user's typing state changes (true/false). Use this to broadcast typing state via SignalR etc.
        /// </summary>
        [Parameter]
        public EventCallback<ChatTypingEventArgs> TypingChanged { get; set; }

        /// <summary>
        /// Optional template to render typing indicator content. Receives the typing <see cref="ChatUser"/> list (excluding current user by default).
        /// </summary>
        [Parameter]
        public RenderFragment<IReadOnlyList<ChatUser>>? TypingTemplate { get; set; }

        /// <summary>
        /// Gets or sets the single user typing format. <see cref="TypingTemplate" /> has preference over this property.
        /// </summary>
        /// <value>The single user typing format.</value>
        [Parameter]
        public string? TypingFormat { get; set; } = "{0} is typing...";

        /// <summary>
        /// Gets or sets the two users typing format. <see cref="TypingTemplate" /> has preference over this property.
        /// </summary>
        /// <value>The two users typing format.</value>
        [Parameter]
        public string? TwoUsersTypingFormat { get; set; } = "{0} and {1} are typing...";

        /// <summary>
        /// Gets or sets the multiple users typing format. <see cref="TypingTemplate" /> has preference over this property.
        /// </summary>
        /// <value>The two multiple typing format.</value>
        [Parameter]
        public string? MultipleUsersTypingFormat { get; set; } = "{0} and {1} others are typing...";

        /// <summary>
        /// Gets or sets the title displayed in the chat header. <see cref="TitleContent" /> has preference over this property.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        private string? placeholder;

        /// <summary>
        /// Gets or sets the custom title content rendered in the chat header.
        /// </summary>
        [Parameter]
        public RenderFragment? TitleContent { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text for the input field.
        /// </summary>
        [Parameter]
        public string Placeholder { get => placeholder ?? Localize(nameof(RadzenStrings.Chat_Placeholder)); set => placeholder = value; }

        private string? emptyMessage;

        /// <summary>
        /// Gets or sets the message displayed when there are no messages.
        /// </summary>
        [Parameter]
        public string EmptyMessage { get => emptyMessage ?? Localize(nameof(RadzenStrings.Chat_EmptyMessage)); set => emptyMessage = value; }

        private string? newMessagesText;

        /// <summary>
        /// Gets or sets the text displayed on the new messages indicator button.
        /// </summary>
        [Parameter]
        public string NewMessagesText { get => newMessagesText ?? Localize(nameof(RadzenStrings.Chat_NewMessagesText)); set => newMessagesText = value; }

        /// <summary>
        /// Gets or sets the format used to render the timestamp shown next to each message. Defaults to <c>"HH:mm"</c>.
        /// </summary>
        [Parameter]
        public string TimestampFormat { get; set; } = "HH:mm";

        /// <summary>
        /// Gets or sets whether a date separator is rendered between messages when the day changes.
        /// </summary>
        [Parameter]
        public bool ShowDateSeparator { get; set; } = true;

        /// <summary>
        /// Gets or sets the format used to render the date separator. Defaults to <c>"D"</c> (long date pattern).
        /// </summary>
        [Parameter]
        public string DateSeparatorFormat { get; set; } = "D";

        /// <summary>
        /// Optional template to render the date separator shown between messages when the day changes. Receives the <see cref="DateTime"/> of the following message.
        /// </summary>
        [Parameter]
        public RenderFragment<DateTime>? DateSeparatorTemplate { get; set; }

        /// <summary>
        /// Gets or sets whether to show participant names above messages.
        /// </summary>
        [Parameter]
        public bool ShowUserNames { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show users in the header.
        /// </summary>
        [Parameter]
        public bool ShowUsers { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of users to show in the header.
        /// </summary>
        [Parameter]
        public int MaxVisibleUsers { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to show the clear chat button.
        /// </summary>
        [Parameter]
        public bool ShowClearButton { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the chat is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets whether the input is read-only.
        /// </summary>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of messages to keep in the chat.
        /// </summary>
        [Parameter]
        public int MaxMessages { get; set; } = 100;

        /// <summary>
        /// Event callback that is invoked when a new message is added.
        /// </summary>
        [Parameter]
        public EventCallback<ChatMessage> MessageAdded { get; set; }

        /// <summary>
        /// Event callback that is invoked when the chat is cleared.
        /// </summary>
        [Parameter]
        public EventCallback ChatCleared { get; set; }

        /// <summary>
        /// Event callback that is invoked when a message is sent.
        /// </summary>
        [Parameter]
        public EventCallback<ChatMessage> MessageSent { get; set; }

        /// <summary>
        /// Event callback that is invoked when a participant is added.
        /// </summary>
        [Parameter]
        public EventCallback<ChatUser> UserAdded { get; set; }

        /// <summary>
        /// Event callback that is invoked when a participant is removed.
        /// </summary>
        [Parameter]
        public EventCallback<ChatUser> UserRemoved { get; set; }

        /// <summary>
        /// Gets or sets the character that triggers the mention search popup (e.g., '@'). When null, mention feature is disabled.
        /// </summary>
        [Parameter]
        public char? MentionCharacter { get; set; }

        /// <summary>
        /// Event callback that is invoked when a mention search is triggered.
        /// The callback receives a <see cref="MentionSearchArgs"/> with the search filter and pagination info.
        /// The app should populate the search results which will be displayed in the mention popup.
        /// </summary>
        [Parameter]
        public EventCallback<MentionSearchArgs> MentionSearch { get; set; }

        /// <summary>
        /// Gets or sets the template for rendering individual items in the mention search popup.
        /// Receives a <see cref="MentionUserContext"/> containing the user ID, name, and chat status.
        /// </summary>
        [Parameter]
        public RenderFragment<MentionUserContext>? MentionItemTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for rendering a stored mention in the chat message.
        /// Receives the user ID string (from @[userid] format) and should render how the mention is displayed (e.g., as a badge).
        /// </summary>
        [Parameter]
        public RenderFragment<string>? MentionDisplayTemplate { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of mention search results to display in the popup. Defaults to 10.
        /// </summary>
        [Parameter]
        public int MentionMaxResults { get; set; } = 10;

        /// <summary>
        /// Gets or sets the event callback that stores mention search results from the MentionSearch callback.
        /// </summary>
        [Parameter]
        public EventCallback<IEnumerable<MentionUserContext>> MentionSearchResultsChanged { get; set; }

        /// <summary>
        /// Updates the mention search results and refreshes the popup.
        /// Call this from within your MentionSearch event callback handler.
        /// </summary>
        /// <param name="results">The search results to display.</param>
        public async Task SetMentionSearchResults(IEnumerable<MentionUserContext>? results)
        {
            mentionSearchResults = results?.ToList() ?? new();
            selectedMentionIndex = mentionSearchResults.Count > 0 ? 0 : -1;
            isMentionPopupOpen = mentionSearchResults.Count > 0;

            if (MentionSearchResultsChanged.HasDelegate)
            {
                await MentionSearchResultsChanged.InvokeAsync(mentionSearchResults);
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Gets the current list of messages.
        /// </summary>
        public IReadOnlyList<ChatMessage> GetMessages() => Messages.ToList().AsReadOnly();

        /// <summary>
        /// Gets the current list of users.
        /// </summary>
        public IReadOnlyList<ChatUser> GetUsers() => Users.ToList().AsReadOnly();

        /// <summary>
        /// Gets a participant by their ID.
        /// </summary>
        /// <param name="userId">The participant ID.</param>
        /// <returns>The participant or null if not found.</returns>
        public ChatUser? GetUser(string userId)
        {
            return Users.FirstOrDefault(p => p.Id == userId);
        }

        /// <summary>
        /// Adds a message to the chat.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <param name="userId">The ID of the participant who sent the message.</param>
        /// <returns>The created message.</returns>
        public async Task<ChatMessage> AddMessage(string content, string userId)
        {
            var message = new ChatMessage
            {
                Content = content,
                UserId = userId,
                Timestamp = DateTime.Now
            };

            var messagesList = Messages.ToList();
            messagesList.Add(message);

            // Limit the number of messages
            if (messagesList.Count > MaxMessages)
            {
                messagesList.RemoveAt(0);
            }

            await MessagesChanged.InvokeAsync(messagesList);
            await InvokeAsync(StateHasChanged);
            return message;
        }

        /// <summary>
        /// Adds a participant to the chat.
        /// </summary>
        /// <param name="participant">The participant to add.</param>
        public async Task AddUser(ChatUser participant)
        {
            var usersList = Users.ToList();
            if (!usersList.Any(p => p.Id == participant.Id))
            {
                usersList.Add(participant);
                await UsersChanged.InvokeAsync(usersList);
                await UserAdded.InvokeAsync(participant);
            }
        }

        /// <summary>
        /// Removes a participant from the chat.
        /// </summary>
        /// <param name="userId">The ID of the participant to remove.</param>
        public async Task RemoveUser(string userId)
        {
            var usersList = Users.ToList();
            var participant = usersList.FirstOrDefault(p => p.Id == userId);
            if (participant != null)
            {
                usersList.Remove(participant);
                await UsersChanged.InvokeAsync(usersList);
                await UserRemoved.InvokeAsync(participant);
            }
        }

        /// <summary>
        /// Clears all messages from the chat.
        /// </summary>
        public async Task ClearChat()
        {
            await MessagesChanged.InvokeAsync(new List<ChatMessage>());
            await ChatCleared.InvokeAsync();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Sends a message programmatically.
        /// </summary>
        /// <param name="content">The message content to send.</param>
        /// <param name="userId">The ID of the participant sending the message (defaults to CurrentUserId).</param>
        public async Task SendMessage(string content, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(content) || Disabled || IsLoading)
            {
                return;
            }

            var senderId = userId ?? CurrentUserId;
            if (string.IsNullOrEmpty(senderId))
            {
                return;
            }

            // Add message
            var message = await AddMessage(content, senderId);
            await MessageAdded.InvokeAsync(message);
            await MessageSent.InvokeAsync(message);

            // Clear input
            CurrentInput = string.Empty;
            mentionInputSegments.Clear();
            await SetCurrentUserTyping(false);
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Sets a participant typing state. Use this for remote users (e.g. SignalR updates).
        /// </summary>
        public async Task SetUserTyping(string userId, bool isTyping)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            bool changed;
            if (isTyping)
            {
                changed = typingUsers.Add(userId);
            }
            else
            {
                changed = typingUsers.Remove(userId);
            }

            if (changed)
            {
                await InvokeAsync(StateHasChanged);
            }
        }

        internal IReadOnlyList<ChatUser> GetTypingUsersForDisplay()
        {
            // Don't show current user typing by default.
            return typingUsers
                .Where(id => id != CurrentUserId)
                .Select(id => GetUser(id) ?? new ChatUser { Id = id, Name = id })
                .ToList()
                .AsReadOnly();
        }

        internal string GetTypingText(IReadOnlyList<ChatUser> users)
        {
            if (users == null || users.Count == 0)
            {
                return string.Empty;
            }

            if (users.Count == 1)
            {
                return string.Format(Culture ?? CultureInfo.CurrentCulture, TypingFormat ?? string.Empty, users[0].Name);
            }

            if (users.Count == 2)
            {
                return string.Format(Culture ?? CultureInfo.CurrentCulture, TwoUsersTypingFormat ?? string.Empty, users[0].Name, users[1].Name);
            }

            return string.Format(Culture ?? CultureInfo.CurrentCulture, MultipleUsersTypingFormat ?? string.Empty, users[0].Name, users.Count - 1);
        }

        /// <summary>
        /// Updates a participant's online status.
        /// </summary>
        /// <param name="userId">The participant ID.</param>
        /// <param name="isOnline">The online status.</param>
        public async Task UpdateUserStatus(string userId, bool isOnline)
        {
            var usersList = Users.ToList();
            var participant = usersList.FirstOrDefault(p => p.Id == userId);
            if (participant != null)
            {
                participant.IsOnline = isOnline;
                await UsersChanged.InvokeAsync(usersList);
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Loads messages from an external source.
        /// </summary>
        /// <param name="messages">The messages to load.</param>
        public async Task LoadMessages(IEnumerable<ChatMessage> messages)
        {
            await MessagesChanged.InvokeAsync(messages);
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnInput(ChangeEventArgs e)
        {
            var previousInput = CurrentInput;
            CurrentInput = e.Value?.ToString() ?? "";
            ReconcileMentionInputSegments(previousInput, CurrentInput);
            await NotifyCurrentUserTyping();
            await DetectMentionTrigger(CurrentInput);
            await InvokeAsync(StateHasChanged);
        }

        async Task NotifyCurrentUserTyping()
        {
            if (Disabled || ReadOnly)
            {
                return;
            }

            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return;
            }

            await SetCurrentUserTyping(true);

            typingCts?.Cancel();
            typingCts?.Dispose();
            typingCts = new CancellationTokenSource();
            var token = typingCts.Token;

            try
            {
                var timeout = Math.Max(250, TypingTimeout);
                await Task.Delay(timeout, token);
                if (!token.IsCancellationRequested)
                {
                    await SetCurrentUserTyping(false);
                }
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }

        async Task SetCurrentUserTyping(bool isTyping)
        {
            if (currentUserIsTyping == isTyping)
            {
                return;
            }

            currentUserIsTyping = isTyping;

            // Keep our own typing state in the internal set so the template can include it if needed,
            // but the default display excludes CurrentUserId.
            if (!string.IsNullOrEmpty(CurrentUserId))
            {
                if (isTyping)
                {
                    typingUsers.Add(CurrentUserId);
                }
                else
                {
                    typingUsers.Remove(CurrentUserId);
                }
            }

            if (TypingChanged.HasDelegate && !string.IsNullOrEmpty(CurrentUserId))
            {
                await TypingChanged.InvokeAsync(new ChatTypingEventArgs { UserId = CurrentUserId, IsTyping = isTyping });
            }
        }

        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            // Handle mention popup navigation
            if (isMentionPopupOpen && MentionCharacter.HasValue)
            {
                if (e.Key == "ArrowDown")
                {
                    selectedMentionIndex = Math.Min(selectedMentionIndex + 1, mentionSearchResults.Count - 1);
                    preventDefault = true;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
                else if (e.Key == "ArrowUp")
                {
                    selectedMentionIndex = Math.Max(selectedMentionIndex - 1, 0);
                    preventDefault = true;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
                else if (e.Key == "Enter")
                {
                    if (selectedMentionIndex >= 0 && selectedMentionIndex < mentionSearchResults.Count)
                    {
                        await InsertMention(mentionSearchResults[selectedMentionIndex]);
                        preventDefault = true;
                        return;
                    }
                }
                else if (e.Key == "Escape")
                {
                    await CloseMentionPopup();
                    preventDefault = true;
                    return;
                }
            }

            if (MentionCharacter.HasValue && mentionInputSegments.Count > 0 && (e.Key == "Backspace" || e.Key == "Delete"))
            {
                if (await HandleMentionDeletion(e.Key))
                {
                    preventDefault = true;
                    return;
                }
            }

            // Handle normal message sending
            if (e.Key == "Enter" && !e.ShiftKey && JSRuntime != null)
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", inputElement, "");
                preventDefault = true;
                await OnSendMessage();
            }

            preventDefault = false;
        }

        private async Task OnSendMessage()
        {
            var content = BuildStoredMessageInput();
            if (!string.IsNullOrWhiteSpace(content))
            {
                await SendMessage(content);
            }
        }

        private async Task DetectMentionTrigger(string input)
        {
            if (!MentionCharacter.HasValue || MentionSearch.HasDelegate == false)
            {
                return;
            }

            var char_code = MentionCharacter.Value;
            var trimmedInput = input.TrimEnd();

            // Check if mention character appears at the start or after whitespace
            int mentionPos = -1;
            
            for (int i = trimmedInput.Length - 1; i >= 0; i--)
            {
                if (trimmedInput[i] == char_code)
                {
                    // Check if it's at the start or after whitespace
                    if (i == 0 || char.IsWhiteSpace(trimmedInput[i - 1]))
                    {
                        mentionPos = i;
                    }
                    break;
                }
                else if (char.IsWhiteSpace(trimmedInput[i]))
                {
                    break;
                }
            }

            if (mentionPos >= 0)
            {
                // Extract search text after the mention character
                var searchText = trimmedInput.Substring(mentionPos + 1);
                
                // Check if this is a continuous mention (no spaces in search text)
                if (!searchText.Contains(' ', StringComparison.Ordinal))
                {
                    mentionSearchText = searchText;
                    mentionStartPosition = mentionPos;
                    await PerformMentionSearch(searchText);
                    return;
                }
            }

            await CloseMentionPopup();
        }

        private async Task PerformMentionSearch(string filter)
        {
            if (!MentionSearch.HasDelegate)
            {
                return;
            }

            mentionSearchCts?.Cancel();
            mentionSearchCts?.Dispose();
            mentionSearchCts = new CancellationTokenSource();

            try
            {
                var searchArgs = new MentionSearchArgs 
                { 
                    Filter = filter,
                    Skip = 0,
                    Top = MentionMaxResults
                };

                await MentionSearch.InvokeAsync(searchArgs);
                isMentionPopupOpen = true;
                selectedMentionIndex = 0;
                
                if (MentionSearchResultsChanged.HasDelegate)
                {
                    await MentionSearchResultsChanged.InvokeAsync(mentionSearchResults);
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled
            }
        }

        private async Task InsertMention(MentionUserContext user)
        {
            if (user?.UserId == null)
            {
                return;
            }

            // Replace the search text with the mention
            var beforeMention = CurrentInput.Substring(0, mentionStartPosition);
            var afterMention = CurrentInput.Substring(mentionStartPosition + 1 + mentionSearchText.Length);
            var displayName = ResolveMentionDisplayName(user);
            var displayMention = $"{MentionCharacter}{displayName}";
            var replacedLength = 1 + mentionSearchText.Length;

            ShiftMentionSegmentsAfterEdit(mentionStartPosition, replacedLength, displayMention.Length);
            mentionInputSegments.Add(new MentionInputSegment
            {
                Start = mentionStartPosition,
                Length = displayMention.Length,
                UserId = user.UserId,
                DisplayText = displayMention
            });

            CurrentInput = beforeMention + displayMention + afterMention;

            await CloseMentionPopup();
            await InvokeAsync(StateHasChanged);
        }

        private async Task CloseMentionPopup()
        {
            isMentionPopupOpen = false;
            mentionSearchResults.Clear();
            mentionSearchText = string.Empty;
            selectedMentionIndex = -1;
            mentionStartPosition = -1;
            
            mentionSearchCts?.Cancel();
            mentionSearchCts?.Dispose();
            mentionSearchCts = null;

            if (MentionSearchResultsChanged.HasDelegate)
            {
                await MentionSearchResultsChanged.InvokeAsync(mentionSearchResults);
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task OnMentionItemClick(MentionUserContext user)
        {
            await InsertMention(user);
        }

        private string BuildStoredMessageInput()
        {
            if (mentionInputSegments.Count == 0 || !MentionCharacter.HasValue)
            {
                return CurrentInput;
            }

            var orderedSegments = mentionInputSegments.OrderBy(segment => segment.Start).ToList();
            var builder = new StringBuilder();
            var currentIndex = 0;

            foreach (var segment in orderedSegments)
            {
                if (segment.Start < currentIndex || segment.End > CurrentInput.Length)
                {
                    continue;
                }

                var segmentText = CurrentInput.Substring(segment.Start, segment.Length);
                if (!string.Equals(segmentText, segment.DisplayText, StringComparison.Ordinal))
                {
                    continue;
                }

                builder.Append(CurrentInput.AsSpan(currentIndex, segment.Start - currentIndex));
                builder.Append(MentionCharacter.Value);
                builder.Append('[');
                builder.Append(segment.UserId);
                builder.Append(']');
                currentIndex = segment.End;
            }

            if (currentIndex < CurrentInput.Length)
            {
                builder.Append(CurrentInput.AsSpan(currentIndex));
            }

            return builder.ToString();
        }

        private string ResolveMentionDisplayName(MentionUserContext user)
        {
            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                return user.UserName;
            }

            var userId = user.UserId ?? string.Empty;
            var chatUser = GetUser(userId);
            if (!string.IsNullOrWhiteSpace(chatUser?.Name))
            {
                return chatUser.Name;
            }

            return userId;
        }

        private void ReconcileMentionInputSegments(string previousInput, string nextInput)
        {
            if (mentionInputSegments.Count == 0 || string.Equals(previousInput, nextInput, StringComparison.Ordinal))
            {
                return;
            }

            var commonPrefixLength = 0;
            var maxPrefixLength = Math.Min(previousInput.Length, nextInput.Length);
            while (commonPrefixLength < maxPrefixLength && previousInput[commonPrefixLength] == nextInput[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            var previousSuffixIndex = previousInput.Length;
            var nextSuffixIndex = nextInput.Length;
            while (previousSuffixIndex > commonPrefixLength &&
                   nextSuffixIndex > commonPrefixLength &&
                   previousInput[previousSuffixIndex - 1] == nextInput[nextSuffixIndex - 1])
            {
                previousSuffixIndex--;
                nextSuffixIndex--;
            }

            var previousChangedEnd = previousSuffixIndex;
            var lengthDelta = nextInput.Length - previousInput.Length;

            var updatedSegments = new List<MentionInputSegment>(mentionInputSegments.Count);
            foreach (var segment in mentionInputSegments.OrderBy(segment => segment.Start))
            {
                if (segment.End <= commonPrefixLength)
                {
                    updatedSegments.Add(segment);
                    continue;
                }

                if (segment.Start >= previousChangedEnd)
                {
                    segment.Start += lengthDelta;
                    updatedSegments.Add(segment);
                    continue;
                }
            }

            mentionInputSegments.Clear();
            mentionInputSegments.AddRange(updatedSegments);
        }

        private void ShiftMentionSegmentsAfterEdit(int editStart, int replacedLength, int insertedLength)
        {
            if (mentionInputSegments.Count == 0)
            {
                return;
            }

            var editEnd = editStart + replacedLength;
            var delta = insertedLength - replacedLength;

            mentionInputSegments.RemoveAll(segment => segment.Start < editEnd && segment.End > editStart);

            foreach (var segment in mentionInputSegments.Where(segment => segment.Start >= editEnd))
            {
                segment.Start += delta;
            }
        }

        private async Task<bool> HandleMentionDeletion(string key)
        {
            if (JSRuntime == null)
            {
                return false;
            }

            var selection = await JSRuntime.InvokeAsync<int[]?>("Radzen.getSelectionRange", inputElement);
            if (selection == null || selection.Length < 2)
            {
                return false;
            }

            var selectionStart = selection[0];
            var selectionEnd = selection[1];

            MentionInputSegment? segmentToRemove = null;
            if (selectionStart != selectionEnd)
            {
                segmentToRemove = mentionInputSegments.FirstOrDefault(segment => segment.Start < selectionEnd && segment.End > selectionStart);
            }
            else
            {
                var targetIndex = key == "Backspace" ? selectionStart - 1 : selectionStart;
                if (targetIndex < 0)
                {
                    return false;
                }

                segmentToRemove = mentionInputSegments.FirstOrDefault(segment => segment.Start <= targetIndex && segment.End > targetIndex);
            }

            if (segmentToRemove == null)
            {
                return false;
            }

            CurrentInput = CurrentInput.Remove(segmentToRemove.Start, segmentToRemove.Length);
            mentionInputSegments.Remove(segmentToRemove);
            foreach (var segment in mentionInputSegments.Where(segment => segment.Start > segmentToRemove.Start))
            {
                segment.Start -= segmentToRemove.Length;
            }

            await JSRuntime.InvokeVoidAsync("Radzen.setInputValue", inputElement, CurrentInput);
            await JSRuntime.InvokeVoidAsync("Radzen.setSelectionRange", inputElement, segmentToRemove.Start, segmentToRemove.Start);
            await DetectMentionTrigger(CurrentInput);
            await InvokeAsync(StateHasChanged);
            return true;
        }

        internal List<(int start, int end, string userId)> ParseMentions(string text)
        {
            var mentions = new List<(int, int, string)>();
            
            if (string.IsNullOrEmpty(text) || !MentionCharacter.HasValue)
            {
                return mentions;
            }

            var pattern = $@"\{MentionCharacter}\[([^\]]+)\]";
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(text))
            {
                var userId = match.Groups[1].Value;
                mentions.Add((match.Index, match.Length, userId));
            }

            return mentions;
        }

        private RenderFragment RenderMessageWithMentions(string text)
        {
            return builder =>
            {
                if (string.IsNullOrEmpty(text) || !MentionCharacter.HasValue || MentionDisplayTemplate == null)
                {
                    builder.AddContent(0, new MarkupString($"<p>{System.Net.WebUtility.HtmlEncode(text)}</p>"));
                    return;
                }

                var mentions = ParseMentions(text);
                if (mentions.Count == 0)
                {
                    builder.AddContent(0, new MarkupString($"<p>{System.Net.WebUtility.HtmlEncode(text)}</p>"));
                    return;
                }

                int lastIndex = 0;
                int contentIndex = 0;

                builder.OpenElement(contentIndex++, "p");

                foreach (var mention in mentions)
                {
                    // Add text before mention
                    if (mention.start > lastIndex)
                    {
                        var textBefore = text.Substring(lastIndex, mention.start - lastIndex);
                        builder.AddContent(contentIndex++, textBefore);
                    }

                    // Add the mention using the template
                    var userId = mention.userId;
                    builder.AddContent(contentIndex++, MentionDisplayTemplate(userId));

                    lastIndex = mention.start + mention.end;
                }

                // Add remaining text after last mention
                if (lastIndex < text.Length)
                {
                    var textAfter = text.Substring(lastIndex);
                    builder.AddContent(contentIndex++, textAfter);
                }

                builder.CloseElement();
            };
        }

        private async Task OnClearChat()
        {
            await ClearChat();
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (messagesContainer.Context != null && JSRuntime != null)
            {
                var currentCount = Messages.Count();

                if (firstRender || currentCount != previousMessageCount)
                {
                    if (firstRender)
                    {
                        // Always scroll to bottom on first render
                        await JSRuntime.InvokeVoidAsync("Radzen.chatScrollToBottom", messagesContainer);
                    }
                    else if (currentCount > previousMessageCount)
                    {
                        // New messages added - only scroll if user is near bottom
                        var isNearBottom = await JSRuntime.InvokeAsync<bool>("Radzen.chatIsNearBottom", messagesContainer, 50);

                        if (isNearBottom)
                        {
                            await JSRuntime.InvokeVoidAsync("Radzen.chatScrollToBottom", messagesContainer);
                        }
                        else
                        {
                            hasNewMessages = true;
                            StateHasChanged();
                        }
                    }
                    else if (currentCount < previousMessageCount)
                    {
                        // Messages cleared
                        hasNewMessages = false;
                    }

                    previousMessageCount = currentCount;
                }
            }
        }

        private async Task OnMessagesScroll()
        {
            if (hasNewMessages && messagesContainer.Context != null && JSRuntime != null)
            {
                var isNearBottom = await JSRuntime.InvokeAsync<bool>("Radzen.chatIsNearBottom", messagesContainer, 50);

                if (isNearBottom)
                {
                    hasNewMessages = false;
                }
            }
        }

        /// <summary>
        /// Scrolls to the bottom of the message list and dismisses the new messages indicator.
        /// </summary>
        public async Task ScrollToBottom()
        {
            hasNewMessages = false;

            if (messagesContainer.Context != null && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.chatScrollToBottom", messagesContainer);
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-chat").ToString();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            try
            {
                typingCts?.Cancel();
                typingCts?.Dispose();
                typingCts = null;

                mentionSearchCts?.Cancel();
                mentionSearchCts?.Dispose();
                mentionSearchCts = null;
            }
            catch
            {
                // ignore
            }

            base.Dispose();
        }
    }
}
