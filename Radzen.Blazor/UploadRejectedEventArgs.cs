using System;
using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Describes files rejected during a file selection and the limits used for that selection.
/// </summary>
public class UploadRejectedEventArgs
{
    /// <summary>Gets or sets the rejected files.</summary>
    public IEnumerable<UploadRejectedFile> Files { get; set; } = Array.Empty<UploadRejectedFile>();

    /// <summary>Gets or sets the maximum allowed size in bytes, or null if unrestricted.</summary>
    public long? MaxFileSize { get; set; }

    /// <summary>Gets or sets the allowed filename suffixes used for the selection.</summary>
    public IEnumerable<string> AllowedExtensions { get; set; } = Array.Empty<string>();
}
