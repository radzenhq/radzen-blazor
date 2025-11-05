using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// DataGrid settings class used to Save/Load settings.
/// </summary>
public class DataGridSettings
{
    /// <summary>
    /// Columns.
    /// </summary>
    public IEnumerable<DataGridColumnSettings> Columns { get; set; }

    /// <summary>
    /// Groups.
    /// </summary>
    public IEnumerable<GroupDescriptor> Groups { get; set; }

    /// <summary>
    /// CurrentPage.
    /// </summary>
    public int? CurrentPage { get; set; }

    /// <summary>
    /// PageSize.
    /// </summary>
    public int? PageSize { get; set; }
}

