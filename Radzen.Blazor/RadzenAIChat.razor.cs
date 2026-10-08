using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
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
        private bool IsLoading { get; set; }
        private bool preventDefault;
        private ElementReference inputElement;
        private ElementReference messagesContainer;
        private CancellationTokenSource cts = new();
        private string? currentSessionId;

        /// <summary>
        /// Gets or sets the session ID for maintaining conversation memory. If null, a new session will be created.
        /// </summary>
        [Parameter]
        public string? SessionId { get; set; }

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
        public IReadOnlyDictionary<string, object>? InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets the title displayed in the chat header.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        private string? placeholder;

        /// <summary>
        /// Gets or sets the placeholder text for the input field.
        /// </summary>
        [Parameter]
        public string Placeholder { get => placeholder ?? Localize(nameof(RadzenStrings.AIChat_Placeholder)); set => placeholder = value; }

        private string? emptyMessage;

        /// <summary>
        /// Gets or sets the message displayed when there are no messages.
        /// </summary>
        [Parameter]
        public string EmptyMessage { get => emptyMessage ?? Localize(nameof(RadzenStrings.AIChat_EmptyMessage)); set => emptyMessage = value; }

        private string? userAvatarText;

        /// <summary>
        /// Gets or sets the text displayed in the user avatar.
        /// </summary>
        [Parameter]
        public string UserAvatarText { get => userAvatarText ?? Localize(nameof(RadzenStrings.AIChat_UserAvatarText)); set => userAvatarText = value; }

        private string? assistantAvatarText;

        /// <summary>
        /// Gets or sets the text displayed in the assistant avatar.
        /// </summary>
        [Parameter]
        public string AssistantAvatarText { get => assistantAvatarText ?? Localize(nameof(RadzenStrings.AIChat_AssistantAvatarText)); set => assistantAvatarText = value; }

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        [Parameter]
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the system prompt.
        /// </summary>
        [Parameter]
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// Gets or sets the temperature.
        /// </summary>
        [Parameter]
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the max tokens.
        /// </summary>
        [Parameter]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the endpoint URL for the AI service.
        /// </summary>
        [Parameter]
        public string? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the proxy URL for the AI service.
        /// </summary>
        [Parameter]
        public string? Proxy { get; set; }

        /// <summary>
        /// Gets or sets the API key for authentication.
        /// </summary>
        [Parameter]
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the API key header name.
        /// </summary>
        [Parameter]
        public string? ApiKeyHeader { get; set; }

        /// <summary>
        /// Gets or sets the tools the model can call. Create them with <see cref="AIFunctionFactory"/>; wrap a tool in <see cref="ApprovalRequiredAIFunction"/> to ask the user before it runs.
        /// Tools are invoked automatically and rendered in the assistant message; use <see cref="ToolCallTemplate"/> to customize how.
        /// </summary>
        [Parameter]
        public IEnumerable<AITool>? Tools { get; set; }

        /// <summary>
        /// Gets or sets whether the model may call several tools in one response. <c>null</c> (the default) leaves it to the provider.
        /// Set to <c>false</c> when using tools that require approval so that each call is approved on its own.
        /// </summary>
        [Parameter]
        public bool? AllowMultipleToolCalls { get; set; }

        /// <summary>
        /// Gets or sets whether the reasoning of models that expose it is rendered as a collapsible block in assistant messages. Default is <c>true</c>.
        /// </summary>
        [Parameter]
        public bool ShowReasoning { get; set; } = true;

        /// <summary>
        /// Gets or sets whether tool calls are rendered in assistant messages. Default is <c>true</c>.
        /// </summary>
        [Parameter]
        public bool ShowToolCalls { get; set; } = true;

        /// <summary>
        /// Gets or sets the template used to render a tool call. The template is responsible for rendering approval UI for calls in the
        /// <see cref="ChatToolCallStatus.AwaitingApproval"/> state; call <see cref="ApproveToolCall"/> or <see cref="RejectToolCall"/> to answer them.
        /// </summary>
        [Parameter]
        public RenderFragment<ChatToolCall>? ToolCallTemplate { get; set; }

        /// <summary>
        /// Event callback that is invoked when a tool call completes, fails or is rejected.
        /// </summary>
        [Parameter]
        public EventCallback<ChatToolCall> ToolCallCompleted { get; set; }

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
        /// Gets or sets the maximum number of messages to keep in the chat.
        /// </summary>
        [Parameter]
        public int MaxMessages { get; set; } = 100;

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
        public string? GetSessionId() => currentSessionId;

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
                UserId = isUser ? "user" : "system",
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
        /// Loads messages into the chat, replacing any existing ones. Preserves the timestamp of each message — use this to restore conversation history.
        /// </summary>
        /// <param name="messages">The messages to load.</param>
        public async Task LoadMessages(IEnumerable<ChatMessage> messages)
        {
            Messages.Clear();

            if (messages != null)
            {
                Messages.AddRange(messages);

                while (Messages.Count > MaxMessages)
                {
                    Messages.RemoveAt(0);
                }
            }

            await InvokeAsync(StateHasChanged);
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
            {
                return;
            }

            // Add user message
            var userMessage = AddMessage(content, true);
            await MessageAdded.InvokeAsync(userMessage);
            await MessageSent.InvokeAsync(content);

            // Clear input
            CurrentInput = string.Empty;
            await InvokeAsync(StateHasChanged);

            // Get AI response
            await GetAIResponse(content, Model, SystemPrompt, Temperature, MaxTokens, Endpoint, Proxy, ApiKey, ApiKeyHeader);
        }

        /// <summary>
        /// Sends a message programmatically with custom AI parameters.
        /// </summary>
        /// <param name="content">The message content to send.</param>
        /// <param name="model">Optional model name to override the configured model.</param>
        /// <param name="systemPrompt">Optional system prompt to override the configured system prompt.</param>
        /// <param name="temperature">Optional temperature to override the configured temperature.</param>
        /// <param name="maxTokens">Optional maximum tokens to override the configured max tokens.</param>
        /// <param name="endpoint">Optional endpoint URL to override the configured endpoint.</param>
        /// <param name="proxy">Optional proxy URL to override the configured proxy.</param>
        /// <param name="apiKey">Optional API key to override the configured API key.</param>
        /// <param name="apiKeyHeader">Optional API key header name to override the configured header.</param>
        public async Task SendMessage(string content, string? model = null, string? systemPrompt = null, double? temperature = null, int? maxTokens = null, string? endpoint = null, string? proxy = null, string? apiKey = null, string? apiKeyHeader = null)
        {
            if (string.IsNullOrWhiteSpace(content) || Disabled || IsLoading)
            {
                return;
            }

            // Add user message
            var userMessage = AddMessage(content, true);
            await MessageAdded.InvokeAsync(userMessage);
            await MessageSent.InvokeAsync(content);

            // Clear input
            CurrentInput = string.Empty;
            await InvokeAsync(StateHasChanged);

            // Get AI response with custom parameters
            await GetAIResponse(content, model, systemPrompt, temperature, maxTokens, endpoint, proxy, apiKey, apiKeyHeader);
        }

        /// <summary>
        /// Approves a tool call that is awaiting the user's approval. The tool is invoked and the model continues its response.
        /// When the same message holds other calls that still await approval the answer is sent once all of them are answered.
        /// </summary>
        /// <param name="call">The tool call.</param>
        public Task ApproveToolCall(ChatToolCall call) => RespondToToolApproval(call, true);

        /// <summary>
        /// Rejects a tool call that is awaiting the user's approval. The model is told the call was rejected and continues its response.
        /// </summary>
        /// <param name="call">The tool call.</param>
        public Task RejectToolCall(ChatToolCall call) => RespondToToolApproval(call, false);

        private async Task RespondToToolApproval(ChatToolCall call, bool approved)
        {
            ArgumentNullException.ThrowIfNull(call);

            if (call.Status != ChatToolCallStatus.AwaitingApproval || call.ApprovalRequest == null || Disabled || IsLoading)
            {
                return;
            }

            call.Status = approved ? ChatToolCallStatus.Pending : ChatToolCallStatus.Rejected;
            call.ApprovalResponse = call.ApprovalRequest.CreateResponse(approved);

            if (!approved)
            {
                await ToolCallCompleted.InvokeAsync(call);
            }

            var message = Messages.FirstOrDefault(m => m.ToolCalls.Contains(call));
            var siblings = message?.ToolCalls ?? [call];

            if (siblings.Any(sibling => sibling.Status == ChatToolCallStatus.AwaitingApproval))
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            await InvokeAsync(StateHasChanged);

            var responses = siblings.Where(sibling => sibling.ApprovalResponse != null).Select(sibling => (AIContent)sibling.ApprovalResponse!).ToList();

            foreach (var sibling in siblings)
            {
                sibling.ApprovalResponse = null;
            }

            await GetAIResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, responses), Model, SystemPrompt, Temperature, MaxTokens, Endpoint, Proxy, ApiKey, ApiKeyHeader);
        }

        /// <summary>
        /// Loads conversation history from the AI service session.
        /// </summary>
        public async Task LoadConversationHistory()
        {
            if (string.IsNullOrEmpty(currentSessionId))
            {
                return;
            }

            var session = ChatService.GetOrCreateSession(currentSessionId);
            
            // Clear current messages
            Messages.Clear();
            
            foreach (var message in session.Messages)
            {
                var copy = AddMessage(message.Content, message.IsUser);
                copy.Timestamp = message.Timestamp;
                copy.ToolCalls = message.ToolCalls;
            }
            
            await InvokeAsync(StateHasChanged);
        }

        private Task GetAIResponse(string userInput, string? model = null, string? systemPrompt = null, double? temperature = null, int? maxTokens = null, string? endpoint = null, string? proxy = null, string? apiKey = null, string? apiKeyHeader = null)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return Task.CompletedTask;
            }

            return GetAIResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userInput), model, systemPrompt, temperature, maxTokens, endpoint, proxy, apiKey, apiKeyHeader);
        }

        private async Task GetAIResponse(Microsoft.Extensions.AI.ChatMessage request, string? model, string? systemPrompt, double? temperature, int? maxTokens, string? endpoint, string? proxy, string? apiKey, string? apiKeyHeader)
        {
            IsLoading = true;
            var previousCts = cts;
#if NET8_0_OR_GREATER
            await previousCts.CancelAsync();
#else
            previousCts.Cancel();
#endif
            previousCts.Dispose();
            cts = new CancellationTokenSource();

            if (string.IsNullOrEmpty(currentSessionId))
            {
                currentSessionId = SessionId ?? Guid.NewGuid().ToString();
                await SessionIdChanged.InvokeAsync(currentSessionId);
            }

            var assistantMessage = AddMessage("", false);
            assistantMessage.IsStreaming = true;

            var options = new ChatOptions
            {
                ModelId = model,
                Instructions = systemPrompt,
                Temperature = temperature.HasValue ? (float)temperature.Value : null,
                MaxOutputTokens = maxTokens,
                Tools = Tools?.ToList(),
                AllowMultipleToolCalls = AllowMultipleToolCalls
            };

            var text = new StringBuilder();
            var completedCalls = new List<ChatToolCall>();

            try
            {
                await foreach (var update in ChatService.GetStreamingResponseAsync(request, currentSessionId, options, endpoint, proxy, apiKey, apiKeyHeader, cts.Token))
                {
                    foreach (var content in update.Contents)
                    {
                        ApplyContent(assistantMessage, content, text, completedCalls);
                    }

                    assistantMessage.Content = text.ToString();
                    await InvokeAsync(StateHasChanged);
                }

                assistantMessage.IsStreaming = false;

                foreach (var call in completedCalls)
                {
                    await ToolCallCompleted.InvokeAsync(call);
                }

                await ResponseReceived.InvokeAsync(assistantMessage.Content);
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

                if (assistantMessage.Content.Length == 0 && assistantMessage.ToolCalls.Count == 0)
                {
                    Messages.Remove(assistantMessage);
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private void ApplyContent(ChatMessage message, AIContent content, StringBuilder text, List<ChatToolCall> completedCalls)
        {
            if (content is FunctionCallContent functionCall)
            {
                var existing = FindToolCall(functionCall.CallId);

                if (existing != null)
                {
                    existing.Name = functionCall.Name;
                    existing.Arguments = functionCall.Arguments;

                    if (existing.Status == ChatToolCallStatus.AwaitingApproval)
                    {
                        existing.Status = ChatToolCallStatus.Pending;
                    }

                    return;
                }
            }

            if (content is FunctionResultContent result)
            {
                var call = FindToolCall(result.CallId);

                if (call != null)
                {
                    call.Result = result.Result;
                    call.Exception = result.Exception;

                    if (call.Status != ChatToolCallStatus.Rejected)
                    {
                        call.Status = result.Exception != null ? ChatToolCallStatus.Failed : ChatToolCallStatus.Completed;
                        completedCalls.Add(call);
                    }

                    return;
                }
            }

            AIChatService.ApplyContent(message, content, text);
        }

        private ChatToolCall? FindToolCall(string callId)
        {
            for (var index = Messages.Count - 1; index >= 0; index--)
            {
                var call = Messages[index].ToolCalls.FirstOrDefault(toolCall => toolCall.CallId == callId);

                if (call != null)
                {
                    return call;
                }
            }

            return null;
        }

        private static string GetToolCallIcon(ChatToolCall call)
        {
            return call.Status switch
            {
                ChatToolCallStatus.Pending => "progress_activity",
                ChatToolCallStatus.AwaitingApproval => "help",
                ChatToolCallStatus.Completed => "check_circle",
                ChatToolCallStatus.Failed => "error",
                ChatToolCallStatus.Rejected => "block",
                _ => "build"
            };
        }

        private string GetToolCallStatusText(ChatToolCall call)
        {
            return call.Status switch
            {
                ChatToolCallStatus.Pending => Localize(nameof(RadzenStrings.AIChat_ToolCallPending)),
                ChatToolCallStatus.AwaitingApproval => Localize(nameof(RadzenStrings.AIChat_ToolCallAwaitingApproval)),
                ChatToolCallStatus.Completed => Localize(nameof(RadzenStrings.AIChat_ToolCallCompleted)),
                ChatToolCallStatus.Failed => Localize(nameof(RadzenStrings.AIChat_ToolCallFailed)),
                ChatToolCallStatus.Rejected => Localize(nameof(RadzenStrings.AIChat_ToolCallRejected)),
                _ => string.Empty
            };
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
            if (!string.IsNullOrEmpty(SessionId) && SessionId != currentSessionId)
            {
                currentSessionId = SessionId;
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
            if (!firstRender && messagesContainer.Context != null && JSRuntime != null)
            {
                // Scroll to bottom when new messages are added
                await JSRuntime.InvokeVoidAsync("Radzen.chatScrollAfterRender", ".rz-chat-messages", 100);
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
            base.Dispose();
            
            cts?.Cancel();
            cts?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
