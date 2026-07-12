namespace Radzen.Documents.Pdf;


/// <summary>
/// A contiguous run of text sharing the same font.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="Run"/> with the specified text.
/// </remarks>
/// <param name="text">The run text.</param>
public class Run(string text)
{

    /// <summary>Gets or sets the run text.</summary>
    public string Text { get; set; } = text;

    /// <summary>
    /// Gets or sets the hyperlink target URI. When set, every laid-out fragment of
    /// this run is covered by a link annotation opening the URI.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>Gets the run font.</summary>
    public Font Font { get; } = new();

    internal Font? EffectiveFont { get; set; }

    internal Font ResolvedFont => EffectiveFont ?? Font;
}
