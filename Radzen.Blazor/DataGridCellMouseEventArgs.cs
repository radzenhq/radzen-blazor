using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.CellContextMenu" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridCellMouseEventArgs<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] T> : Microsoft.AspNetCore.Components.Web.MouseEventArgs where T : notnull
{
    /// <summary>
    /// Gets the data item which the clicked DataGrid row represents.
    /// </summary>
    public T? Data { get; internal set; }

    /// <summary>
    /// Gets the RadzenDataGridColumn which this cells represents.
    /// </summary>
    public RadzenDataGridColumn<T>? Column { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the default action has been prevented.
    /// </summary>
    public bool IsDefaultPrevented { get; private set; }

    /// <summary>
    /// Prevents the default action of the DataGrid for the same click. Call it in <see cref="RadzenDataGrid{TItem}.CellClick" /> to stop the DataGrid
    /// from raising <see cref="RadzenDataGrid{TItem}.RowClick" /> and selecting the row, or in <see cref="RadzenDataGrid{TItem}.CellDoubleClick" />
    /// to stop it from raising <see cref="RadzenDataGrid{TItem}.RowDoubleClick" />. An asynchronous <see cref="RadzenDataGrid{TItem}.CellClick" /> handler should call it before its first <c>await</c>.
    /// </summary>
    public void PreventDefault()
    {
        IsDefaultPrevented = true;
    }
}

