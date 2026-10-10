using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Radzen;

/// <summary>
/// Service for interacting with AI chat models to get completions with conversation memory.
/// Uses the <see cref="IChatClient"/> registered in the service collection, or an <see cref="OpenAICompatibleChatClient"/> configured from <see cref="AIChatServiceOptions"/> when none is registered.
/// Tools passed via <see cref="ChatOptions.Tools"/> are invoked automatically with a <see cref="FunctionInvokingChatClient"/>.
/// </summary>
public class AIChatService(IServiceProvider serviceProvider, IOptions<AIChatServiceOptions> options) : IAIChatService
{
    private readonly Dictionary<string, ConversationSession> sessions = new();
    private readonly object sessionsLock = new();
    private IConversationStore? store;

    internal static bool? IsBrowserOverride { get; set; }

    private static bool IsBrowser => IsBrowserOverride ?? OperatingSystem.IsBrowser();

    private const string BrowserKeyMessage = "An API key must not be used in the browser: everything in a WebAssembly application is visible to its users. Keep the key on the server and set AIChatServiceOptions.Proxy (or the Proxy parameter) to a server endpoint that adds it, as the ChatController of the demos does.";

    private static void EnsureKeyStaysOnTheServer(string? apiKey)
    {
        if (IsBrowser && !string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(BrowserKeyMessage);
        }
    }
    private readonly Dictionary<string, IChatClient> clients = new();
    private IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator;

    /// <summary>
    /// Gets the configuration options for the chat streaming service.
    /// </summary>
    public AIChatServiceOptions Options => options.Value;

    /// <inheritdoc />
    public IConversationStore Store => store ??= serviceProvider.GetService<IConversationStore>() ?? new InMemoryConversationStore();

    /// <inheritdoc />
    public async Task<ConversationSession> GetOrCreateSessionAsync(string? sessionId = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            var created = GetOrCreateSession(null);
            created.UserId = userId;
            return created;
        }

        lock (sessionsLock)
        {
            if (sessions.TryGetValue(sessionId, out var cached))
            {
                return cached;
            }
        }

        var loaded = await Store.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);

        lock (sessionsLock)
        {
            if (sessions.TryGetValue(sessionId, out var cached))
            {
                return cached;
            }

            loaded ??= new ConversationSession { Id = sessionId, MaxMessages = Options.MaxMessages, UserId = userId };
            sessions[sessionId] = loaded;
            return loaded;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConversationSession>> GetSessionsAsync(string? userId = null, CancellationToken cancellationToken = default) => Store.ListAsync(userId, cancellationToken);

    /// <inheritdoc />
    public async Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        var session = await GetOrCreateSessionAsync(sessionId, null, cancellationToken).ConfigureAwait(false);
        session.Clear();
        await Store.SaveAsync(session, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        lock (sessionsLock)
        {
            sessions.Remove(sessionId);
        }

        await Store.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetCompletionsAsync(string userInput, string? sessionId = null, [EnumeratorCancellation] CancellationToken cancellationToken = default, string? model = null, string? systemPrompt = null, double? temperature = null, int? maxTokens = null, string? endpoint = null, string? proxy = null, string? apiKey = null, string? apiKeyHeader = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            throw new ArgumentException("User input cannot be null or empty.", nameof(userInput));
        }

        var chatOptions = new ChatOptions
        {
            ModelId = model,
            Instructions = systemPrompt,
            Temperature = temperature.HasValue ? (float)temperature.Value : null,
            MaxOutputTokens = maxTokens
        };

        await foreach (var update in GetStreamingResponseAsync(new ChatMessage(ChatRole.User, userInput), sessionId, chatOptions, endpoint, proxy, apiKey, apiKeyHeader, null, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (update.Role != null && update.Role != ChatRole.Assistant)
            {
                continue;
            }

            foreach (var content in update.Contents.OfType<TextContent>())
            {
                if (!string.IsNullOrEmpty(content.Text))
                {
                    yield return content.Text;
                }
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(ChatMessage message, string? sessionId = null, ChatOptions? options = null, string? endpoint = null, string? proxy = null, string? apiKey = null, string? apiKeyHeader = null, string? userId = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var session = await GetOrCreateSessionAsync(sessionId, userId, cancellationToken).ConfigureAwait(false);

        options = options == null ? new ChatOptions() : options.Clone();
        options.ModelId ??= Options.Model;
        options.Instructions ??= Options.SystemPrompt;
        options.Temperature ??= (float)Options.Temperature;
        options.MaxOutputTokens ??= Options.MaxTokens;

        var client = GetChatClient(endpoint, proxy, apiKey, apiKeyHeader);

        var userText = string.Concat(message.Contents.OfType<TextContent>().Select(content => content.Text));

        var attachments = message.Contents.OfType<DataContent>().Select(Blazor.ChatAttachment.FromDataContent).ToList();

        if (userText.Length > 0 || attachments.Count > 0)
        {
            session.AddMessage(new Blazor.ChatMessage
            {
                UserId = "user",
                Role = "user",
                IsUser = true,
                Content = userText,
                Timestamp = DateTime.Now,
                Attachments = attachments
            });
        }

        session.AddHistory([message]);

        var updates = new List<ChatResponseUpdate>();

        try
        {
            await foreach (var update in client.GetStreamingResponseAsync(session.History, options, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                updates.Add(update);

                yield return update;
            }
        }
        finally
        {
            if (updates.Count > 0)
            {
                var response = updates.ToChatResponse();

                session.AddHistory(response.Messages);

                var displayMessage = ToDisplayMessage(response);

                if (displayMessage != null)
                {
                    MergeToolCalls(session, displayMessage);

                    if (displayMessage.Content.Length > 0 || displayMessage.ToolCalls.Count > 0)
                    {
                        session.AddMessage(displayMessage);
                    }
                }
            }

            await Store.SaveAsync(session, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public ConversationSession GetOrCreateSession(string? sessionId = null)
    {
        lock (sessionsLock)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
            }

            if (!sessions.TryGetValue(sessionId, out var session))
            {
                session = new ConversationSession
                {
                    Id = sessionId,
                    MaxMessages = Options.MaxMessages
                };
                sessions[sessionId] = session;
            }

            return session;
        }
    }

    /// <inheritdoc />
    public void ClearSession(string sessionId)
    {
        lock (sessionsLock)
        {
            if (sessions.TryGetValue(sessionId, out var session))
            {
                session.Clear();
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ConversationSession> GetActiveSessions()
    {
        lock (sessionsLock)
        {
            return sessions.Values.ToList();
        }
    }

    /// <inheritdoc />
    public void CleanupOldSessions(int maxAgeHours = 24)
    {
        lock (sessionsLock)
        {
            var cutoffTime = DateTime.Now.AddHours(-maxAgeHours);
            var sessionsToRemove = sessions.Values
                .Where(s => s.LastUpdated < cutoffTime)
                .Select(s => s.Id)
                .ToList();

            foreach (var sessionId in sessionsToRemove)
            {
                sessions.Remove(sessionId);
            }
        }
    }

    /// <summary>
    /// Builds the <see cref="Blazor.ChatMessage"/> shown for a model response: its text and the tool calls it made.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>The message, or <c>null</c> when the response has neither text nor tool calls.</returns>
    public static Blazor.ChatMessage? ToDisplayMessage(ChatResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var message = new Blazor.ChatMessage
        {
            UserId = "assistant",
            Role = "assistant",
            IsUser = false,
            Timestamp = DateTime.Now
        };

        var text = new StringBuilder();

        foreach (var responseMessage in response.Messages)
        {
            foreach (var content in responseMessage.Contents)
            {
                ApplyContent(message, content, text);
            }
        }

        message.Content = Blazor.ChatCitation.NormalizeMarkers(text.ToString());

        return message.Content.Length == 0 && message.ToolCalls.Count == 0 ? null : message;
    }

    /// <summary>
    /// Applies a piece of response content to a displayed message: appends text and tracks tool calls, their results and approval requests.
    /// </summary>
    /// <param name="message">The displayed message.</param>
    /// <param name="content">The content.</param>
    /// <param name="text">The builder that accumulates the message text.</param>
    public static void ApplyContent(Blazor.ChatMessage message, AIContent content, StringBuilder text)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(text);

        ApplyCitations(message, content);

        switch (content)
        {
            case TextContent textContent:
                text.Append(textContent.Text);
                break;
            case FunctionCallContent call:
                message.ToolCalls.Add(new Blazor.ChatToolCall
                {
                    CallId = call.CallId,
                    Name = call.Name,
                    Arguments = call.Arguments,
                    Exception = call.Exception,
                    Status = call.Exception != null ? Blazor.ChatToolCallStatus.Failed : Blazor.ChatToolCallStatus.Pending
                });
                break;
            case FunctionResultContent result:
                {
                    var call = message.ToolCalls.FirstOrDefault(toolCall => toolCall.CallId == result.CallId);

                    if (call == null)
                    {
                        call = new Blazor.ChatToolCall { CallId = result.CallId };
                        message.ToolCalls.Add(call);
                    }

                    call.Result = result.Result;
                    call.Exception = result.Exception;
                    call.Status = result.Exception != null ? Blazor.ChatToolCallStatus.Failed : call.Status == Blazor.ChatToolCallStatus.Rejected ? call.Status : Blazor.ChatToolCallStatus.Completed;
                }
                break;
            case ToolApprovalRequestContent approval when approval.ToolCall is FunctionCallContent call:
                {
                    var existing = message.ToolCalls.FirstOrDefault(toolCall => toolCall.CallId == call.CallId);

                    if (existing == null)
                    {
                        existing = new Blazor.ChatToolCall { CallId = call.CallId };
                        message.ToolCalls.Add(existing);
                    }

                    existing.Name = call.Name;
                    existing.Arguments = call.Arguments;
                    existing.ApprovalRequest = approval;
                    existing.Status = Blazor.ChatToolCallStatus.AwaitingApproval;
                }
                break;
            case TextReasoningContent reasoning:
                message.Reasoning += reasoning.Text;
                break;
            case UsageContent usage:
                message.Usage = usage.Details;
                break;
            case ErrorContent error:
                if (text.Length > 0)
                {
                    text.AppendLine();
                }

                text.Append(error.Message);
                break;
        }
    }

    /// <summary>
    /// Adds the citations a piece of response content carries to a displayed message, skipping sources the message already cites.
    /// </summary>
    /// <param name="message">The displayed message.</param>
    /// <param name="content">The content.</param>
    public static void ApplyCitations(Blazor.ChatMessage message, AIContent content)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(content);

        foreach (var citation in Blazor.ChatCitation.FromContent(content))
        {
            if (!message.Citations.Any(existing => existing.IsSameSource(citation)))
            {
                message.Citations.Add(citation);
            }
        }
    }

    private static void MergeToolCalls(ConversationSession session, Blazor.ChatMessage displayMessage)
    {
        for (var index = displayMessage.ToolCalls.Count - 1; index >= 0; index--)
        {
            var call = displayMessage.ToolCalls[index];
            var existing = session.Messages.SelectMany(message => message.ToolCalls).FirstOrDefault(toolCall => toolCall.CallId == call.CallId);

            if (existing == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(call.Name))
            {
                existing.Name = call.Name;
                existing.Arguments = call.Arguments;
            }

            existing.Result = call.Result;
            existing.Exception = call.Exception;
            existing.Status = call.Status;

            displayMessage.ToolCalls.RemoveAt(index);
        }
    }

    /// <inheritdoc />
    /// <remarks>The returned pipeline applies the configured <see cref="AIChatServiceOptions.Model"/>, <see cref="AIChatServiceOptions.MaxTokens"/> and <see cref="AIChatServiceOptions.Temperature"/> to requests that do not set them.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The pipelines are cached for the lifetime of the service and are never disposed: disposing one would dispose the IChatClient the application registered.")]
    public IChatClient GetChatClient(string? endpoint = null, string? proxy = null, string? apiKey = null, string? apiKeyHeader = null)
    {
        var hasOverrides = endpoint != null || proxy != null || apiKey != null || apiKeyHeader != null;
        var key = hasOverrides ? string.Join("|", endpoint, proxy, apiKey, apiKeyHeader) : string.Empty;

        lock (clients)
        {
            if (!clients.TryGetValue(key, out var client))
            {
                client = hasOverrides ? CreateOpenAIClient(endpoint, proxy, apiKey, apiKeyHeader) : serviceProvider.GetService<IChatClient>() ?? CreateOpenAIClient(null, null, null, null);
                client = BuildPipeline(client);

                clients[key] = client;
            }

            return client;
        }
    }

    /// <inheritdoc />
    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator()
    {
        lock (clients)
        {
            return embeddingGenerator ??= serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>() ?? CreateEmbeddingGenerator();
        }
    }

    private OpenAICompatibleEmbeddingGenerator CreateEmbeddingGenerator()
    {
        var url = Options.GetEmbeddingsProxy() ?? Options.GetEmbeddingsEndpoint();

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Set AIChatServiceOptions.EmbeddingsEndpoint or register an IEmbeddingGenerator to generate embeddings.");
        }

        EnsureKeyStaysOnTheServer(Options.ApiKey);

        if (!string.IsNullOrEmpty(Options.ApiKey) && string.IsNullOrWhiteSpace(Options.ApiKeyHeader))
        {
            throw new InvalidOperationException("API key header must be specified when an API key is provided.");
        }

        var httpClient = serviceProvider.GetService<HttpClient>() ?? throw new InvalidOperationException("Register an HttpClient or an IEmbeddingGenerator to use the AIChatService.");

        return new OpenAICompatibleEmbeddingGenerator(httpClient, url, Options.ApiKey, Options.ApiKeyHeader, Options.EmbeddingsModel);
    }

    private IChatClient BuildPipeline(IChatClient inner)
    {
        var builder = new ChatClientBuilder(inner);

        if (inner.GetService<FunctionInvokingChatClient>() == null)
        {
            builder.UseFunctionInvocation(serviceProvider.GetService<ILoggerFactory>());
        }

        builder.ConfigureOptions(ApplyDefaults);

        return builder.Build(serviceProvider);
    }

    private void ApplyDefaults(ChatOptions options)
    {
        options.ModelId ??= Options.Model;
        options.MaxOutputTokens ??= Options.MaxTokens;
        options.Temperature ??= (float)Options.Temperature;
    }

    private OpenAICompatibleChatClient CreateOpenAIClient(string? endpoint, string? proxy, string? apiKey, string? apiKeyHeader)
    {
        var url = proxy ?? Options.Proxy ?? endpoint ?? Options.Endpoint;
        var effectiveApiKey = apiKey ?? Options.ApiKey;
        var effectiveApiKeyHeader = apiKeyHeader ?? Options.ApiKeyHeader;

        EnsureKeyStaysOnTheServer(effectiveApiKey);

        if (!string.IsNullOrEmpty(effectiveApiKey) && string.IsNullOrWhiteSpace(effectiveApiKeyHeader))
        {
            throw new InvalidOperationException("API key header must be specified when an API key is provided.");
        }

        var httpClient = serviceProvider.GetService<HttpClient>() ?? throw new InvalidOperationException("Register an HttpClient or an IChatClient to use the AIChatService.");

        return new OpenAICompatibleChatClient(httpClient, url, effectiveApiKey, effectiveApiKeyHeader, Options.Model);
    }
}
