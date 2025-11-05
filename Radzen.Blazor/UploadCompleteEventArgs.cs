using System.Text.Json;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenUpload.Complete" /> event that is being raised.
/// </summary>
public class UploadCompleteEventArgs
{
    /// <summary>
    /// Gets the JSON response which the server returned after the upload.
    /// </summary>
    public JsonDocument JsonResponse { get; set; }

    /// <summary>
    /// Gets the raw server response.
    /// </summary>
    public string RawResponse { get; set; }

    /// <summary>
    /// Gets a boolean value indicating if the upload was cancelled by the user.
    /// </summary>
    public bool Cancelled { get; set; }
}

