using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="PagedDataBoundComponent{TItem}.LoadData" /> event that is being raised.
/// </summary>
public class LoadDataArgs
{
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
    /// Gets the sort expression as a string.
    /// </summary>
    public string OrderBy { get; set; }

    private Func<string> getFilter;

    internal Func<string> GetFilter
    {
        get
        {
            return getFilter;
        }
        set
        {
            filter = null;
            getFilter = value;
        }
    }

    private string filter;

    /// <summary>
    /// Gets the filter expression as a string.
    /// </summary>
    /// <value>The filter.</value>
    public string Filter
    {
        get
        {
            if (filter == null && GetFilter != null)
            {
                filter = GetFilter();
            }
            return filter;
        }
        set
        {
            filter = value;
        }
    }

    /// <summary>
    /// Gets the filter expression as a collection of filter descriptors.
    /// </summary>
    public IEnumerable<FilterDescriptor> Filters { get; set; }

    /// <summary>
    /// Gets the sort expression as a collection of sort descriptors.
    /// </summary>
    /// <value>The sorts.</value>
    public IEnumerable<SortDescriptor> Sorts { get; set; }
}

