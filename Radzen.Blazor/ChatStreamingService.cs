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
    /// Configuration options for the ChatStreamingService.
    /// </summary>
    public class ChatStreamingServiceOptions
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
        public string Model { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// Gets or sets the system prompt for the AI assistant.
        /// </summary>
        public string SystemPrompt { get; set; } = "You are a helpful AI code assistant.";

        /// <summary>
        /// Gets or sets the temperature for the AI model (0.0 to 2.0).
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the maximum number of tokens per response chunk.
        /// </summary>
        public int MaxTokens { get; set; } = 50;
    }

    /// <summary>
    /// Provides streaming chat completion functionality for AI models through a configurable endpoint (OpenAI, Azure, Cloudflare, etc).
    /// </summary>
    public class ChatStreamingService
    {
        private readonly HttpClient _httpClient;
        private readonly ChatStreamingServiceOptions _options;

        /// <summary>
        /// Gets the configuration options for the chat streaming service.
        /// </summary>
        public ChatStreamingServiceOptions Options => _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatStreamingService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used for making API requests.</param>
        /// <param name="options">The configuration options for the chat streaming service.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> or <paramref name="options"/> is null.</exception>
        public ChatStreamingService(HttpClient httpClient, IOptions<ChatStreamingServiceOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Streams chat completion responses from the AI model asynchronously.
        /// </summary>
        /// <param name="userInput">The user's input message to send to the AI model.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An async enumerable that yields streaming response chunks from the AI model.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="userInput"/> is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request to the AI service fails.</exception>
        /// <exception cref="JsonException">Thrown when the response from the AI service contains invalid JSON.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
        /// <remarks>
        /// This method streams responses from either OpenAI API or Azure OpenAI Service depending on the configuration.
        /// The AI model is configured with a system prompt as a helpful code assistant and uses a temperature of 0.7
        /// with a maximum of 50 tokens per response chunk. The method yields content as it becomes available,
        /// providing real-time streaming capabilities for interactive chat applications.
        /// </remarks>
        public async IAsyncEnumerable<string> StreamChatCompletionAsync(string userInput, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
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
                max_tokens = _options.MaxTokens,
                stream = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }), Encoding.UTF8, "application/json")
            };

            // Use configurable API key header
            if (string.Equals(_options.ApiKeyHeader, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }
            else
            {
                request.Headers.Add(_options.ApiKeyHeader, _options.ApiKey);
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

        private string ParseStreamingResponse(string json)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check if the response has the expected structure
                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    return string.Empty;

                var firstChoice = choices[0];
                if (!firstChoice.TryGetProperty("delta", out var delta))
                    return string.Empty;

                // Check if delta has a content property
                if (delta.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? string.Empty;
                }

                return string.Empty;
            }
            catch (JsonException ex)
            {
                // Log the JSON parsing error but continue processing
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message} for line: {json}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                // Log any other errors but continue processing
                System.Diagnostics.Debug.WriteLine($"Error processing stream line: {ex.Message}");
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Extension methods for configuring ChatStreamingService in the dependency injection container.
    /// </summary>
    public static class ChatStreamingServiceExtensions
    {
        /// <summary>
        /// Adds the ChatStreamingService to the service collection with the specified configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The action to configure the ChatStreamingService options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.</exception>
        public static IServiceCollection AddChatStreamingService(this IServiceCollection services, Action<ChatStreamingServiceOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddScoped<ChatStreamingService>();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="ChatStreamingServiceExtensions" /> to the service collection.
        /// </summary>
        public static IServiceCollection AddChatStreamingService(this IServiceCollection services)
        {
            services.AddOptions<ChatStreamingServiceOptions>();
            services.AddScoped<ChatStreamingService>();

            return services;
        }
    }
}
