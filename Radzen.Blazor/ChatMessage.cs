using System;

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
    public string Role { get; set; }
}