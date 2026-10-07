using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Radzen;

/// <summary>
/// An <see cref="IChatClient"/> for OpenAI-compatible chat completion endpoints (OpenAI, Azure OpenAI, Ollama, Cloudflare Workers AI, LM Studio and others).
/// Supports streaming responses and tool calling. Pair it with <see cref="FunctionInvokingChatClient"/> to invoke <see cref="AIFunction"/> tools automatically.
/// </summary>
/// <example>
/// <code>
/// builder.Services.AddChatClient(services =>
///     new OpenAICompatibleChatClient(services.GetRequiredService&lt;HttpClient&gt;(), "https://api.openai.com/v1/chat/completions", apiKey, defaultModel: "gpt-4o-mini"))
///     .UseFunctionInvocation();
/// </code>
/// </example>
public sealed class OpenAICompatibleChatClient : IChatClient
{
    private readonly HttpClient httpClient;
    private readonly string endpoint;
    private readonly string? apiKey;
    private readonly string apiKeyHeader;
    private readonly string? defaultModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAICompatibleChatClient"/> class.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> used to send requests. It is not disposed by the client.</param>
    /// <param name="endpoint">The chat completions URL, for example <c>https://api.openai.com/v1/chat/completions</c>. A relative URL is resolved against the <see cref="HttpClient.BaseAddress"/>.</param>
    /// <param name="apiKey">The API key. When <c>null</c> or empty no authentication header is sent.</param>
    /// <param name="apiKeyHeader">The header that carries the API key. <c>Authorization</c> (the default) sends it as a bearer token.</param>
    /// <param name="defaultModel">The model used when <see cref="ChatOptions.ModelId"/> is not set.</param>
    public OpenAICompatibleChatClient(HttpClient httpClient, string endpoint, string? apiKey = null, string? apiKeyHeader = "Authorization", string? defaultModel = null)
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

        Metadata = new ChatClientMetadata("openai-compatible", Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri : null, defaultModel);
    }

    /// <summary>
    /// Gets the metadata that describes the client.
    /// </summary>
    public ChatClientMetadata Metadata { get; }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new ByteArrayContent(BuildRequestBody(messages, options))
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

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            throw new HttpRequestException($"Chat request failed with status {(int)response.StatusCode}: {error}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var toolCalls = new SortedDictionary<int, PendingToolCall>();
        var flushed = false;
        string? responseId = null;
        string? modelId = null;
        DateTimeOffset? createdAt = null;

        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var json = line.AsSpan("data:".Length).Trim();

            if (json.Length == 0)
            {
                continue;
            }

            if (json.SequenceEqual("[DONE]"))
            {
                break;
            }

            JsonDocument document;

            try
            {
                document = JsonDocument.Parse(json.ToString());
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (root.TryGetProperty("error", out var errorElement))
                {
                    var message = errorElement.ValueKind == JsonValueKind.Object && errorElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : errorElement.ToString();

                    throw new InvalidOperationException(message ?? "The chat request failed.");
                }

                responseId ??= root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String ? idElement.GetString() : null;
                modelId ??= root.TryGetProperty("model", out var modelElement) && modelElement.ValueKind == JsonValueKind.String ? modelElement.GetString() : null;

                if (createdAt == null && root.TryGetProperty("created", out var createdElement) && createdElement.ValueKind == JsonValueKind.Number && createdElement.TryGetInt64(out var created))
                {
                    createdAt = DateTimeOffset.FromUnixTimeSeconds(created);
                }

                if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.Object)
                    {
                        if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                        {
                            var text = content.GetString();

                            if (!string.IsNullOrEmpty(text))
                            {
                                yield return CreateUpdate(responseId, modelId, createdAt, new TextContent(text));
                            }
                        }

                        if (delta.TryGetProperty("tool_calls", out var deltaToolCalls) && deltaToolCalls.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var toolCall in deltaToolCalls.EnumerateArray())
                            {
                                AccumulateToolCall(toolCalls, toolCall);
                            }
                        }
                    }

                    if (choice.TryGetProperty("finish_reason", out var finishReason) && finishReason.ValueKind == JsonValueKind.String && !flushed && toolCalls.Count > 0)
                    {
                        flushed = true;

                        foreach (var functionCall in FlushToolCalls(toolCalls))
                        {
                            yield return CreateUpdate(responseId, modelId, createdAt, functionCall, finishReason: ChatFinishReason.ToolCalls);
                        }
                    }
                }
            }
        }

        if (!flushed && toolCalls.Count > 0)
        {
            foreach (var functionCall in FlushToolCalls(toolCalls))
            {
                yield return CreateUpdate(responseId, modelId, createdAt, functionCall, finishReason: ChatFinishReason.ToolCalls);
            }
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey != null)
        {
            return null;
        }

        if (serviceType == typeof(ChatClientMetadata))
        {
            return Metadata;
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private static ChatResponseUpdate CreateUpdate(string? responseId, string? modelId, DateTimeOffset? createdAt, AIContent content, ChatFinishReason? finishReason = null)
    {
        return new ChatResponseUpdate(ChatRole.Assistant, [content])
        {
            ResponseId = responseId,
            MessageId = responseId,
            ModelId = modelId,
            CreatedAt = createdAt,
            FinishReason = finishReason
        };
    }

    private static void AccumulateToolCall(SortedDictionary<int, PendingToolCall> toolCalls, JsonElement toolCall)
    {
        if (toolCall.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var index = toolCall.TryGetProperty("index", out var indexElement) && indexElement.ValueKind == JsonValueKind.Number && indexElement.TryGetInt32(out var parsedIndex) ? parsedIndex : toolCalls.Count;

        if (!toolCalls.TryGetValue(index, out var pending))
        {
            pending = new PendingToolCall();
            toolCalls[index] = pending;
        }

        if (toolCall.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
        {
            pending.Id = idElement.GetString();
        }

        if (toolCall.TryGetProperty("function", out var function) && function.ValueKind == JsonValueKind.Object)
        {
            if (function.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                pending.Name += nameElement.GetString();
            }

            if (function.TryGetProperty("arguments", out var argumentsElement) && argumentsElement.ValueKind == JsonValueKind.String)
            {
                pending.Arguments.Append(argumentsElement.GetString());
            }
        }
    }

    private static IEnumerable<FunctionCallContent> FlushToolCalls(SortedDictionary<int, PendingToolCall> toolCalls)
    {
        foreach (var pending in toolCalls.Values)
        {
            yield return pending.ToFunctionCall();
        }

        toolCalls.Clear();
    }

    private byte[] BuildRequestBody(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            var model = options?.ModelId ?? defaultModel;

            if (!string.IsNullOrEmpty(model))
            {
                writer.WriteString("model", model);
            }

            writer.WriteBoolean("stream", true);

            writer.WritePropertyName("messages");
            writer.WriteStartArray();

            if (!string.IsNullOrWhiteSpace(options?.Instructions))
            {
                WriteTextMessage(writer, "system", options.Instructions);
            }

            foreach (var message in messages)
            {
                WriteMessage(writer, message);
            }

            writer.WriteEndArray();

            if (options != null)
            {
                if (options.Temperature.HasValue)
                {
                    writer.WriteNumber("temperature", options.Temperature.Value);
                }

                if (options.MaxOutputTokens.HasValue)
                {
                    writer.WriteNumber("max_tokens", options.MaxOutputTokens.Value);
                }

                if (options.TopP.HasValue)
                {
                    writer.WriteNumber("top_p", options.TopP.Value);
                }

                if (options.FrequencyPenalty.HasValue)
                {
                    writer.WriteNumber("frequency_penalty", options.FrequencyPenalty.Value);
                }

                if (options.PresencePenalty.HasValue)
                {
                    writer.WriteNumber("presence_penalty", options.PresencePenalty.Value);
                }

                if (options.Seed.HasValue)
                {
                    writer.WriteNumber("seed", options.Seed.Value);
                }

                if (options.StopSequences is { Count: > 0 })
                {
                    writer.WritePropertyName("stop");
                    writer.WriteStartArray();

                    foreach (var stop in options.StopSequences)
                    {
                        writer.WriteStringValue(stop);
                    }

                    writer.WriteEndArray();
                }

                if (options.ResponseFormat is ChatResponseFormatJson)
                {
                    writer.WritePropertyName("response_format");
                    writer.WriteStartObject();
                    writer.WriteString("type", "json_object");
                    writer.WriteEndObject();
                }

                var functions = options.Tools?.OfType<AIFunctionDeclaration>().ToList();

                if (functions is { Count: > 0 })
                {
                    writer.WritePropertyName("tools");
                    writer.WriteStartArray();

                    foreach (var function in functions)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("type", "function");
                        writer.WritePropertyName("function");
                        writer.WriteStartObject();
                        writer.WriteString("name", function.Name);

                        if (!string.IsNullOrEmpty(function.Description))
                        {
                            writer.WriteString("description", function.Description);
                        }

                        writer.WritePropertyName("parameters");
                        function.JsonSchema.WriteTo(writer);
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();

                    switch (options.ToolMode)
                    {
                        case NoneChatToolMode:
                            writer.WriteString("tool_choice", "none");
                            break;
                        case RequiredChatToolMode required when !string.IsNullOrEmpty(required.RequiredFunctionName):
                            writer.WritePropertyName("tool_choice");
                            writer.WriteStartObject();
                            writer.WriteString("type", "function");
                            writer.WritePropertyName("function");
                            writer.WriteStartObject();
                            writer.WriteString("name", required.RequiredFunctionName);
                            writer.WriteEndObject();
                            writer.WriteEndObject();
                            break;
                        case RequiredChatToolMode:
                            writer.WriteString("tool_choice", "required");
                            break;
                    }

                    if (options.AllowMultipleToolCalls == false)
                    {
                        writer.WriteBoolean("parallel_tool_calls", false);
                    }
                }
            }

            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteMessage(Utf8JsonWriter writer, ChatMessage message)
    {
        if (message.Role == ChatRole.Tool)
        {
            foreach (var result in message.Contents.OfType<FunctionResultContent>())
            {
                writer.WriteStartObject();
                writer.WriteString("role", "tool");
                writer.WriteString("tool_call_id", result.CallId);
                writer.WriteString("content", FormatResult(result));
                writer.WriteEndObject();
            }

            return;
        }

        var text = string.Concat(message.Contents.OfType<TextContent>().Select(content => content.Text));

        if (message.Role == ChatRole.Assistant)
        {
            var functionCalls = message.Contents.OfType<FunctionCallContent>().ToList();

            if (text.Length == 0 && functionCalls.Count == 0)
            {
                return;
            }

            writer.WriteStartObject();
            writer.WriteString("role", "assistant");

            writer.WriteString("content", text);

            if (functionCalls.Count > 0)
            {
                writer.WritePropertyName("tool_calls");
                writer.WriteStartArray();

                foreach (var functionCall in functionCalls)
                {
                    writer.WriteStartObject();
                    writer.WriteString("id", functionCall.CallId);
                    writer.WriteString("type", "function");
                    writer.WritePropertyName("function");
                    writer.WriteStartObject();
                    writer.WriteString("name", functionCall.Name);
                    writer.WriteString("arguments", FormatArguments(functionCall.Arguments));
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();

            return;
        }

        if (text.Length == 0)
        {
            return;
        }

        WriteTextMessage(writer, message.Role == ChatRole.System ? "system" : "user", text);
    }

    private static void WriteTextMessage(Utf8JsonWriter writer, string role, string text)
    {
        writer.WriteStartObject();
        writer.WriteString("role", role);
        writer.WriteString("content", text);
        writer.WriteEndObject();
    }

    private static string FormatArguments(IDictionary<string, object?>? arguments)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    writer.WritePropertyName(argument.Key);
                    WriteValue(writer, argument.Value);
                }
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string FormatResult(FunctionResultContent result)
    {
        if (result.Exception != null)
        {
            return $"Error: {result.Exception.Message}";
        }

        switch (result.Result)
        {
            case null:
                return "null";
            case string text:
                return text;
            case JsonElement { ValueKind: JsonValueKind.String } element:
                return element.GetString() ?? string.Empty;
            case JsonElement element:
                return element.GetRawText();
        }

        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteValue(writer, result.Result);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case JsonElement element:
                element.WriteTo(writer);
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool boolean:
                writer.WriteBooleanValue(boolean);
                break;
            case int number:
                writer.WriteNumberValue(number);
                break;
            case long number:
                writer.WriteNumberValue(number);
                break;
            case double number:
                writer.WriteNumberValue(number);
                break;
            case float number:
                writer.WriteNumberValue(number);
                break;
            case decimal number:
                writer.WriteNumberValue(number);
                break;
            default:
                JsonSerializer.Serialize(writer, value, AIJsonUtilities.DefaultOptions.GetTypeInfo(value.GetType()));
                break;
        }
    }

    private sealed class PendingToolCall
    {
        public string? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public StringBuilder Arguments { get; } = new();

        public FunctionCallContent ToFunctionCall()
        {
            var callId = Id ?? Guid.NewGuid().ToString("N");
            var json = Arguments.ToString();

            if (string.IsNullOrWhiteSpace(json))
            {
                return new FunctionCallContent(callId, Name, new Dictionary<string, object?>());
            }

            try
            {
                using var document = JsonDocument.Parse(json);

                var arguments = new Dictionary<string, object?>();

                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        arguments[property.Name] = property.Value.Clone();
                    }
                }

                return new FunctionCallContent(callId, Name, arguments);
            }
            catch (JsonException exception)
            {
                return new FunctionCallContent(callId, Name) { Exception = exception };
            }
        }
    }
}
