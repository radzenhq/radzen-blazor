using System;

namespace Radzen.Blazor;

/// <summary>
/// Represents context information for a user in a mention search result in the RadzenChat component.
/// </summary>
public class MentionUserContext
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets whether the user is currently in the chat.
    /// </summary>
    public bool IsInChat { get; set; }
}
