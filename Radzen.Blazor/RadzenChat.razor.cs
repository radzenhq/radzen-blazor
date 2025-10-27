using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
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
                return "?";

            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpper();
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
        private bool IsLoading { get; set; } = false;
        private bool preventDefault = false;
        private ElementReference inputElement;
        private ElementReference messagesContainer;

        /// <summary>
        /// Gets or sets the message template.
        /// </summary>
        /// <value>The message template.</value>
        [Parameter]
        public RenderFragment<ChatMessage> MessageTemplate { get; set; }

        /// <summary>
        /// Gets or sets the empty template shown when there are no messages.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

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
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets the title displayed in the chat header.
        /// </summary>
        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the placeholder text for the input field.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = "Type your message...";

        /// <summary>
        /// Gets or sets the message displayed when there are no messages.
        /// </summary>
        [Parameter]
        public string EmptyMessage { get; set; } = "No messages yet. Start a conversation!";

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
        public ChatUser GetUser(string userId)
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
        public async Task SendMessage(string content, string userId = null)
        {
            if (string.IsNullOrWhiteSpace(content) || Disabled || IsLoading)
                return;

            var senderId = userId ?? CurrentUserId;
            if (string.IsNullOrEmpty(senderId))
                return;

            // Add message
            var message = await AddMessage(content, senderId);
            await MessageAdded.InvokeAsync(message);
            await MessageSent.InvokeAsync(message);

            // Clear input
            CurrentInput = string.Empty;
            await InvokeAsync(StateHasChanged);
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
            CurrentInput = e.Value?.ToString() ?? "";
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey)
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", inputElement, "");
                preventDefault = true;
                await OnSendMessage();
            }
            else
            {
                preventDefault = false;
            }
        }

        private async Task OnSendMessage()
        {
            if (!string.IsNullOrWhiteSpace(CurrentInput))
            {
                await SendMessage(CurrentInput);
            }
        }

        private async Task OnClearChat()
        {
            await ClearChat();
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender && messagesContainer.Context != null)
            {
                // Scroll to bottom when new messages are added
                await JSRuntime.InvokeVoidAsync("eval", 
                    "setTimeout(() => { " +
                    "const container = document.querySelector('.rz-chat-messages'); " +
                    "if (container) container.scrollTop = container.scrollHeight; " +
                    "}, 100);");
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-chat").ToString();
        }
    }
}
