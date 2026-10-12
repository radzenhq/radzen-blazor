using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
        private readonly Queue<QueuedMessage> queuedMessages = new();
        private readonly HashSet<ChatMessage> takenMessages = new();

        private sealed record QueuedMessage(ChatMessage UserMessage, Microsoft.Extensions.AI.ChatMessage Request, string? Model, string? SystemPrompt, double? Temperature, int? MaxTokens, string? Endpoint, string? Proxy, string? ApiKey, string? ApiKeyHeader);
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
        /// Gets or sets the user the conversation belongs to. Stored in <see cref="ConversationSession.UserId"/> so that <see cref="IConversationStore"/> implementations can list the conversations of a user.
        /// </summary>
        [Parameter]
        public string? UserId { get; set; }

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

        /// <summary>
        /// Gets or sets the content rendered in the chat header between <see cref="Title"/> and the clear button, for example a conversation switcher.
        /// The header is rendered when a title, the clear button or this template is set.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the content rendered in the input area above the text box, below the pending attachments, for example a status line or a notice about the attachments.
        /// </summary>
        [Parameter]
        public RenderFragment? InputHeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the content rendered in the input area below the text box, for example a model picker or action buttons.
        /// </summary>
        [Parameter]
        public RenderFragment? InputFooterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the text of the input. Bind it with <c>@bind-Input</c> to read what the user has typed or to set the text from code, for example to offer a prompt.
        /// </summary>
        [Parameter]
        public string? Input { get; set; }

        /// <summary>
        /// Event callback that is invoked when the text of the input changes.
        /// </summary>
        [Parameter]
        public EventCallback<string> InputChanged { get; set; }

        /// <summary>
        /// Gets or sets whether messages sent while a response is being generated are accepted instead of ignored. Default is <c>false</c>.
        /// Such a message appears in the conversation right away, marked as queued, and is offered to the service with <see cref="IAIChatService.TryQueueMessageAsync"/> as a request whose
        /// <see cref="Microsoft.Extensions.AI.ChatMessage.MessageId"/> is the <see cref="ChatMessage.Id"/> of the displayed message. A service that takes it tells the chat when the model reads it by
        /// yielding a <see cref="ChatResponseUpdate"/> with <see cref="ChatRole.User"/> and that id: the message then moves to the end of the conversation, loses the queued mark and the rest of
        /// the response goes into a new assistant message under it. A service that does not take the message is sent it with the next request once the response completes;
        /// <see cref="Stop"/> drops the messages waiting for that and returns their text to the input, while messages a service took stay for the service to remove with <see cref="RemoveMessage"/>.
        /// The send button stays next to the stop button while the response streams.
        /// </summary>
        [Parameter]
        public bool QueueMessages { get; set; }

        /// <summary>
        /// Gets the messages waiting to be sent once the current response completes. See <see cref="QueueMessages"/>.
        /// </summary>
        public IReadOnlyList<ChatMessage> QueuedMessages => queuedMessages.Select(queued => queued.UserMessage).ToList();

        /// <summary>
        /// Gets or sets whether the avatars of the user and the assistant are rendered next to the messages. Default is <c>true</c>.
        /// </summary>
        [Parameter]
        public bool ShowAvatars { get; set; } = true;

        /// <summary>
        /// Gets or sets the text of the send button. Default is <c>null</c>: the button shows only its icon.
        /// </summary>
        [Parameter]
        public string? SendButtonText { get; set; }

        /// <summary>
        /// Gets or sets the icon of the send button. Default is <c>send</c>.
        /// </summary>
        [Parameter]
        public string SendButtonIcon { get; set; } = "send";

        /// <summary>
        /// Gets or sets the text of the stop button shown while a response streams. Default is <c>null</c>: the button shows only its icon.
        /// </summary>
        [Parameter]
        public string? StopButtonText { get; set; }

        /// <summary>
        /// Gets or sets the icon of the stop button. Default is <c>stop_circle</c>.
        /// </summary>
        [Parameter]
        public string StopButtonIcon { get; set; } = "stop_circle";

        /// <summary>
        /// Gets or sets the template rendered next to the time of an assistant message instead of the default token count, when <see cref="ShowUsage"/> is <c>true</c> and the message has <see cref="ChatMessage.Usage"/>.
        /// Use it to show what <see cref="UsageDetails.AdditionalCounts"/> carries, for example credits.
        /// </summary>
        [Parameter]
        public RenderFragment<ChatMessage>? UsageTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template rendered at the end of every message, after its text and sources, for example the files an answer changed.
        /// </summary>
        [Parameter]
        public RenderFragment<ChatMessage>? MessageFooterTemplate { get; set; }

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
        /// Gets or sets the endpoint URL of the AI provider, overriding <see cref="AIChatServiceOptions.Endpoint"/>. In a WebAssembly application use <see cref="Proxy"/> instead, so the request goes through your server.
        /// </summary>
        [Parameter]
        public string? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the URL of a server endpoint that forwards the requests to the AI provider and adds the API key, overriding <see cref="AIChatServiceOptions.Proxy"/>.
        /// </summary>
        [Parameter]
        public string? Proxy { get; set; }

        /// <summary>
        /// Gets or sets the API key, overriding <see cref="AIChatServiceOptions.ApiKey"/>. Blazor Server only: the component throws when a key is set in a WebAssembly application, because it would be visible to every user. Keep the key on the server and use <see cref="Proxy"/>.
        /// </summary>
        [Parameter]
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the header the API key is sent with, overriding <see cref="AIChatServiceOptions.ApiKeyHeader"/>.
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
        /// Gets or sets whether the user can attach files to messages. Images are sent to the model as image content and need a multimodal model;
        /// text files (txt, csv, md, json, xml and similar) are sent as text, PDF files as file content for providers that support it. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool AllowAttachments { get; set; }

        /// <summary>
        /// Gets or sets the accepted attachment types as an <c>accept</c> attribute value, for example <c>image/*,.txt,.csv,.pdf</c>. Default is <c>image/*</c>.
        /// </summary>
        [Parameter]
        public string AcceptedAttachmentTypes { get; set; } = "image/*";

        /// <summary>
        /// Gets or sets the maximum size of an attachment in bytes. Default is 4 MB.
        /// </summary>
        [Parameter]
        public long MaxAttachmentSize { get; set; } = 4 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum total size in bytes of the attachments of a message. A file the user picks that would push the pending attachments over it is not attached and <see cref="Error"/> is raised.
        /// Checked after <see cref="AttachmentAdding"/>, on the attachment it leaves. Attachments added from code with <see cref="AddAttachment"/> are not checked. Default is <c>null</c>: no limit.
        /// </summary>
        [Parameter]
        public long? MaxTotalAttachmentSize { get; set; }

        /// <summary>
        /// Gets or sets whether the token usage reported by the model is shown next to the time of assistant messages. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool ShowUsage { get; set; }

        /// <summary>
        /// Event callback that is invoked when an attachment cannot be read, for example because it exceeds <see cref="MaxAttachmentSize"/>.
        /// </summary>
        [Parameter]
        public EventCallback<Exception> Error { get; set; }

        /// <summary>
        /// Event callback that is invoked with every file the user picks before it is attached. Set <see cref="ChatAttachmentAddingEventArgs.Cancel"/> to reject it or replace
        /// <see cref="ChatAttachmentAddingEventArgs.Attachment"/>, for example with a scaled down image. Files added from code with <see cref="AddAttachment"/> do not raise it.
        /// </summary>
        [Parameter]
        public EventCallback<ChatAttachmentAddingEventArgs> AttachmentAdding { get; set; }

        private readonly List<ChatAttachment> pendingAttachments = new();
        private readonly string attachInputId = $"rz-chat-attach-{Guid.NewGuid():N}";

        /// <summary>
        /// Gets the attachments that will be sent with the next message.
        /// </summary>
        public IReadOnlyList<ChatAttachment> PendingAttachments => pendingAttachments;

        /// <summary>
        /// Adds an attachment to the next message as it is: <see cref="AttachmentAdding"/>, <see cref="MaxAttachmentSize"/> and <see cref="MaxTotalAttachmentSize"/> apply only to files the user picks.
        /// </summary>
        /// <param name="attachment">The attachment.</param>
        public void AddAttachment(ChatAttachment attachment)
        {
            ArgumentNullException.ThrowIfNull(attachment);
            pendingAttachments.Add(attachment);
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Removes an attachment from the next message.
        /// </summary>
        /// <param name="attachment">The attachment.</param>
        public void RemoveAttachment(ChatAttachment attachment)
        {
            pendingAttachments.Remove(attachment);
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Removes all pending attachments.
        /// </summary>
        public void ClearAttachments()
        {
            pendingAttachments.Clear();
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Stops the response that is being generated. The text received so far is kept.
        /// </summary>
        public void Stop()
        {
            if (queuedMessages.Count > 0)
            {
                var texts = new List<string>();

                while (queuedMessages.Count > 0)
                {
                    var queued = queuedMessages.Dequeue();
                    Messages.Remove(queued.UserMessage);

                    if (queued.UserMessage.Content.Length > 0)
                    {
                        texts.Add(queued.UserMessage.Content);
                    }

                    pendingAttachments.AddRange(queued.UserMessage.Attachments);
                }

                if (CurrentInput.Length > 0)
                {
                    texts.Add(CurrentInput);
                }

                _ = SetInput(string.Join(Environment.NewLine, texts));
            }

            if (IsLoading)
            {
                cts.Cancel();
            }
            else
            {
                InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Focuses the input.
        /// </summary>
        public ValueTask FocusAsync() => inputElement.Context != null ? inputElement.FocusAsync() : ValueTask.CompletedTask;

        private async Task SetInput(string value)
        {
            if (CurrentInput == value)
            {
                return;
            }

            CurrentInput = value;
            Input = value;
            await InputChanged.InvokeAsync(value);
        }

        private async Task OnFilesSelected(InputFileChangeEventArgs args)
        {
            foreach (var file in args.GetMultipleFiles())
            {
                try
                {
                    using var stream = file.OpenReadStream(MaxAttachmentSize);
                    using var memory = new System.IO.MemoryStream();
                    await stream.CopyToAsync(memory);

                    var attachment = new ChatAttachment { Name = file.Name, MediaType = string.IsNullOrEmpty(file.ContentType) ? ChatAttachment.GetMediaType(file.Name) : file.ContentType, Data = memory.ToArray() };

                    if (AttachmentAdding.HasDelegate)
                    {
                        var adding = new ChatAttachmentAddingEventArgs(attachment);
                        await AttachmentAdding.InvokeAsync(adding);

                        if (adding.Cancel)
                        {
                            continue;
                        }

                        attachment = adding.Attachment ?? attachment;
                    }

                    if (MaxTotalAttachmentSize != null && pendingAttachments.Sum(pending => (long)pending.Data.Length) + attachment.Data.Length > MaxTotalAttachmentSize)
                    {
                        throw new System.IO.IOException($"Attaching {attachment.Name} ({attachment.Data.Length} bytes) would exceed the total attachment limit of {MaxTotalAttachmentSize} bytes.");
                    }

                    pendingAttachments.Add(attachment);
                }
                catch (Exception exception) when (exception is System.IO.IOException or InvalidOperationException)
                {
                    await Error.InvokeAsync(exception);
                }
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task OnAttachClick()
        {
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.clickElement", attachInputId);
            }
        }

        private string FormatUsage(ChatMessage message)
        {
            var total = message.Usage?.TotalTokenCount ?? ((message.Usage?.InputTokenCount ?? 0) + (message.Usage?.OutputTokenCount ?? 0));

            return string.Format(Culture ?? System.Globalization.CultureInfo.CurrentCulture, Localize(nameof(RadzenStrings.AIChat_UsageFormat)), total);
        }

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
        /// Gets or sets whether the sources of an answer are rendered under it. Default is <c>true</c>.
        /// Sources are the <see cref="CitationAnnotation"/> annotations the provider attaches to the response and the <see cref="ChatCitation"/> instances tools return.
        /// </summary>
        [Parameter]
        public bool ShowCitations { get; set; } = true;

        /// <summary>
        /// Gets or sets the template used to render a source. The default renders a numbered link with the title and the snippet as tooltip.
        /// </summary>
        [Parameter]
        public RenderFragment<ChatCitation>? CitationTemplate { get; set; }

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
        /// Gets or sets the template that renders every message in place of the default rendering. Call <see cref="RenderMessage"/> from it to keep the default rendering for some messages,
        /// for example to render only the entries the application adds with <see cref="AddMessage(ChatMessage)"/> your way, or to add content under a message.
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
            return AddMessage(new ChatMessage
            {
                Content = content,
                UserId = isUser ? "user" : "system",
                IsUser = isUser,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Adds a message to the chat: a message of the conversation, or an entry of another <see cref="ChatMessage.Kind"/> such as a status line, a progress indicator or an error.
        /// Messages added this way are shown but not sent to the model and not saved in the conversation.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The message.</returns>
        public ChatMessage AddMessage(ChatMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            Messages.Add(message);

            while (Messages.Count > MaxMessages)
            {
                Messages.RemoveAt(0);
            }

            InvokeAsync(StateHasChanged);
            return message;
        }

        /// <summary>
        /// Removes a message from the chat, for example a <see cref="ChatMessageKind.Progress"/> entry that ended. The conversation sent to the model is not affected.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns><c>true</c> when the message was in the chat.</returns>
        public bool RemoveMessage(ChatMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            var removed = Messages.Remove(message);

            if (removed)
            {
                InvokeAsync(StateHasChanged);
            }

            return removed;
        }

        /// <summary>
        /// Renders the chat again, for example after changing the <see cref="ChatMessage.Content"/> of a message it shows.
        /// </summary>
        public void Refresh() => InvokeAsync(StateHasChanged);

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
            queuedMessages.Clear();

            if (!string.IsNullOrEmpty(currentSessionId))
            {
                await ChatService.ClearSessionAsync(currentSessionId);
            }

            await ChatCleared.InvokeAsync();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Sends a message programmatically.
        /// </summary>
        /// <param name="content">The message content to send.</param>
        public Task SendMessage(string content) => SendMessage(content, Model, SystemPrompt, Temperature, MaxTokens, Endpoint, Proxy, ApiKey, ApiKeyHeader);

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
            if ((string.IsNullOrWhiteSpace(content) && pendingAttachments.Count == 0) || Disabled || (IsLoading && !QueueMessages))
            {
                return;
            }

            content ??= string.Empty;

            var attachments = pendingAttachments.ToList();
            pendingAttachments.Clear();

            var userMessage = AddMessage(content, true);
            userMessage.Attachments = attachments;
            await MessageAdded.InvokeAsync(userMessage);
            await MessageSent.InvokeAsync(content);

            await SetInput(string.Empty);
            await InvokeAsync(StateHasChanged);

            var contents = new List<AIContent>();

            if (content.Length > 0)
            {
                contents.Add(new TextContent(content));
            }

            contents.AddRange(attachments.Select(attachment => (AIContent)attachment.ToDataContent()));

            var request = new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, contents) { MessageId = userMessage.Id };

            if (IsLoading)
            {
                if (await ChatService.TryQueueMessageAsync(request, currentSessionId, cts.Token))
                {
                    takenMessages.Add(userMessage);
                }
                else
                {
                    queuedMessages.Enqueue(new QueuedMessage(userMessage, request, model, systemPrompt, temperature, maxTokens, endpoint, proxy, apiKey, apiKeyHeader));
                }

                await InvokeAsync(StateHasChanged);
                return;
            }

            await GetAIResponse(request, model, systemPrompt, temperature, maxTokens, endpoint, proxy, apiKey, apiKeyHeader);
        }

        private bool IsQueued(ChatMessage message) => takenMessages.Contains(message) || (queuedMessages.Count > 0 && queuedMessages.Any(queued => queued.UserMessage == message));

        private bool TryMarkRead(string messageId)
        {
            var message = Messages.FirstOrDefault(candidate => candidate.IsUser && candidate.Id == messageId);

            if (message == null)
            {
                return false;
            }

            takenMessages.Remove(message);
            Messages.Remove(message);
            Messages.Add(message);

            return true;
        }

        private async Task CompleteAssistantMessage(ChatMessage message)
        {
            ChatCitation.NormalizeMarkers(message);
            message.IsStreaming = false;

            if (message.Content.Length == 0 && message.ToolCalls.Count == 0)
            {
                Messages.Remove(message);
                return;
            }

            await ResponseReceived.InvokeAsync(message.Content);
            await MessageAdded.InvokeAsync(message);
        }

        private string GetMessageCssClass(ChatMessage message)
        {
            return ClassList.Create("rz-chat-message")
                .Add("rz-chat-message-user", message.IsUser)
                .Add("rz-chat-message-assistant", !message.IsUser)
                .Add("rz-chat-message-queued", IsQueued(message))
                .ToString();
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

            var session = await ChatService.GetOrCreateSessionAsync(currentSessionId, UserId);

            Messages.Clear();

            foreach (var message in session.Messages)
            {
                var copy = AddMessage(message.Content, message.IsUser);
                copy.Timestamp = message.Timestamp;
                copy.ToolCalls = message.ToolCalls;
                copy.Attachments = message.Attachments;
                copy.Reasoning = message.Reasoning;
                copy.Usage = message.Usage;
                copy.Citations = message.Citations;
                copy.AdditionalProperties = message.AdditionalProperties;
                copy.Kind = message.Kind;
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
            await GetSingleAIResponse(request, model, systemPrompt, temperature, maxTokens, endpoint, proxy, apiKey, apiKeyHeader);

            while (!Disabled && queuedMessages.Count > 0)
            {
                var queued = queuedMessages.Dequeue();

                if (!Messages.Remove(queued.UserMessage))
                {
                    continue;
                }

                Messages.Add(queued.UserMessage);

                await GetSingleAIResponse(queued.Request, queued.Model, queued.SystemPrompt, queued.Temperature, queued.MaxTokens, queued.Endpoint, queued.Proxy, queued.ApiKey, queued.ApiKeyHeader);
            }
        }

        private async Task GetSingleAIResponse(Microsoft.Extensions.AI.ChatMessage request, string? model, string? systemPrompt, double? temperature, int? maxTokens, string? endpoint, string? proxy, string? apiKey, string? apiKeyHeader)
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

            var assistantMessage = AddMessage(new ChatMessage { UserId = "system", IsUser = false, Timestamp = DateTime.Now, IsStreaming = true });

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
                await foreach (var update in ChatService.GetStreamingResponseAsync(request, currentSessionId, options, endpoint, proxy, apiKey, apiKeyHeader, UserId, cts.Token))
                {
                    if (update.Role == ChatRole.User)
                    {
                        if (!string.IsNullOrEmpty(update.MessageId) && TryMarkRead(update.MessageId))
                        {
                            await CompleteAssistantMessage(assistantMessage);

                            assistantMessage = AddMessage(new ChatMessage { UserId = "system", IsUser = false, Timestamp = DateTime.Now, IsStreaming = true });
                            text.Clear();

                            await InvokeAsync(StateHasChanged);
                        }

                        continue;
                    }

                    foreach (var content in update.Contents)
                    {
                        ApplyContent(assistantMessage, content, text, completedCalls);
                    }

                    assistantMessage.MergeAdditionalProperties(update.AdditionalProperties);
                    assistantMessage.Content = text.ToString();
                    await InvokeAsync(StateHasChanged);
                }

                ChatCitation.NormalizeMarkers(assistantMessage);
                assistantMessage.IsStreaming = false;

                foreach (var call in completedCalls)
                {
                    await ToolCallCompleted.InvokeAsync(call);
                }

                await ResponseReceived.InvokeAsync(assistantMessage.Content);
                await MessageAdded.InvokeAsync(assistantMessage);
            }
            catch (OperationCanceledException)
            {
                assistantMessage.IsStreaming = false;

                if (assistantMessage.Content.Length > 0 || assistantMessage.ToolCalls.Count > 0)
                {
                    await ResponseReceived.InvokeAsync(assistantMessage.Content);
                    await MessageAdded.InvokeAsync(assistantMessage);
                }
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
                takenMessages.Clear();

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
                    AIChatService.ApplyCitations(message, content);

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

        private sealed record MessageSegment(string? Text, List<ChatToolCall>? ToolCalls);

        private List<MessageSegment> GetSegments(ChatMessage message)
        {
            var segments = new List<MessageSegment>();

            if (!ShowToolCalls)
            {
                if (!string.IsNullOrEmpty(message.Content))
                {
                    segments.Add(new MessageSegment(message.Content, null));
                }

                return segments;
            }

            List<ChatToolCall>? calls = null;

            foreach (var part in message.GetParts())
            {
                if (part.ToolCall != null)
                {
                    calls ??= new List<ChatToolCall>();
                    calls.Add(part.ToolCall);
                    continue;
                }

                if (calls != null)
                {
                    segments.Add(new MessageSegment(null, calls));
                    calls = null;
                }

                segments.Add(new MessageSegment(part.Text, null));
            }

            if (calls != null)
            {
                segments.Add(new MessageSegment(null, calls));
            }

            return segments;
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

            currentSessionId = SessionId ?? Guid.NewGuid().ToString();

            if (currentSessionId != SessionId)
            {
                await SessionIdChanged.InvokeAsync(currentSessionId);
            }
            else
            {
                await LoadConversationHistory();
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
            await SetInput(e.Value?.ToString() ?? "");
            await InvokeAsync(StateHasChanged);
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var inputChanged = parameters.DidParameterChange(nameof(Input), Input);

            await base.SetParametersAsync(parameters);

            if (inputChanged)
            {
                CurrentInput = Input ?? string.Empty;
            }
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
            if (!string.IsNullOrWhiteSpace(CurrentInput) || pendingAttachments.Count > 0)
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
