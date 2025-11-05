using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Internal class for managing hierarchical child data in DataGrid.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
internal class DataGridChildData<T>
{
    /// <summary>
    /// Gets or sets the parent child data.
    /// </summary>
    internal DataGridChildData<T> ParentChildData { get; set; }

    /// <summary>
    /// Gets or sets the level.
    /// </summary>
    internal int Level { get; set; }

    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    internal IEnumerable<T> Data { get; set; }
}

