namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a drawing on a spreadsheet sheet positioned through cell anchors,
/// such as a <see cref="SheetImage"/> or a <see cref="SheetChart"/>.
/// </summary>
public interface IAnchoredDrawing
{
    /// <summary>
    /// Gets or sets the anchor mode.
    /// </summary>
    DrawingAnchorMode AnchorMode { get; set; }

    /// <summary>
    /// Gets or sets the starting anchor position.
    /// </summary>
    CellAnchor From { get; set; }

    /// <summary>
    /// Gets or sets the ending anchor position (TwoCellAnchor only).
    /// </summary>
    CellAnchor? To { get; set; }

    /// <summary>
    /// Gets or sets the drawing width in pixels (OneCellAnchor only).
    /// </summary>
    double Width { get; set; }

    /// <summary>
    /// Gets or sets the drawing height in pixels (OneCellAnchor only).
    /// </summary>
    double Height { get; set; }
}
