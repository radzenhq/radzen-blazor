using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenUpload.Change" /> event that is being raised.
/// </summary>
public class UploadChangeEventArgs
{
    /// <summary>
    /// Gets a collection of the selected files.
    /// </summary>
    public IEnumerable<FileInfo> Files { get; set; }
}

