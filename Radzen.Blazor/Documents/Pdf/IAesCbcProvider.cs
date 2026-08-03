using System;
using System.Threading.Tasks;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Supplies the AES-CBC primitive that PDF standard security needs (ISO 32000-2 section 7.6.4).
/// The library ships no AES implementation of its own; it calls this interface instead.
/// </summary>
/// <remarks>
/// <para>
/// The contract is raw CBC over whole 16-byte blocks with no padding: the engine applies and
/// strips PKCS#7 itself. A browser implementation backed by <c>crypto.subtle</c> therefore does
/// not have to fight the Web Crypto AES-CBC padding rules - see
/// <see cref="Radzen.Blazor.SubtleCryptoAesCbcProvider"/>.
/// </para>
/// <para>
/// Implementations that complete synchronously keep the synchronous entry points
/// (<see cref="PortableDocument.SaveToStream"/>, <see cref="PortableDocument.ToArray"/>,
/// <see cref="PortableDocument.LoadFromStream(System.IO.Stream, LoadOptions)"/>) working. An
/// implementation that completes asynchronously requires the asynchronous entry points
/// (<see cref="PortableDocument.SaveToStreamAsync"/>, <see cref="PortableDocument.ToArrayAsync"/>,
/// <see cref="PortableDocument.LoadFromStreamAsync(System.IO.Stream, LoadOptions)"/>).
/// </para>
/// <para>
/// The provider is supplied per operation: <see cref="EncryptionOptions.AesProvider"/> for writing
/// and <see cref="LoadOptions.AesProvider"/> for reading.
/// </para>
/// </remarks>
public interface IAesCbcProvider
{
    /// <summary>
    /// Encrypts <paramref name="plaintext"/> in CBC mode with no padding.
    /// </summary>
    /// <param name="key">The AES key. 16, 24 or 32 bytes.</param>
    /// <param name="iv">The 16-byte initialization vector.</param>
    /// <param name="plaintext">The plaintext. Its length is a whole number of 16-byte blocks.</param>
    /// <returns>The ciphertext, exactly as long as <paramref name="plaintext"/>.</returns>
    ValueTask<byte[]> EncryptCbcAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> plaintext);

    /// <summary>
    /// Decrypts <paramref name="ciphertext"/> in CBC mode with no padding.
    /// </summary>
    /// <param name="key">The AES key. 16, 24 or 32 bytes.</param>
    /// <param name="iv">The 16-byte initialization vector.</param>
    /// <param name="ciphertext">The ciphertext. Its length is a whole number of 16-byte blocks.</param>
    /// <returns>The plaintext, exactly as long as <paramref name="ciphertext"/>.</returns>
    ValueTask<byte[]> DecryptCbcAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> ciphertext);
}
