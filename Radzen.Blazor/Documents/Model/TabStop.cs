using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// An explicit tab stop: a position and the alignment applied to the text that follows the tab.
/// </summary>
/// <param name="position">The distance of the stop from the paragraph content-box left edge.</param>
/// <param name="alignment">The alignment applied to the text following the tab. Defaults to <see cref="TabAlignment.Left"/>.</param>
/// <param name="leader">The character repeated to fill the tab gap (e.g. '.' for dot leaders). '\0' (the default) fills the gap with blank space.</param>
/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="position"/> is relative.</exception>
public sealed class TabStop(Unit position, TabAlignment alignment = TabAlignment.Left, char leader = '\0')
{
    /// <summary>Gets the distance of the stop from the paragraph content-box left edge.</summary>
    public Unit Position { get; } = AuthoredNumber.Absolute(position, "TabStop.Position");

    /// <summary>Gets the alignment applied to the text following the tab.</summary>
    public TabAlignment Alignment { get; } = alignment;

    /// <summary>Gets the character repeated to fill the tab gap, or '\0' when the gap is left blank.</summary>
    public char Leader { get; } = leader;
}
