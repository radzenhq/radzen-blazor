using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenUpload.Progress" /> event that is being raised.
/// </summary>
public class UploadProgressArgs
{
    /// <summary>
    /// Gets or sets the number of bytes that have been uploaded.
    /// </summary>
    public long Loaded { get; set; }

    /// <summary>
    /// Gets the total number of bytes that need to be uploaded.
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// Gets the progress as a percentage value (from <c>0</c> to <c>100</c>).
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Gets a collection of files that are being uploaded.
    /// </summary>
    public IEnumerable<FileInfo> Files { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether the underlying XMLHttpRequest should be aborted.
    /// </summary>
    public bool Cancel { get; set; }
}

