namespace Radzen;

/// <summary>
/// MD5 hash calculator. Forwards to the pure-managed
/// <see cref="Radzen.Documents.Pdf.Crypto.Md5"/> implementation.
/// </summary>
public class MD5
{
    /// <summary>
    /// Calculates the MD5 hash.
    /// </summary>
    /// <param name="input">The input bytes.</param>
    /// <returns>The MD5 hash as a string.</returns>
    public static string Calculate(byte[] input) => Radzen.Documents.Pdf.Crypto.Md5.ComputeHashHex(input);

    /// <summary>
    /// Computes the raw 16-byte MD5 digest of the input (RFC 1321).
    /// </summary>
    /// <param name="input">The bytes to hash.</param>
    /// <returns>The 16-byte digest.</returns>
    public static byte[] ComputeHash(byte[] input) => Radzen.Documents.Pdf.Crypto.Md5.ComputeHash(input);
}
