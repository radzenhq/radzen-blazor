using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;

namespace Radzen;

/// <summary>
/// Contains a LINQ query in a serializable format.
/// </summary>
public class Query
{
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
    /// <value>The filter parameters.</value>
    public IEnumerable<FilterDescriptor> Filters { get; set; }

    /// <summary>
    /// Gets the sort expression as a collection of sort descriptors.
    /// </summary>
    /// <value>The sorts.</value>
    public IEnumerable<SortDescriptor> Sorts { get; set; }

    /// <summary>
    /// Gets or sets the filter parameters.
    /// </summary>
    /// <value>The filter parameters.</value>
    public object[] FilterParameters { get; set; }

    /// <summary>
    /// Gets or sets the order by.
    /// </summary>
    /// <value>The order by.</value>
    public string OrderBy { get; set; }

    /// <summary>
    /// Gets or sets the expand.
    /// </summary>
    /// <value>The expand.</value>
    public string Expand { get; set; }

    /// <summary>
    /// Gets or sets the select.
    /// </summary>
    /// <value>The select.</value>
    public string Select { get; set; }

    /// <summary>
    /// Gets or sets the skip.
    /// </summary>
    /// <value>The skip.</value>
    public int? Skip { get; set; }

    /// <summary>
    /// Gets or sets the top.
    /// </summary>
    /// <value>The top.</value>
    public int? Top { get; set; }

    /// <summary>
    /// Converts the query to OData query format.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <returns>System.String.</returns>
    public string ToUrl(string url)
    {
        var queryParameters = new Dictionary<string, object>();

        if (Skip != null)
        {
            queryParameters.Add("$skip", Skip.Value);
        }

        if (Top != null)
        {
            queryParameters.Add("$top", Top.Value);
        }

        if (!string.IsNullOrEmpty(OrderBy))
        {
            queryParameters.Add("$orderBy", OrderBy);
        }

        if (!string.IsNullOrEmpty(Filter))
        {
            queryParameters.Add("$filter", UrlEncoder.Default.Encode(Filter));
        }

        if (!string.IsNullOrEmpty(Expand))
        {
            queryParameters.Add("$expand", Expand);
        }

        if (!string.IsNullOrEmpty(Select))
        {
            queryParameters.Add("$select", Select);
        }

        return string.Format("{0}{1}", url, queryParameters.Any() ? "?" + string.Join("&", queryParameters.Select(a => $"{a.Key}={a.Value}")) : "");
    }
}

