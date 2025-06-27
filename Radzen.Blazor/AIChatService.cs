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

namespace Radzen;

/// <summary>
/// Interface for getting chat completions from an AI model.
/// </summary>
public interface IAIChatService
{
    /// <summary>
    /// Streams chat completion responses from the AI model asynchronously.
    /// </summary>
    /// <param name="userInput">The user's input message to send to the AI model.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <param name="model">Optional model name to override the configured model.</param>
    /// <param name="systemPrompt">Optional system prompt to override the configured system prompt.</param>
    /// <param name="temperature">Optional temperature to override the configured temperature.</param>
    /// <param name="maxTokens">Optional maximum tokens to override the configured max tokens.</param>
    /// <returns>An async enumerable that yields streaming response chunks from the AI model.</returns>
    IAsyncEnumerable<string> GetCompletionsAsync(string userInput, CancellationToken cancellationToken = default, string? model = null, string? systemPrompt = null, double? temperature = null, int? maxTokens = null);
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
}

/// <summary>
/// Service for interacting with AI chat models to get completions.
/// </summary>
public class AIChatService(HttpClient httpClient, IOptions<AIChatServiceOptions> options) : IAIChatService
{
    /// <summary>
    /// Gets the configuration options for the chat streaming service.
    /// </summary>
    public AIChatServiceOptions Options => options.Value;

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetCompletionsAsync(string userInput, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default, string? model = null, string? systemPrompt = null, double? temperature = null, int? maxTokens = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            throw new ArgumentException("User input cannot be null or empty.", nameof(userInput));
        }

        var url = Options.Proxy ?? Options.Endpoint;

        var payload = new
        {
            model = model ?? Options.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? Options.SystemPrompt },
                new { role = "user", content = userInput }
            },
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

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Chat stream failed: {await response.Content.ReadAsStringAsync(cancellationToken)}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

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
                yield break;
            }

            var content = ParseStreamingResponse(json);
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
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