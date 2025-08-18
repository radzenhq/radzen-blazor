using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Radzen;

/// <summary>
/// Represents a chat message in the conversation history.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the role of the message sender (system, user, or assistant).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Represents a conversation session with memory.
/// </summary>
public class ConversationSession
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation session.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the list of messages in the conversation.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the timestamp when the conversation was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the maximum number of messages to keep in memory.
    /// </summary>
    public int MaxMessages { get; set; } = 50;

    /// <summary>
    /// Adds a message to the conversation and manages memory limits.
    /// </summary>
    /// <param name="role">The role of the message sender.</param>
    /// <param name="content">The message content.</param>
    public void AddMessage(string role, string content)
    {
        Messages.Add(new ChatMessage
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.Now
        });

        LastUpdated = DateTime.Now;

        // Remove oldest messages if we exceed the limit
        while (Messages.Count > MaxMessages)
        {
            Messages.RemoveAt(0);
        }
    }

    /// <summary>
    /// Clears all messages from the conversation.
    /// </summary>
    public void Clear()
    {
        Messages.Clear();
        LastUpdated = DateTime.Now;
    }

    /// <summary>
    /// Gets the conversation messages formatted for the AI API.
    /// </summary>
    /// <param name="systemPrompt">The system prompt to include.</param>
    /// <returns>A list of message objects for the AI API.</returns>
    public List<object> GetFormattedMessages(string systemPrompt)
    {
        var messages = new List<object>();

        // Add system message
        messages.Add(new { role = "system", content = systemPrompt });

        // Add conversation messages
        foreach (var message in Messages)
        {
            messages.Add(new { role = message.Role, content = message.Content });
        }

        return messages;
    }
}

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
    /// <returns>An async enumerable that yields streaming response chunks from the AI model.</returns>
    IAsyncEnumerable<string> GetCompletionsAsync(string userInput, string sessionId = null, CancellationToken cancellationToken = default, string model = null, string systemPrompt = null, double? temperature = null, int? maxTokens = null);

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

/// <summary>
/// Configuration options for the <see cref="AIChatService"/>.
/// </summary>
public class AIChatServiceOptions
{
    /// <summary>
    /// Gets or sets the endpoint URL for the AI service.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proxy URL for the AI service, if any. If set, this will override the Endpoint.
    /// </summary>
    public string Proxy { get; set; } = null;

    /// <summary>
    /// Gets or sets the API key for authentication with the AI service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the header name for the API key (e.g., 'Authorization' or 'api-key').
    /// </summary>
    public string ApiKeyHeader { get; set; } = "Authorization";

    /// <summary>
    /// Gets or sets the model name to use for executing chat completions (e.g., 'gpt-3.5-turbo').
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the system prompt for the AI assistant.
    /// </summary>
    public string SystemPrompt { get; set; } = "You are a helpful AI code assistant.";

    /// <summary>
    /// Gets or sets the temperature for the AI model (0.0 to 2.0). Set to 0.0 for deterministic responses, higher values for more creative outputs.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate in the response.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages to keep in conversation memory.
    /// </summary>
    public int MaxMessages { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum age in hours for conversation sessions before cleanup.
    /// </summary>
    public int SessionMaxAgeHours { get; set; } = 24;
}

/// <summary>
/// Service for interacting with AI chat models to get completions with conversation memory.
/// </summary>
public class AIChatService(IServiceProvider serviceProvider, IOptions<AIChatServiceOptions> options) : IAIChatService
{
    private readonly Dictionary<string, ConversationSession> _sessions = new();
    private readonly object _sessionsLock = new();

    /// <summary>
    /// Gets the configuration options for the chat streaming service.
    /// </summary>
    public AIChatServiceOptions Options => options.Value;

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetCompletionsAsync(string userInput, string sessionId = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default, string model = null, string systemPrompt = null, double? temperature = null, int? maxTokens = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            throw new ArgumentException("User input cannot be null or empty.", nameof(userInput));
        }

        // Get or create session
        var session = GetOrCreateSession(sessionId);
        
        // Add user message to conversation history
        session.AddMessage("user", userInput);

        var url = Options.Proxy ?? Options.Endpoint;

        // Get formatted messages including conversation history
        var messages = session.GetFormattedMessages(systemPrompt ?? Options.SystemPrompt);

        var payload = new
        {
            model = model ?? Options.Model,
            messages = messages,
            temperature = temperature ?? Options.Temperature,
            max_tokens = maxTokens ?? Options.MaxTokens,
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(Options.ApiKey))
        {
            if (string.Equals(Options.ApiKeyHeader, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);
            }
            else
            {
                request.Headers.Add(Options.ApiKeyHeader, Options.ApiKey);
            }
        }

        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Chat stream failed: {await response.Content.ReadAsStringAsync(cancellationToken)}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var assistantResponse = new StringBuilder();

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
            {
                continue;
            }

            var json = line["data:".Length..].Trim();

            if (json == "[DONE]")
            {
                break;
            }

            var content = ParseStreamingResponse(json);
            if (!string.IsNullOrEmpty(content))
            {
                assistantResponse.Append(content);
                yield return content;
            }
        }

        // Add assistant response to conversation history
        if (assistantResponse.Length > 0)
        {
            session.AddMessage("assistant", assistantResponse.ToString());
        }
    }

    /// <inheritdoc />
    public ConversationSession GetOrCreateSession(string sessionId = null)
    {
        lock (_sessionsLock)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
            }

            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                session = new ConversationSession
                {
                    Id = sessionId,
                    MaxMessages = Options.MaxMessages
                };
                _sessions[sessionId] = session;
            }

            return session;
        }
    }

    /// <inheritdoc />
    public void ClearSession(string sessionId)
    {
        lock (_sessionsLock)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Clear();
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ConversationSession> GetActiveSessions()
    {
        lock (_sessionsLock)
        {
            return _sessions.Values.ToList();
        }
    }

    /// <inheritdoc />
    public void CleanupOldSessions(int maxAgeHours = 24)
    {
        lock (_sessionsLock)
        {
            var cutoffTime = DateTime.Now.AddHours(-maxAgeHours);
            var sessionsToRemove = _sessions.Values
                .Where(s => s.LastUpdated < cutoffTime)
                .Select(s => s.Id)
                .ToList();

            foreach (var sessionId in sessionsToRemove)
            {
                _sessions.Remove(sessionId);
            }
        }
    }

    private static string ParseStreamingResponse(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var firstChoice = choices[0];

            if (!firstChoice.TryGetProperty("delta", out var delta))
            {
                return string.Empty;
            }

            if (delta.TryGetProperty("content", out var contentElement))
            {
                return contentElement.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// Extension methods for configuring AIChatService in the dependency injection container.
/// </summary>
public static class AIChatServiceExtensions
{
    /// <summary>
    /// Adds the AIChatService to the service collection with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The action to configure the AIChatService options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAIChatService(this IServiceCollection services, Action<AIChatServiceOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        services.Configure(configureOptions);
        services.AddScoped<IAIChatService, AIChatService>();

        return services;
    }

    /// <summary>
    /// Adds the AIChatService to the service collection with default options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAIChatService(this IServiceCollection services)
    {
        services.AddOptions<AIChatServiceOptions>();
        services.AddScoped<IAIChatService, AIChatService>();

        return services;
    }
}