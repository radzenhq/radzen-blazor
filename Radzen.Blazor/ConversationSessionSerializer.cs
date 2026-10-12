using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Radzen;

/// <summary>
/// Serializes <see cref="ConversationSession"/> instances to JSON and back, including the model history with tool calls, results and attachments.
/// Use it in <see cref="IConversationStore"/> implementations that keep conversations as text.
/// </summary>
public static class ConversationSessionSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(AIJsonUtilities.DefaultOptions);
        options.TypeInfoResolverChain.Insert(0, ConversationSessionJsonContext.Default);
        options.MakeReadOnly();
        return options;
    }

    /// <summary>
    /// Serializes a session to JSON.
    /// </summary>
    /// <param name="session">The session.</param>
    public static string Serialize(ConversationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return JsonSerializer.Serialize(session, Options.GetTypeInfo(typeof(ConversationSession)));
    }

    /// <summary>
    /// Deserializes a session from JSON produced by <see cref="Serialize"/>.
    /// </summary>
    /// <param name="json">The JSON.</param>
    public static ConversationSession? Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return (ConversationSession?)JsonSerializer.Deserialize(json, Options.GetTypeInfo(typeof(ConversationSession)));
    }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ConversationSession))]
[JsonSerializable(typeof(Blazor.ChatMessage), TypeInfoPropertyName = "RadzenChatMessage")]
[JsonSerializable(typeof(System.Collections.Generic.List<Blazor.ChatMessage>), TypeInfoPropertyName = "RadzenChatMessageList")]
[JsonSerializable(typeof(ChatMessage), TypeInfoPropertyName = "AIChatMessage")]
[JsonSerializable(typeof(System.Collections.Generic.List<ChatMessage>), TypeInfoPropertyName = "AIChatMessageList")]
internal partial class ConversationSessionJsonContext : JsonSerializerContext
{
}
