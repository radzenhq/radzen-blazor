namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A contiguous run of text sharing the same font.
/// </summary>
public class Run
{
    /// <summary>
    /// Initializes a new <see cref="Run"/> with the specified text.
    /// </summary>
    /// <param name="text">The run text.</param>
    public Run(string text) => Text = text;

    /// <summary>Gets or sets the run text.</summary>
    public string Text { get; set; }

    /// <summary>Gets the run font.</summary>
    public Font Font { get; } = new();
}
