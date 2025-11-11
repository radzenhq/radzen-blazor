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
/// Service for interacting with AI chat models to get completions with conversation memory.
/// </summary>
public class AIChatService(IServiceProvider serviceProvider, IOptions<AIChatServiceOptions> options) : IAIChatService
{
    private readonly Dictionary<string, ConversationSession> sessions = new();
    private readonly object sessionsLock = new();

    /// <summary>
    /// Gets the configuration options for the chat streaming service.
    /// </summary>
    public AIChatServiceOptions Options => options.Value;

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetCompletionsAsync(string userInput, string sessionId = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default, string model = null, string systemPrompt = null, double? temperature = null, int? maxTokens = null, string endpoint = null, string proxy = null, string apiKey = null, string apiKeyHeader = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            throw new ArgumentException("User input cannot be null or empty.", nameof(userInput));
        }

        // Get or create session
        var session = GetOrCreateSession(sessionId);

        // Add user message to conversation history
        session.AddMessage("user", userInput);

        // Use runtime parameters or fall back to configured options
        var url = proxy ?? Options.Proxy ?? endpoint ?? Options.Endpoint;
        var effectiveApiKey = apiKey ?? Options.ApiKey;
        var effectiveApiKeyHeader = apiKeyHeader ?? Options.ApiKeyHeader;

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

        if (!string.IsNullOrEmpty(effectiveApiKey))
        {
            if (string.Equals(effectiveApiKeyHeader, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", effectiveApiKey);
            }
            else
            {
                request.Headers.Add(effectiveApiKeyHeader, effectiveApiKey);
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

        string line;
        while ((line = await reader.ReadLineAsync()) is not null && !cancellationToken.IsCancellationRequested)
        {
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
