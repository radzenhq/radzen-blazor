using System.Collections.Generic;
using System.Threading;

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
    IAsyncEnumerable<string> GetCompletionsAsync(string userInput, string sessionId = null, CancellationToken cancellationToken = default, string model = null, string systemPrompt = null, double? temperature = null, int? maxTokens = null, string endpoint = null, string proxy = null, string apiKey = null, string apiKeyHeader = null);

    /// <summary>
    /// Gets or creates a conversation session.
    /// </summary>
    /// <param name="sessionId">The session ID. If null, a new session will be created.</param>
    /// <returns>The conversation session.</returns>
    ConversationSession GetOrCreateSession(string sessionId = null);

    /// <summary>
    /// Clears the conversation history for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to clear.</param>
    void ClearSession(string sessionId);

    /// <summary>
    /// Gets all active conversation sessions.
    /// </summary>
    /// <returns>A list of active conversation sessions.</returns>
    IEnumerable<ConversationSession> GetActiveSessions();

    /// <summary>
    /// Removes old conversation sessions based on age.
    /// </summary>
    /// <param name="maxAgeHours">Maximum age in hours for sessions to keep.</param>
    void CleanupOldSessions(int maxAgeHours = 24);
}

