using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Radzen
{
    /// <summary>
    /// Interface for streaming chat completion responses.
    /// </summary>
    public interface IAIChatService
    {
        /// <summary>
        /// Streams chat completion responses from the AI model asynchronously.
        /// </summary>
        /// <param name="userInput">The user's input message to send to the AI model.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An async enumerable that yields streaming response chunks from the AI model.</returns>
        IAsyncEnumerable<string> GetCompletionsAsync(string userInput, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Configuration options for the AIChatStreamingService.
    /// </summary>
    public class AIChatServiceOptions
    {
        /// <summary>
        /// Gets or sets the endpoint URL for the AI service.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the proxy URL for the AI service, if any.
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
        /// Gets or sets the model name to use.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Gets or sets the system prompt for the AI assistant.
        /// </summary>
        public string SystemPrompt { get; set; } = "You are a helpful AI code assistant.";

        /// <summary>
        /// Gets or sets the temperature for the AI model (0.0 to 2.0).
        /// </summary>
        public double Temperature { get; set; } = 0.7;
    }

    /// <summary>
    /// Provides streaming chat completion functionality for AI models through a configurable endpoint.
    /// </summary>
    public class AIChatService : IAIChatService
    {
        private readonly HttpClient _httpClient;
        private readonly AIChatServiceOptions _options;

        /// <summary>
        /// Gets the configuration options for the chat streaming service.
        /// </summary>
        public AIChatServiceOptions Options => _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIChatService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used for making API requests.</param>
        /// <param name="options">The configuration options for the chat streaming service.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> or <paramref name="options"/> is null.</exception>
        public AIChatService(HttpClient httpClient, IOptions<AIChatServiceOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> GetCompletionsAsync(string userInput, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                throw new ArgumentException("User input cannot be null or empty.", nameof(userInput));

            var url = _options.Proxy ?? _options.Endpoint;

            var payload = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "system", content = _options.SystemPrompt },
                    new { role = "user", content = userInput }
                },
                temperature = _options.Temperature,
                stream = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                if (string.Equals(_options.ApiKeyHeader, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                }
                else
                {
                    request.Headers.Add(_options.ApiKeyHeader, _options.ApiKey);
                }
            }

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Chat stream failed: {await response.Content.ReadAsStringAsync()}");

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                var json = line["data:".Length..].Trim();

                if (json == "[DONE]") yield break;

                var content = ParseStreamingResponse(json);
                if (!string.IsNullOrEmpty(content))
                    yield return content;
            }
        }

        /// <summary>
        /// Parses a streaming JSON line from the AI service to extract content.
        /// </summary>
        /// <param name="json">The JSON line received from the streaming response.</param>
        /// <returns>The content text if available; otherwise, an empty string.</returns>
        private string ParseStreamingResponse(string json)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    return string.Empty;

                var firstChoice = choices[0];
                if (!firstChoice.TryGetProperty("delta", out var delta))
                    return string.Empty;

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
    public static class ChatStreamingServiceExtensions
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
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

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
}
