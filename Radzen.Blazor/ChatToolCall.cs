using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Radzen.Blazor;

/// <summary>
/// The state of a tool call made by the assistant.
/// </summary>
public enum ChatToolCallStatus
{
    /// <summary>
    /// The tool is being invoked.
    /// </summary>
    Pending,

    /// <summary>
    /// The tool requires the user's approval before it is invoked.
    /// </summary>
    AwaitingApproval,

    /// <summary>
    /// The tool was invoked and returned a result.
    /// </summary>
    Completed,

    /// <summary>
    /// The tool threw an exception.
    /// </summary>
    Failed,

    /// <summary>
    /// The user rejected the tool call.
    /// </summary>
    Rejected
}

/// <summary>
/// Represents a tool (function) call made by the assistant as part of a <see cref="ChatMessage"/>.
/// </summary>
public class ChatToolCall
{
    /// <summary>
    /// Gets or sets the identifier of the call assigned by the model.
    /// </summary>
    public string CallId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments the model passed to the tool.
    /// </summary>
    public IDictionary<string, object?>? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the result returned by the tool. Functions created with <see cref="AIFunctionFactory"/> return a <see cref="JsonElement"/>.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets the exception thrown by the tool, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the status of the call.
    /// </summary>
    public ChatToolCallStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the approval request when the tool is an <see cref="ApprovalRequiredAIFunction"/>.
    /// </summary>
    public ToolApprovalRequestContent? ApprovalRequest { get; set; }

    /// <summary>
    /// Gets or sets the user's answer to <see cref="ApprovalRequest"/>.
    /// </summary>
    public ToolApprovalResponseContent? ApprovalResponse { get; set; }

    /// <summary>
    /// Gets the arguments formatted as <c>name: value</c> pairs for display.
    /// </summary>
    public string FormattedArguments => Arguments == null ? string.Empty : string.Join(", ", Arguments.Select(argument => $"{argument.Key}: {FormatValue(argument.Value)}"));

    /// <summary>
    /// Gets the arguments formatted as indented JSON for display.
    /// </summary>
    public string FormattedArgumentsJson
    {
        get
        {
            if (Arguments == null || Arguments.Count == 0)
            {
                return string.Empty;
            }

            var buffer = new System.Buffers.ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                writer.WriteStartObject();

                foreach (var argument in Arguments)
                {
                    writer.WritePropertyName(argument.Key);
                    WriteValue(writer, argument.Value);
                }

                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
    }

    /// <summary>
    /// Gets the result formatted for display: indented JSON for structured results, the text itself for strings.
    /// </summary>
    public string FormattedResult
    {
        get
        {
            switch (Result)
            {
                case null:
                    return string.Empty;
                case string text:
                    return text;
                case JsonElement { ValueKind: JsonValueKind.String } element:
                    return element.GetString() ?? string.Empty;
                case JsonElement element:
                    {
                        var buffer = new System.Buffers.ArrayBufferWriter<byte>();

                        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                        {
                            element.WriteTo(writer);
                        }

                        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
                    }
                default:
                    return Result.ToString() ?? string.Empty;
            }
        }
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
            case decimal number:
                writer.WriteNumberValue(number);
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    /// <summary>
    /// Deserializes <see cref="Result"/> to <typeparamref name="T"/>. Returns <c>default</c> when there is no result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="options">Optional serializer options. Defaults to <see cref="AIJsonUtilities.DefaultOptions"/>.</param>
    public T? GetResult<T>(JsonSerializerOptions? options = null)
    {
        options ??= AIJsonUtilities.DefaultOptions;

        switch (Result)
        {
            case null:
                return default;
            case T typed:
                return typed;
            case JsonElement element:
                return (T?)JsonSerializer.Deserialize(element, options.GetTypeInfo(typeof(T)));
            case string text when typeof(T) != typeof(string):
                return (T?)JsonSerializer.Deserialize(text, options.GetTypeInfo(typeof(T)));
            default:
                return (T?)JsonSerializer.Deserialize(JsonSerializer.SerializeToElement(Result, options.GetTypeInfo(Result.GetType())), options.GetTypeInfo(typeof(T)));
        }
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString() ?? string.Empty,
            JsonElement element => element.GetRawText(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
