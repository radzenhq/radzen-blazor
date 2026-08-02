namespace Radzen.Documents.Pdf;

/// <summary>
/// Selects the standard security handler algorithm used when writing an
/// encrypted PDF (ISO 32000-1 section 7.6.3, ISO 32000-2 section 7.6.4).
/// </summary>
public enum EncryptionAlgorithm
{
    /// <summary>RC4 with a 128-bit key (/V 2 /R 3).</summary>
    Rc4,

    /// <summary>AES-128 in CBC mode (/V 4 /R 4, crypt filter /AESV2).</summary>
    Aes128,

    /// <summary>AES-256 in CBC mode (/V 5 /R 6, crypt filter /AESV3).</summary>
    Aes256,
}

/// <summary>
/// Configures standard PDF encryption for a <see cref="Objects.DocumentWriter"/>. When
/// assigned to <see cref="Objects.DocumentWriter.Encryption"/> every string and stream is
/// encrypted, an <c>/Encrypt</c> dictionary is written, and a document <c>/ID</c>
/// is generated.
/// </summary>
/// <remarks>
/// The salts, encryption key and document identifier are generated with a
/// cryptographically secure random number generator by default, so setting a password
/// and the permission flags is enough to produce a securely encrypted document.
/// Consequently two saves of the same content differ byte for byte.
/// </remarks>
public sealed class EncryptionOptions
{
    /// <summary>
    /// Gets or sets the user password required to open the document. An empty
    /// string (the default) produces a file that opens without a password prompt.
    /// </summary>
    public string UserPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner password granting full permissions. When empty the
    /// user password is used as the owner password (ISO 32000-1 algorithm 3).
    /// </summary>
    public string OwnerPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encryption algorithm. Defaults to <see cref="EncryptionAlgorithm.Aes128"/>.
    /// </summary>
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.Aes128;

    internal IEncryptionMaterial? Material { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document metadata is encrypted
    /// along with the rest of the file. Defaults to <c>true</c>. When set to
    /// <c>false</c> with a crypt-filter handler (<see cref="EncryptionAlgorithm.Aes128"/>
    /// or <see cref="EncryptionAlgorithm.Aes256"/>) the <c>/Encrypt</c> dictionary
    /// carries <c>/EncryptMetadata false</c> and the flag is folded into the key
    /// derivation (ISO 32000-1 7.6.3.2). It has no effect on
    /// <see cref="EncryptionAlgorithm.Rc4"/>, whose handler predates the flag.
    /// </summary>
    public bool EncryptMetadata { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether printing is permitted (/P bit 3).</summary>
    public bool AllowPrinting { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether high-resolution printing is permitted (/P bit 12).</summary>
    public bool AllowHighResPrinting { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether modifying the document is permitted (/P bit 4).</summary>
    public bool AllowModification { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether copying content is permitted (/P bit 5).</summary>
    public bool AllowContentCopy { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether adding or modifying annotations is permitted (/P bit 6).</summary>
    public bool AllowAnnotation { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether filling in form fields is permitted (/P bit 9).</summary>
    public bool AllowFormFill { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether assembling the document is permitted (/P bit 11).</summary>
    public bool AllowAssembly { get; set; } = true;

    // ISO 32000-1 Table 22: bits 1-2 are 0, bits 7-8 and 13-32 are 1, bit 10 always set.
    private const int ReservedPermissions = unchecked((int)0xFFFFF0C0) | 0x200;

    // ISO 32000-1 Table 22: maps the permission flags onto the /P integer.
    internal int ToPermissions()
    {
        var value = ReservedPermissions;
        if (AllowPrinting)
        {
            value |= 0x004;
        }

        if (AllowModification)
        {
            value |= 0x008;
        }

        if (AllowContentCopy)
        {
            value |= 0x010;
        }

        if (AllowAnnotation)
        {
            value |= 0x020;
        }

        if (AllowFormFill)
        {
            value |= 0x100;
        }

        if (AllowAssembly)
        {
            value |= 0x400;
        }

        if (AllowHighResPrinting)
        {
            value |= 0x800;
        }

        return value;
    }
}
