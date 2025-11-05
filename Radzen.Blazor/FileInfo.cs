using System;
using System.Threading;
using Microsoft.AspNetCore.Components.Forms;

namespace Radzen;

/// <summary>
/// Represents a file which the user selects for upload via <see cref="Radzen.Blazor.RadzenUpload" />.
/// </summary>
public class FileInfo : IBrowserFile
{
    /// <summary>
    /// Creates FileInfo.
    /// </summary>
    public FileInfo()
    {
        //
    }

    private IBrowserFile source;

    /// <summary>
    /// Creates FileInfo with IBrowserFile as source.
    /// </summary>
    /// <param name="source">The source browser file.</param>
    public FileInfo(IBrowserFile source)
    {
        this.source = source;
    }

    private string name;

    /// <summary>
    /// Gets the name of the selected file.
    /// </summary>
    public string Name
    {
        get
        {
            return name ?? source.Name;
        }
        set
        {
            name = value;
        }
    }

    private long size;

    /// <summary>
    /// Gets the size (in bytes) of the selected file.
    /// </summary>
    public long Size
    {
        get
        {
            return size != default(long) ? size : source != null ? source.Size : 0;
        }
        set
        {
            size = value;
        }
    }

    /// <summary>
    /// Gets the IBrowserFile source.
    /// </summary>
    public IBrowserFile Source => source;

    /// <summary>
    /// Gets the LastModified.
    /// </summary>
    public DateTimeOffset LastModified => source.LastModified;

    /// <summary>
    /// Gets the ContentType.
    /// </summary>
    public string ContentType => source.ContentType;

    /// <summary>
    /// Open read stream.
    /// </summary>
    /// <param name="maxAllowedSize">The maximum allowed size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stream.</returns>
    public System.IO.Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        return source.OpenReadStream(maxAllowedSize, cancellationToken);
    }
}

