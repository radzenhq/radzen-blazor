namespace Radzen.Documents.Pdf;


/// <summary>
/// Options controlling how a document is loaded by
/// <see cref="PortableDocument.LoadFromStream(System.IO.Stream, LoadOptions)"/>.
/// </summary>
public sealed class LoadOptions
{
    /// <summary>
    /// Gets or sets the user or owner password used to open an encrypted
    /// document. Use an empty string for a document with an empty user password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the AES-CBC implementation used to decrypt an AES-encrypted document. When
    /// <c>null</c> the platform implementation is used, except on browser-wasm where there is none
    /// and a provider is required. Documents encrypted with RC4 (revisions 2 to 4) need no provider.
    /// </summary>
    public IAesCbcProvider? AesProvider { get; set; }
}
