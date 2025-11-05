namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenUpload.Error" /> event that is being raised.
/// </summary>
public class UploadErrorEventArgs
{
    /// <summary>
    /// Gets a message telling what caused the error.
    /// </summary>
    public string Message { get; set; }
}

