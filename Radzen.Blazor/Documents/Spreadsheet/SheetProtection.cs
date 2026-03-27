namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Enumerates the discrete actions that sheet protection can block.
/// </summary>
public enum SheetAction
{
    /// <summary>Formatting cells (fonts, colors, borders).</summary>
    FormatCells,
    /// <summary>Formatting rows (height, visibility).</summary>
    FormatRows,
    /// <summary>Formatting columns (width, visibility).</summary>
    FormatColumns,
    /// <summary>Inserting columns.</summary>
    InsertColumns,
    /// <summary>Inserting rows.</summary>
    InsertRows,
    /// <summary>Inserting hyperlinks.</summary>
    InsertHyperlinks,
    /// <summary>Deleting columns.</summary>
    DeleteColumns,
    /// <summary>Deleting rows.</summary>
    DeleteRows,
    /// <summary>Sorting data.</summary>
    Sort,
    /// <summary>Using auto-filter.</summary>
    AutoFilter,
    /// <summary>Editing cell content.</summary>
    EditCell
}

/// <summary>
/// Represents sheet-level protection settings matching the XLSX sheetProtection element.
/// When <see cref="IsProtected"/> is true, actions are blocked unless the corresponding Allow property is true.
/// </summary>
public class SheetProtection
{
    /// <summary>
    /// Gets or sets whether sheet protection is active.
    /// </summary>
    public bool IsProtected { get; set; }

    /// <summary>Gets or sets whether formatting cells is allowed when the sheet is protected.</summary>
    public bool AllowFormatCells { get; set; }

    /// <summary>Gets or sets whether formatting rows is allowed when the sheet is protected.</summary>
    public bool AllowFormatRows { get; set; }

    /// <summary>Gets or sets whether formatting columns is allowed when the sheet is protected.</summary>
    public bool AllowFormatColumns { get; set; }

    /// <summary>Gets or sets whether inserting columns is allowed when the sheet is protected.</summary>
    public bool AllowInsertColumns { get; set; }

    /// <summary>Gets or sets whether inserting rows is allowed when the sheet is protected.</summary>
    public bool AllowInsertRows { get; set; }

    /// <summary>Gets or sets whether inserting hyperlinks is allowed when the sheet is protected.</summary>
    public bool AllowInsertHyperlinks { get; set; }

    /// <summary>Gets or sets whether deleting columns is allowed when the sheet is protected.</summary>
    public bool AllowDeleteColumns { get; set; }

    /// <summary>Gets or sets whether deleting rows is allowed when the sheet is protected.</summary>
    public bool AllowDeleteRows { get; set; }

    /// <summary>Gets or sets whether sorting is allowed when the sheet is protected.</summary>
    public bool AllowSort { get; set; }

    /// <summary>Gets or sets whether auto-filter is allowed when the sheet is protected.</summary>
    public bool AllowAutoFilter { get; set; }

    /// <summary>Gets or sets whether selecting locked cells is allowed when the sheet is protected.</summary>
    public bool AllowSelectLockedCells { get; set; } = true;

    /// <summary>Gets or sets whether selecting unlocked cells is allowed when the sheet is protected.</summary>
    public bool AllowSelectUnlockedCells { get; set; } = true;

    /// <summary>Gets or sets the legacy 16-bit password hash (4 hex characters).</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Gets or sets the hash algorithm name (e.g. "SHA-512").</summary>
    public string? AlgorithmName { get; set; }

    /// <summary>Gets or sets the base64-encoded password hash value.</summary>
    public string? HashValue { get; set; }

    /// <summary>Gets or sets the base64-encoded salt value.</summary>
    public string? SaltValue { get; set; }

    /// <summary>Gets or sets the number of hash iterations.</summary>
    public int? SpinCount { get; set; }

    /// <summary>
    /// Returns true if the given action is blocked by protection.
    /// </summary>
    public bool IsActionBlocked(SheetAction action)
    {
        if (!IsProtected)
        {
            return false;
        }

        return action switch
        {
            SheetAction.FormatCells => !AllowFormatCells,
            SheetAction.FormatRows => !AllowFormatRows,
            SheetAction.FormatColumns => !AllowFormatColumns,
            SheetAction.InsertColumns => !AllowInsertColumns,
            SheetAction.InsertRows => !AllowInsertRows,
            SheetAction.InsertHyperlinks => !AllowInsertHyperlinks,
            SheetAction.DeleteColumns => !AllowDeleteColumns,
            SheetAction.DeleteRows => !AllowDeleteRows,
            SheetAction.Sort => !AllowSort,
            SheetAction.AutoFilter => !AllowAutoFilter,
            SheetAction.EditCell => true,
            _ => false
        };
    }
}
