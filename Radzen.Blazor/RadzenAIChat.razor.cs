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
    /// Represents a chat message in the RadzenAIChat component.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier for the message.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this message is from the user.
        /// </summary>
        public bool IsUser { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets whether this message is currently streaming.
        /// </summary>
        public bool IsStreaming { get; set; }
    }

    /// <summary>
    /// RadzenAIChat component that provides a modern chat interface with AI integration and conversation memory.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenAIChat Title="AI Assistant" Placeholder="Type your message..." @bind-Messages="@chatMessages" SessionId="@sessionId" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenAIChat : RadzenComponent
    {
        private List<ChatMessage> Messages { get; set; } = new();
        private string CurrentInput { get; set; } = string.Empty;
        private bool IsLoading { get; set; } = false;
        private bool preventDefault = false;
        private ElementReference inputElement;
        private ElementReference messagesContainer;
        private CancellationTokenSource cts = new();
        private string currentSessionId;

        /// <summary>
        /// Gets or sets the session ID for maintaining conversation memory. If null, a new session will be created.
        /// </summary>
        [Parameter]
        public string SessionId { get; set; }

        /// <summary>
        /// Event callback that is invoked when a session ID is created or retrieved.
        /// </summary>
        [Parameter]
        public EventCallback<string> SessionIdChanged { get; set; }

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
        /// Gets or sets the text displayed in the user avatar.
        /// </summary>
        [Parameter]
        public string UserAvatarText { get; set; } = "U";

        /// <summary>
        /// Gets or sets the text displayed in the assistant avatar.
        /// </summary>
        [Parameter]
        public string AssistantAvatarText { get; set; } = "AI";

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
        public EventCallback<string> MessageSent { get; set; }

        /// <summary>
        /// Event callback that is invoked when the AI response is received.
        /// </summary>
        [Parameter]
        public EventCallback<string> ResponseReceived { get; set; }

        /// <summary>
        /// Gets the current list of messages.
        /// </summary>
        public IReadOnlyList<ChatMessage> GetMessages() => Messages.AsReadOnly();

        /// <summary>
        /// Gets the current session ID.
        /// </summary>
        public string GetSessionId() => currentSessionId;

        /// <summary>
        /// Adds a message to the chat.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <param name="isUser">Whether the message is from the user.</param>
        /// <returns>The created message.</returns>
        public ChatMessage AddMessage(string content, bool isUser = false)
        {
            var message = new ChatMessage
            {
                Content = content,
                IsUser = isUser,
                Timestamp = DateTime.Now
            };

            Messages.Add(message);

            // Limit the number of messages
            if (Messages.Count > MaxMessages)
            {
                Messages.RemoveAt(0);
            }

            InvokeAsync(StateHasChanged);
            return message;
        }

        /// <summary>
        /// Clears all messages from the chat.
        /// </summary>
        public async Task ClearChat()
        {
            Messages.Clear();
            
            // Clear the session in the AI service
            if (!string.IsNullOrEmpty(currentSessionId))
            {
                ChatService.ClearSession(currentSessionId);
            }
            
            await ChatCleared.InvokeAsync();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Sends a message programmatically.
        /// </summary>
        /// <param name="content">The message content to send.</param>
        public async Task SendMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content) || Disabled || IsLoading)
                return;

            // Add user message
            var userMessage = AddMessage(content, true);
            await MessageAdded.InvokeAsync(userMessage);
            await MessageSent.InvokeAsync(content);

            // Clear input
            CurrentInput = string.Empty;
            await InvokeAsync(StateHasChanged);

            // Get AI response
            await GetAIResponse(content);
        }

        /// <summary>
        /// Sends a message programmatically with custom AI parameters.
        /// </summary>
        /// <param name="content">The message content to send.</param>
        /// <param name="model">Optional model name to override the configured model.</param>
        /// <param name="systemPrompt">Optional system prompt to override the configured system prompt.</param>
        /// <param name="temperature">Optional temperature to override the configured temperature.</param>
        /// <param name="maxTokens">Optional maximum tokens to override the configured max tokens.</param>
        public async Task SendMessage(string content, string model = null, string systemPrompt = null, double? temperature = null, int? maxTokens = null)
        {
            if (string.IsNullOrWhiteSpace(content) || Disabled || IsLoading)
                return;

            // Add user message
            var userMessage = AddMessage(content, true);
            await MessageAdded.InvokeAsync(userMessage);
            await MessageSent.InvokeAsync(content);

            // Clear input
            CurrentInput = string.Empty;
            await InvokeAsync(StateHasChanged);

            // Get AI response with custom parameters
            await GetAIResponse(content, model, systemPrompt, temperature, maxTokens);
        }

        /// <summary>
        /// Loads conversation history from the AI service session.
        /// </summary>
        public async Task LoadConversationHistory()
        {
            if (string.IsNullOrEmpty(currentSessionId))
                return;

            var session = ChatService.GetOrCreateSession(currentSessionId);
            
            // Clear current messages
            Messages.Clear();
            
            // Add messages from session history
            foreach (var message in session.Messages)
            {
                var isUser = message.Role.Equals("user", StringComparison.OrdinalIgnoreCase);
                AddMessage(message.Content, isUser);
            }
            
            await InvokeAsync(StateHasChanged);
        }

        private async Task GetAIResponse(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return;

            IsLoading = true;
            cts.Cancel();
            cts = new CancellationTokenSource();

            // Ensure we have a session ID
            if (string.IsNullOrEmpty(currentSessionId))
            {
                currentSessionId = SessionId ?? Guid.NewGuid().ToString();
                await SessionIdChanged.InvokeAsync(currentSessionId);
            }

            // Add assistant message placeholder
            var assistantMessage = AddMessage("", false);
            assistantMessage.IsStreaming = true;

            try
            {
                var response = "";
                await foreach (var token in ChatService.GetCompletionsAsync(userInput, currentSessionId, cts.Token))
                {
                    response += token;
                    assistantMessage.Content = response;
                    await InvokeAsync(StateHasChanged);
                }

                assistantMessage.IsStreaming = false;
                await ResponseReceived.InvokeAsync(response);
                await MessageAdded.InvokeAsync(assistantMessage);
            }
            catch (Exception ex)
            {
                assistantMessage.Content = $"Sorry, I encountered an error: {ex.Message}";
                assistantMessage.IsStreaming = false;
                await InvokeAsync(StateHasChanged);
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task GetAIResponse(string userInput, string model = null, string systemPrompt = null, double? temperature = null, int? maxTokens = null)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return;

            IsLoading = true;
            cts.Cancel();
            cts = new CancellationTokenSource();

            // Ensure we have a session ID
            if (string.IsNullOrEmpty(currentSessionId))
            {
                currentSessionId = SessionId ?? Guid.NewGuid().ToString();
                await SessionIdChanged.InvokeAsync(currentSessionId);
            }

            // Add assistant message placeholder
            var assistantMessage = AddMessage("", false);
            assistantMessage.IsStreaming = true;

            try
            {
                var response = "";
                await foreach (var token in ChatService.GetCompletionsAsync(userInput, currentSessionId, cts.Token, model, systemPrompt, temperature, maxTokens))
                {
                    response += token;
                    assistantMessage.Content = response;
                    await InvokeAsync(StateHasChanged);
                }

                assistantMessage.IsStreaming = false;
                await ResponseReceived.InvokeAsync(response);
                await MessageAdded.InvokeAsync(assistantMessage);
            }
            catch (Exception ex)
            {
                assistantMessage.Content = $"Sorry, I encountered an error: {ex.Message}";
                assistantMessage.IsStreaming = false;
                await InvokeAsync(StateHasChanged);
            }
            finally
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <inheritdoc />
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            
            // Initialize session ID
            currentSessionId = SessionId ?? Guid.NewGuid().ToString();
            if (currentSessionId != SessionId)
            {
                await SessionIdChanged.InvokeAsync(currentSessionId);
            }
        }

        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            
            // Update session ID if it changed
            if (SessionId != currentSessionId)
            {
                currentSessionId = SessionId ?? Guid.NewGuid().ToString();
                await SessionIdChanged.InvokeAsync(currentSessionId);
                
                // Load conversation history for the new session
                await LoadConversationHistory();
            }
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
