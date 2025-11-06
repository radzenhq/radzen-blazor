using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenDataGrid{TItem}.LoadColumnFilterData" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridLoadColumnFilterDataEventArgs<T>
{
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    /// <value>The data.</value>
    public IEnumerable Data { get; set; }

    /// <summary>
    /// Gets or sets the total data count.
    /// </summary>
    /// <value>The total data count.</value>
    public int Count { get; set; }

    /// <summary>
    /// Gets how many items to skip. Related to paging and the current page. Usually used with the <see cref="Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/> LINQ method.
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Gets how many items to take. Related to paging and the current page size. Usually used with the <see cref="Enumerable.Take{TSource}(IEnumerable{TSource}, int)"/> LINQ method.
    /// </summary>
    /// <value>The top.</value>
    public int? Top { get; set; }

    /// <summary>
    /// Gets the filter expression as a string.
    /// </summary>
    /// <value>The filter.</value>
    public string Filter { get; internal set; }

    /// <summary>
    /// Gets or sets filter property used to limit and distinct values, if not set, args.Data are used as values.
    /// </summary>
    /// <value>The filter property.</value>
    public string Property { get; set; }

    /// <summary>
    /// Gets the RadzenDataGridColumn.
    /// </summary>
    public Radzen.Blazor.RadzenDataGridColumn<T> Column { get; internal set; }
}

