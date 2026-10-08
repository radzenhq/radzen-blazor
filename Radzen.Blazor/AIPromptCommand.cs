namespace Radzen.Blazor;

/// <summary>
/// A command shown under the output of <see cref="RadzenAIPrompt"/> that transforms the output with another instruction, for example "Make it shorter".
/// </summary>
public class AIPromptCommand
{
    /// <summary>
    /// Gets or sets the text of the command button.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon of the command button.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the instruction sent to the model together with the current output.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
}

/// <summary>
/// Supplies information about a <see cref="RadzenAIPrompt.Generated"/> event.
/// </summary>
public class AIPromptGeneratedEventArgs
{
    /// <summary>
    /// Gets the prompt that was sent to the model.
    /// </summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>
    /// Gets the command that produced the output, or <c>null</c> when the prompt was typed or picked from the suggestions.
    /// </summary>
    public AIPromptCommand? Command { get; init; }

    /// <summary>
    /// Gets the generated output.
    /// </summary>
    public string Output { get; init; } = string.Empty;
}
