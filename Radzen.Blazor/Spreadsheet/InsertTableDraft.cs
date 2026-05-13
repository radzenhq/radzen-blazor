namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Mutable working state for the Insert Table dialog. Survives close/reopen
/// cycles when the user engages the range picker.
/// </summary>
public sealed class InsertTableDraft
{
    /// <summary>The table name to create.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the first row of the range is treated as a header.</summary>
    public bool HasHeaders { get; set; } = true;

    /// <summary>The sheet-qualified absolute range formula (e.g. <c>Sheet1!$A$1:$C$5</c>).</summary>
    public string RangeFormula { get; set; } = string.Empty;
}
