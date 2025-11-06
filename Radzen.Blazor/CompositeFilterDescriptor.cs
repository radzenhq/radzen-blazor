using System;
using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Represents a filter in a component that supports filtering.
/// </summary>
public class CompositeFilterDescriptor
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
    public FilterOperator? FilterOperator { get; set; }

    /// <summary>
    /// Gets or sets the logic used to combine the outcome of filtering by <see cref="FilterValue" />.
    /// </summary>
    /// <value>The logical filter operator.</value>
    public LogicalFilterOperator LogicalFilterOperator { get; set; }

    /// <summary>
    /// Gets or sets the filters.
    /// </summary>
    /// <value>The filters.</value>
    public IEnumerable<CompositeFilterDescriptor> Filters { get; set; }
}

