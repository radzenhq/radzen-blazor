namespace Radzen;

/// <summary>
/// The result of an edit in <see cref="Radzen.Blazor.RadzenMarkdownEditor" />, sent to the browser to refresh the editing surface.
/// </summary>
public class MarkdownEditorUpdate
{
    /// <summary>The design surface HTML, or <c>null</c> when the surface does not need to be replaced.</summary>
    public string? Html { get; set; }

    /// <summary>The text segments of <see cref="Html" /> as flat triples of document position start, position end and rendered length.</summary>
    public int[]? Segments { get; set; }

    /// <summary>The whole markdown text, sent when the source textarea must be replaced.</summary>
    public string? Text { get; set; }

    /// <summary>The selection start after the edit: a position in the rendered document in Design mode, an offset in the markdown text in Source mode.</summary>
    public int SelectionStart { get; set; }

    /// <summary>The selection end after the edit, in the same units as <see cref="SelectionStart" />.</summary>
    public int SelectionEnd { get; set; }

    /// <summary>The tool state at the selection after the edit.</summary>
    public MarkdownEditorToolState? State { get; set; }

    /// <summary>The version of the edit this update answers, as sent by the browser.</summary>
    public int Version { get; set; }
}
