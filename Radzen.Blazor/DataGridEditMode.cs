namespace Radzen;

/// <summary>
/// Specifies the inline edit mode behavior of <see cref="Radzen.Blazor.RadzenDataGrid{TItem}" />.
/// </summary>
public enum DataGridEditMode
{
    /// <summary>
    /// The user can edit only one row at a time. Editing a different row cancels edit mode for the last edited row.
    /// </summary>
    Single,

    /// <summary>
    /// The user can edit multiple rows.
    /// </summary>
    Multiple
}

