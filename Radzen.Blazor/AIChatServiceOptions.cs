namespace Radzen;

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
    public string? Proxy { get; set; }

    /// <summary>
    /// Gets or sets the API key sent with the <see cref="ApiKeyHeader"/> header. Server-side only: configure it in Blazor Server or in the server that hosts a WebAssembly application and forwards the requests of <see cref="Proxy"/>.
    /// A key configured in the browser is visible to every user, so the service throws when a WebAssembly application sets one.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the header name for the API key (e.g., 'Authorization' or 'api-key').
    /// </summary>
    public string ApiKeyHeader { get; set; } = "Authorization";

    /// <summary>
    /// Gets or sets the model name to use for executing chat completions (e.g., 'gpt-3.5-turbo').
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the embeddings endpoint URL. When not set it is derived from <see cref="Endpoint"/> by replacing <c>chat/completions</c> with <c>embeddings</c>.
    /// </summary>
    public string? EmbeddingsEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the proxy URL for embeddings requests, if any. When not set and <see cref="Proxy"/> is set it is derived from it by replacing <c>chat/completions</c> with <c>embeddings</c>.
    /// </summary>
    public string? EmbeddingsProxy { get; set; }

    /// <summary>
    /// Gets or sets the embeddings model name (e.g., 'text-embedding-3-small').
    /// </summary>
    public string? EmbeddingsModel { get; set; }

    /// <summary>
    /// Gets the effective embeddings endpoint: <see cref="EmbeddingsEndpoint"/> or the one derived from <see cref="Endpoint"/>.
    /// </summary>
    public string GetEmbeddingsEndpoint() => !string.IsNullOrEmpty(EmbeddingsEndpoint) ? EmbeddingsEndpoint : Endpoint.Replace("chat/completions", "embeddings", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the effective embeddings proxy: <see cref="EmbeddingsProxy"/> or the one derived from <see cref="Proxy"/>, or <c>null</c>.
    /// </summary>
    public string? GetEmbeddingsProxy() => !string.IsNullOrEmpty(EmbeddingsProxy) ? EmbeddingsProxy : Proxy?.Replace("chat/completions", "embeddings", System.StringComparison.OrdinalIgnoreCase);

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

