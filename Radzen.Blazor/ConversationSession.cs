using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;
using ChatMessage = Radzen.Blazor.ChatMessage;

namespace Radzen;

/// <summary>
/// Represents a conversation session with memory.
/// </summary>
public class ConversationSession
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation session.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the user the conversation belongs to. Set through <see cref="Blazor.RadzenAIChat.UserId"/> or <see cref="IAIChatService.GetOrCreateSessionAsync"/>; <see cref="IConversationStore.ListAsync"/> filters by it.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the title of the conversation. Defaults to the beginning of the first user message.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the list of messages in the conversation as displayed by <see cref="Blazor.RadzenAIChat"/>.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the conversation history sent to the model, including tool calls and tool results.
    /// </summary>
    public List<Microsoft.Extensions.AI.ChatMessage> History { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the timestamp when the conversation was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the maximum number of messages to keep in memory.
    /// </summary>
    public int MaxMessages { get; set; } = 50;

    /// <summary>
    /// Adds a message to the conversation and manages memory limits.
    /// </summary>
    /// <param name="role">The role of the message sender.</param>
    /// <param name="content">The message content.</param>
    public void AddMessage(string role, string content)
    {
        AddMessage(new ChatMessage
        {
            UserId = role,
            Role = role,
            IsUser = role == "user",
            Content = content,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// Adds a message to the conversation and manages memory limits.
    /// </summary>
    /// <param name="message">The message.</param>
    public void AddMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Messages.Add(message);

        if (string.IsNullOrEmpty(Title) && message.IsUser && !string.IsNullOrWhiteSpace(message.Content))
        {
            var text = message.Content.Trim();
            Title = text.Length > 60 ? text.Substring(0, 60).TrimEnd() + "…" : text;
        }

        LastUpdated = DateTime.Now;

        while (Messages.Count > MaxMessages)
        {
            Messages.RemoveAt(0);
        }
    }

    /// <summary>
    /// Adds messages to the history sent to the model and manages memory limits. Tool calls and their results are kept together.
    /// </summary>
    /// <param name="messages">The messages.</param>
    public void AddHistory(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        History.AddRange(messages);

        LastUpdated = DateTime.Now;

        if (History.Count <= MaxMessages)
        {
            return;
        }

        History.RemoveRange(0, History.Count - MaxMessages);

        while (History.Count > 0 && !(History[0].Role == ChatRole.User && History[0].Contents.OfType<TextContent>().Any()))
        {
            History.RemoveAt(0);
        }
    }

    /// <summary>
    /// Clears all messages from the conversation.
    /// </summary>
    public void Clear()
    {
        Messages.Clear();
        History.Clear();
        Title = null;
        LastUpdated = DateTime.Now;
    }
}
