using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Radzen;

/// <summary>
/// An <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> for OpenAI-compatible embeddings endpoints (OpenAI, Azure OpenAI, Ollama, Cloudflare Workers AI and others).
/// </summary>
/// <example>
/// <code>
/// builder.Services.AddEmbeddingGenerator(services =>
///     new OpenAICompatibleEmbeddingGenerator(services.GetRequiredService&lt;HttpClient&gt;(), "https://api.openai.com/v1/embeddings", apiKey, defaultModel: "text-embedding-3-small"));
/// </code>
/// </example>
public sealed class OpenAICompatibleEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly HttpClient httpClient;
    private readonly string endpoint;
    private readonly string? apiKey;
    private readonly string apiKeyHeader;
    private readonly string? defaultModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAICompatibleEmbeddingGenerator"/> class.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> used to send requests. It is not disposed by the generator.</param>
    /// <param name="endpoint">The embeddings URL, for example <c>https://api.openai.com/v1/embeddings</c>. A relative URL is resolved against the <see cref="HttpClient.BaseAddress"/>.</param>
    /// <param name="apiKey">The API key. When <c>null</c> or empty no authentication header is sent.</param>
    /// <param name="apiKeyHeader">The header that carries the API key. <c>Authorization</c> (the default) sends it as a bearer token.</param>
    /// <param name="defaultModel">The model used when <see cref="EmbeddingGenerationOptions.ModelId"/> is not set.</param>
    public OpenAICompatibleEmbeddingGenerator(HttpClient httpClient, string endpoint, string? apiKey = null, string? apiKeyHeader = "Authorization", string? defaultModel = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("The endpoint cannot be null or empty.", nameof(endpoint));
        }

        this.httpClient = httpClient;
        this.endpoint = endpoint;
        this.apiKey = apiKey;
        this.apiKeyHeader = string.IsNullOrWhiteSpace(apiKeyHeader) ? "Authorization" : apiKeyHeader;
        this.defaultModel = defaultModel;

        Metadata = new EmbeddingGeneratorMetadata("openai-compatible", Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri : null, defaultModel);
    }

    /// <summary>
    /// Gets the metadata that describes the generator.
    /// </summary>
    public EmbeddingGeneratorMetadata Metadata { get; }

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        var inputs = values.ToList();

        if (inputs.Count == 0)
        {
            return new GeneratedEmbeddings<Embedding<float>>();
        }

        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            var model = options?.ModelId ?? defaultModel;

            if (!string.IsNullOrEmpty(model))
            {
                writer.WriteString("model", model);
            }

            writer.WritePropertyName("input");
            writer.WriteStartArray();

            foreach (var input in inputs)
            {
                writer.WriteStringValue(input);
            }

            writer.WriteEndArray();

            if (options?.Dimensions is int dimensions)
            {
                writer.WriteNumber("dimensions", dimensions);
            }

            writer.WriteEndObject();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new ByteArrayContent(buffer.WrittenSpan.ToArray())
        };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        if (!string.IsNullOrEmpty(apiKey))
        {
            if (string.Equals(apiKeyHeader, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(apiKeyHeader, apiKey);
            }
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Embeddings request failed with status {(int)response.StatusCode}: {json}");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("error", out var error))
        {
            throw new InvalidOperationException(error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message) ? message.GetString() : error.ToString());
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("The embeddings response does not contain data.");
        }

        var modelId = root.TryGetProperty("model", out var modelElement) && modelElement.ValueKind == JsonValueKind.String ? modelElement.GetString() : null;
        var embeddings = new Embedding<float>[inputs.Count];
        var position = 0;

        foreach (var item in data.EnumerateArray())
        {
            var index = item.TryGetProperty("index", out var indexElement) && indexElement.ValueKind == JsonValueKind.Number && indexElement.TryGetInt32(out var parsed) ? parsed : position;

            if (index < 0 || index >= embeddings.Length || !item.TryGetProperty("embedding", out var vector) || vector.ValueKind != JsonValueKind.Array)
            {
                position++;
                continue;
            }

            var floats = new float[vector.GetArrayLength()];
            var i = 0;

            foreach (var value in vector.EnumerateArray())
            {
                floats[i++] = value.GetSingle();
            }

            embeddings[index] = new Embedding<float>(floats) { ModelId = modelId, CreatedAt = DateTimeOffset.UtcNow };
            position++;
        }

        if (embeddings.Any(embedding => embedding == null))
        {
            throw new InvalidOperationException("The embeddings response does not contain an embedding for every input.");
        }

        var result = new GeneratedEmbeddings<Embedding<float>>(embeddings);

        if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
        {
            result.Usage = new UsageDetails
            {
                InputTokenCount = usage.TryGetProperty("prompt_tokens", out var prompt) && prompt.TryGetInt64(out var promptTokens) ? promptTokens : null,
                TotalTokenCount = usage.TryGetProperty("total_tokens", out var total) && total.TryGetInt64(out var totalTokens) ? totalTokens : null
            };
        }

        return result;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey != null)
        {
            return null;
        }

        if (serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return Metadata;
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
