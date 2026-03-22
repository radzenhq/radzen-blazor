using System;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Specifies the anchor mode for a sheet image.
/// </summary>
public enum ImageAnchorMode
{
    /// <summary>
    /// The image is anchored to two cells (from and to).
    /// </summary>
    TwoCellAnchor,

    /// <summary>
    /// The image is anchored to one cell with explicit width and height.
    /// </summary>
    OneCellAnchor
}

/// <summary>
/// Represents a cell anchor position for an image.
/// </summary>
public class CellAnchor
{
    /// <summary>
    /// Gets or sets the column index.
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the column offset in EMU (English Metric Units). 1 px = 9525 EMU at 96 DPI.
    /// </summary>
    public long ColumnOffset { get; set; }

    /// <summary>
    /// Gets or sets the row index.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// Gets or sets the row offset in EMU (English Metric Units). 1 px = 9525 EMU at 96 DPI.
    /// </summary>
    public long RowOffset { get; set; }

    /// <summary>
    /// Creates a deep copy of this anchor.
    /// </summary>
    public CellAnchor Clone() => new()
    {
        Column = Column,
        ColumnOffset = ColumnOffset,
        Row = Row,
        RowOffset = RowOffset
    };
}

/// <summary>
/// Represents a floating image on a spreadsheet sheet.
/// </summary>
public class SheetImage
{
    /// <summary>
    /// Gets or sets the anchor mode.
    /// </summary>
    public ImageAnchorMode AnchorMode { get; set; }

    /// <summary>
    /// Gets or sets the starting anchor position.
    /// </summary>
    public CellAnchor From { get; set; } = new();

    /// <summary>
    /// Gets or sets the ending anchor position (TwoCellAnchor only).
    /// </summary>
    public CellAnchor? To { get; set; }

    /// <summary>
    /// Gets or sets the image width in EMU (OneCellAnchor only). 1 px = 9525 EMU at 96 DPI.
    /// </summary>
    public long Width { get; set; }

    /// <summary>
    /// Gets or sets the image height in EMU (OneCellAnchor only). 1 px = 9525 EMU at 96 DPI.
    /// </summary>
    public long Height { get; set; }

    private byte[] data = [];
    private string? dataUri;

    /// <summary>
    /// Gets or sets the image data bytes.
    /// </summary>
    public byte[] Data
    {
        get => data;
        set { data = value; dataUri = null; }
    }

    /// <summary>
    /// Gets or sets the content type (e.g. "image/png").
    /// </summary>
    public string ContentType
    {
        get => contentType;
        set { contentType = value; dataUri = null; }
    }

    private string contentType = "image/png";

    /// <summary>
    /// Gets the data URI for the image (cached).
    /// </summary>
    public string DataUri => dataUri ??= $"data:{ContentType};base64,{Convert.ToBase64String(Data)}";

    /// <summary>
    /// Gets or sets the optional name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }
}
