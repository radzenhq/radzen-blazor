namespace Radzen;

/// <summary>Describes a file rejected before it was added to the upload selection.</summary>
public class UploadRejectedFile
{
    /// <summary>Gets or sets the metadata of the rejected file.</summary>
    public FileInfo File { get; set; } = new FileInfo();

    /// <summary>Gets or sets the rejection reason.</summary>
    public UploadRejectionReason Reason { get; set; }
}
