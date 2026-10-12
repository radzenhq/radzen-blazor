using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor;

/// <summary>
/// The kind of a <see cref="ChatMessage"/>: a message of the conversation or an entry the application adds around it.
/// </summary>
public enum ChatMessageKind
{
    /// <summary>
    /// A message of the user or the assistant.
    /// </summary>
    Message,

    /// <summary>
    /// A short note rendered centered between the messages, for example "Stopped".
    /// </summary>
    Status,

    /// <summary>
    /// Something in progress, rendered with a spinner. Remove it with <see cref="RadzenAIChat.RemoveMessage"/> when it ends.
    /// </summary>
    Progress,

    /// <summary>
    /// An error, rendered as an alert.
    /// </summary>
    Error
}

/// <summary>
/// Represents a chat message in the RadzenAIChat component.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this message is from the user.
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// Gets or sets the kind of the message. Default is <see cref="ChatMessageKind.Message"/>; the other kinds are entries the application adds with <see cref="RadzenAIChat.AddMessage(ChatMessage)"/>.
    /// </summary>
    public ChatMessageKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who sent the message.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets whether this message is currently streaming.
    /// </summary>
    public bool IsStreaming { get; set; }

    /// <summary>
    /// Gets or sets the role associated with the message (e.g., "user", "assistant").
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets the tool calls the assistant made while producing this message.
    /// </summary>
    public List<ChatToolCall> ToolCalls { get; set; } = new();

    /// <summary>
    /// Gets or sets the reasoning the model produced before answering, when the model exposes it.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Gets or sets the files attached to the message.
    /// </summary>
    public List<ChatAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Gets or sets the token usage reported by the model for this response, when the provider reports it.
    /// </summary>
    public Microsoft.Extensions.AI.UsageDetails? Usage { get; set; }

    /// <summary>
    /// Gets or sets the sources the answer is based on: citations the provider attached to the response and citations returned by tools.
    /// </summary>
    public List<ChatCitation> Citations { get; set; } = new();

    /// <summary>
    /// Gets or sets additional data about the message, for example the files an answer changed or the credits it used. Streaming responses collect the
    /// <see cref="Microsoft.Extensions.AI.ChatResponseUpdate.AdditionalProperties"/> of their updates here; render them with <see cref="RadzenAIChat.MessageFooterTemplate"/> or <see cref="RadzenAIChat.UsageTemplate"/>.
    /// </summary>
    public Microsoft.Extensions.AI.AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>
    /// Merges additional data into <see cref="AdditionalProperties"/>.
    /// </summary>
    /// <param name="properties">The data to merge. Nothing happens when it is <c>null</c> or empty.</param>
    public void MergeAdditionalProperties(Microsoft.Extensions.AI.AdditionalPropertiesDictionary? properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return;
        }

        AdditionalProperties ??= new Microsoft.Extensions.AI.AdditionalPropertiesDictionary();

        foreach (var property in properties)
        {
            AdditionalProperties[property.Key] = property.Value;
        }
    }

    /// <summary>
    /// Splits the message into its parts in the order the assistant produced them: the text written before a tool call, the call, the text written after it.
    /// Tool calls are placed by <see cref="ChatToolCall.TextPosition"/>; calls with the same position keep their order in <see cref="ToolCalls"/>. Empty text is skipped.
    /// Each text part is rendered as Markdown on its own, so a call that falls inside a list or a code block splits that block in two.
    /// </summary>
    /// <returns>The parts of the message.</returns>
    public IReadOnlyList<ChatMessagePart> GetParts()
    {
        var parts = new List<ChatMessagePart>();
        var content = Content ?? string.Empty;
        var start = 0;

        foreach (var call in ToolCalls.OrderBy(call => Math.Clamp(call.TextPosition, 0, content.Length)))
        {
            var position = Math.Clamp(call.TextPosition, 0, content.Length);

            if (position > start)
            {
                parts.Add(new ChatMessagePart(content.Substring(start, position - start)));
                start = position;
            }

            parts.Add(new ChatMessagePart(call));
        }

        if (start < content.Length)
        {
            parts.Add(new ChatMessagePart(content.Substring(start)));
        }

        return parts;
    }
}

/// <summary>
/// A part of a <see cref="ChatMessage"/> as returned by <see cref="ChatMessage.GetParts"/>: either a piece of text or a tool call.
/// </summary>
public sealed class ChatMessagePart
{
    internal ChatMessagePart(string text)
    {
        Text = text;
    }

    internal ChatMessagePart(ChatToolCall toolCall)
    {
        ToolCall = toolCall;
        Text = string.Empty;
    }

    /// <summary>
    /// Gets the text of the part. Empty when the part is a tool call.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the tool call of the part, or <c>null</c> when the part is text.
    /// </summary>
    public ChatToolCall? ToolCall { get; }

    /// <summary>
    /// Gets whether the part is text.
    /// </summary>
    public bool IsText => ToolCall == null;
}