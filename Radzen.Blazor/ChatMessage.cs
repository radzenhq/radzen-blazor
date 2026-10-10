using System;
using System.Collections.Generic;

namespace Radzen.Blazor;

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
}