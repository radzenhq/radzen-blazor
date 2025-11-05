namespace Radzen;

/// <summary>
/// DataGrid column settings class used to Save/Load settings.
/// </summary>
public class DataGridColumnSettings
{
    /// <summary>
    /// Property.
    /// </summary>
    public string UniqueID { get; set; }

    /// <summary>
    /// Property.
    /// </summary>
    public string Property { get; set; }

    /// <summary>
    /// Visible.
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Width.
    /// </summary>
    public string Width { get; set; }

    /// <summary>
    /// OrderIndex.
    /// </summary>
    public int? OrderIndex { get; set; }

    /// <summary>
    /// SortOrder.
    /// </summary>
    public SortOrder? SortOrder { get; set; }

    /// <summary>
    /// SortIndex.
    /// </summary>
    public int? SortIndex { get; set; }

    /// <summary>
    /// FilterValue.
    /// </summary>
    public object FilterValue { get; set; }

    /// <summary>
    /// FilterOperator.
    /// </summary>
    public FilterOperator FilterOperator { get; set; }

    /// <summary>
    /// SecondFilterValue.
    /// </summary>
    public object SecondFilterValue { get; set; }

    /// <summary>
    /// SecondFilterOperator.
    /// </summary>
    public FilterOperator SecondFilterOperator { get; set; }

    /// <summary>
    /// LogicalFilterOperator.
    /// </summary>
    public LogicalFilterOperator LogicalFilterOperator { get; set; }

    /// <summary>
    /// CustomFilterExpression.
    /// </summary>
    public string CustomFilterExpression { get; set; }

    /// <summary>
    /// Gets or sets the mode that determines whether the filter applies to any or all items in a collection.
    /// </summary>
    /// <value>
    /// A <see cref="CollectionFilterMode"/> value indicating whether the filter is satisfied by any or all items.
    /// </value>
    public CollectionFilterMode CollectionFilterMode { get; set; }
}

