namespace Radzen.Documents.Pdf;


/// <summary>
/// An explicit tab stop: a position and the alignment applied to the text that follows the tab.
/// </summary>
/// <param name="position">The distance of the stop from the paragraph content-box left edge.</param>
/// <param name="alignment">The alignment applied to the text following the tab. Defaults to <see cref="TabAlignment.Left"/>.</param>
public class TabStop(Unit position, TabAlignment alignment = TabAlignment.Left)
{
    /// <summary>Gets the distance of the stop from the paragraph content-box left edge.</summary>
    public Unit Position { get; } = position;

    /// <summary>Gets the alignment applied to the text following the tab.</summary>
    public TabAlignment Alignment { get; } = alignment;
}
