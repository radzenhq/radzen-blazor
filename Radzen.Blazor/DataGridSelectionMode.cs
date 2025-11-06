namespace Radzen;

/// <summary>
/// Specifies the selection mode behavior of <see cref="Radzen.Blazor.RadzenDataGrid{TItem}" />.
/// </summary>
public enum DataGridSelectionMode
{
    /// <summary>
    /// The user can select only one row at a time. Selecting a different row deselects the last selected row.
    /// </summary>
    Single,

    /// <summary>
    /// The user can select multiple rows.
    /// </summary>
    Multiple
}

