using System;
using System.Text.Json.Serialization;

namespace Radzen;

/// <summary>
/// Represents a filter in a component that supports filtering.
/// </summary>
public class FilterDescriptor
{
    /// <summary>
    /// Gets or sets the name of the filtered property.
    /// </summary>
    /// <value>The property.</value>
    public string Property { get; set; }

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    /// <value>The property type.</value>
    [JsonIgnore]
    public Type Type { get; set; }

    /// <summary>
    /// Gets or sets the name of the filtered property.
    /// </summary>
    /// <value>The property.</value>
    public string FilterProperty { get; set; }

    /// <summary>
    /// Gets or sets the value to filter by.
    /// </summary>
    /// <value>The filter value.</value>
    public object FilterValue { get; set; }

    /// <summary>
    /// Gets or sets the operator which will compare the property value with <see cref="FilterValue" />.
    /// </summary>
    /// <value>The filter operator.</value>
    public FilterOperator FilterOperator { get; set; }

    /// <summary>
    /// Gets or sets a second value to filter by.
    /// </summary>
    /// <value>The second filter value.</value>
    public object SecondFilterValue { get; set; }

    /// <summary>
    /// Gets or sets the operator which will compare the property value with <see cref="SecondFilterValue" />.
    /// </summary>
    /// <value>The second filter operator.</value>
    public FilterOperator SecondFilterOperator { get; set; }

    /// <summary>
    /// Gets or sets the logic used to combine the outcome of filtering by <see cref="FilterValue" /> and <see cref="SecondFilterValue" />.
    /// </summary>
    /// <value>The logical filter operator.</value>
    public LogicalFilterOperator LogicalFilterOperator { get; set; }

    /// <summary>
    /// Gets or sets the mode that determines whether the filter applies to any or all items in a collection.
    /// </summary>
    /// <value>
    /// A <see cref="CollectionFilterMode"/> value indicating whether the filter is satisfied by any or all items.
    /// </value>
    public CollectionFilterMode CollectionFilterMode { get; set; }
}

