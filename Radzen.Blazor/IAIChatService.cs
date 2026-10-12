using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Radzen;

/// <summary>
/// Interface for getting chat completions from an AI model with conversation memory.
/// </summary>
public interface IAIChatService
{
    /// <summary>
    /// Streams chat completion responses from the AI model asynchronously with conversation memory.
    /// </summary>
    /// <param name="userInput">The user's input message to send to the AI model.</param>
    /// <param name="sessionId">Optional session ID to maintain conversation context. If null, a new session will be created.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <param name="model">Optional model name to override the configured model.</param>
    /// <param name="systemPrompt">Optional system prompt to override the configured system prompt.</param>
    /// <param name="temperature">Optional temperature to override the configured temperature.</param>
    /// <param name="maxTokens">Optional maximum tokens to override the configured max tokens.</param>
    /// <param name="endpoint">Optional endpoint URL to override the configured endpoint.</param>
    /// <param name="proxy">Optional proxy URL to override the configured proxy.</param>
    /// <param name="apiKey">Optional API key to override the configured API key.</param>
    /// <param name="apiKeyHeader">Optional API key header name to override the configured header.</param>
    /// <returns>An async enumerable that yields streaming response chunks from the AI model.</returns>
    IAsyncEnumerable<string> GetCompletionsAsync(string userInput, string? sessionId = null, CancellationToken cancellationToken = default, string? model = null, string? systemPrompt = null, double? temperature = null, int? maxTokens = null, string? endpoint = null, string? proxy = null, string? apiKey = null, string? apiKeyHeader = null);

    /// <summary>
    /// Streams the response of the AI model to a message with conversation memory. Tools in <see cref="ChatOptions.Tools"/> are invoked automatically;
    /// their calls and results are yielded as <see cref="FunctionCallContent"/> and <see cref="FunctionResultContent"/> and a tool wrapped in
    /// <see cref="ApprovalRequiredAIFunction"/> yields a <see cref="ToolApprovalRequestContent"/> that must be answered with a message carrying a <see cref="ToolApprovalResponseContent"/>.
    /// </summary>
    /// <param name="message">The message to send. Usually a user message with text, or a user message carrying a <see cref="ToolApprovalResponseContent"/>.</param>
    /// <param name="sessionId">Optional session ID to maintain conversation context. If null, a new session will be created.</param>
    /// <param name="options">Optional chat options. <see cref="ChatOptions.ModelId"/>, <see cref="ChatOptions.Instructions"/>, <see cref="ChatOptions.Temperature"/> and <see cref="ChatOptions.MaxOutputTokens"/> fall back to the configured <see cref="AIChatServiceOptions"/> when not set.</param>
    /// <param name="endpoint">Optional endpoint URL to override the configured endpoint. Ignored when an <see cref="IChatClient"/> is registered and no override is specified.</param>
    /// <param name="proxy">Optional proxy URL to override the configured proxy.</param>
    /// <param name="apiKey">Optional API key to override the configured API key.</param>
    /// <param name="apiKeyHeader">Optional API key header name to override the configured header.</param>
    /// <param name="userId">The user the session belongs to, used when the session is created.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An async enumerable that yields the streaming response updates of the AI model.</returns>
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(ChatMessage message, string? sessionId = null, ChatOptions? options = null, string? endpoint = null, string? proxy = null, string? apiKey = null, string? apiKeyHeader = null, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the store the conversations are kept in.
    /// </summary>
    IConversationStore Store { get; }

    /// <summary>
    /// Gets a conversation session from the store, or creates it. The session is cached for the lifetime of the service and saved to the store after every response.
    /// </summary>
    /// <param name="sessionId">The session ID. If null, a new session will be created.</param>
    /// <param name="userId">The user the session belongs to, used when it is created.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<ConversationSession> GetOrCreateSessionAsync(string? sessionId = null, string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the conversations in the store, most recently updated first.
    /// </summary>
    /// <param name="userId">When specified only the conversations of that user are returned.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IReadOnlyList<ConversationSession>> GetSessionsAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the messages of a conversation and saves it.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation from the store.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <see cref="IChatClient"/> the service sends requests with: the one registered in the service collection or an <see cref="OpenAICompatibleChatClient"/>
    /// configured from <see cref="AIChatServiceOptions"/>, wrapped in a <see cref="FunctionInvokingChatClient"/>. Use it for one-off completions outside of a conversation.
    /// </summary>
    /// <param name="endpoint">Optional endpoint URL to override the configured endpoint. Any override selects the built-in OpenAI-compatible client.</param>
    /// <param name="proxy">Optional proxy URL to override the configured proxy.</param>
    /// <param name="apiKey">Optional API key to override the configured API key.</param>
    /// <param name="apiKeyHeader">Optional API key header name to override the configured header.</param>
    IChatClient GetChatClient(string? endpoint = null, string? proxy = null, string? apiKey = null, string? apiKeyHeader = null);

    /// <summary>
    /// Gets the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> used for semantic search and similar scenarios: the one registered in the service collection or an
    /// <see cref="OpenAICompatibleEmbeddingGenerator"/> configured from <see cref="AIChatServiceOptions"/>.
    /// </summary>
    IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator();

    /// <summary>
    /// Gets or creates a conversation session in the cache of this service instance without consulting the store. Prefer <see cref="GetOrCreateSessionAsync"/>.
    /// </summary>
    /// <param name="sessionId">The session ID. If null, a new session will be created.</param>
    /// <returns>The conversation session.</returns>
    ConversationSession GetOrCreateSession(string? sessionId = null);

    /// <summary>
    /// Clears the conversation history for a specific session in the cache of this service instance. Prefer <see cref="ClearSessionAsync"/>.
    /// </summary>
    /// <param name="sessionId">The session ID to clear.</param>
    void ClearSession(string sessionId);

    /// <summary>
    /// Gets the conversation sessions cached by this service instance. Prefer <see cref="GetSessionsAsync"/>, which lists the store.
    /// </summary>
    /// <returns>A list of active conversation sessions.</returns>
    IEnumerable<ConversationSession> GetActiveSessions();

    /// <summary>
    /// Removes old conversation sessions from the cache of this service instance based on age.
    /// </summary>
    /// <param name="maxAgeHours">Maximum age in hours for sessions to keep.</param>
    void CleanupOldSessions(int maxAgeHours = 24);
}
