namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// Options controlling how a document is loaded by
/// <see cref="Document.LoadFromStream"/>.
/// </summary>
public sealed class LoadOptions
{
    /// <summary>
    /// Gets or sets the user or owner password used to open an encrypted
    /// document. Use an empty string for a document with an empty user password.
    /// </summary>
    public string? Password { get; set; }
}
