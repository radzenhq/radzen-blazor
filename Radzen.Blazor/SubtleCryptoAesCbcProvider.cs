using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor;

internal interface ISubtleCbcBridge
{
    ValueTask<byte[]> EncryptAsync(byte[] key, byte[] iv, byte[] data);

    ValueTask<byte[]> DecryptAsync(byte[] key, byte[] iv, byte[] data);
}

/// <summary>
/// An <see cref="IAesCbcProvider"/> backed by the browser's <c>crypto.subtle</c>. Supply it through
/// <see cref="EncryptionOptions.AesProvider"/> or <see cref="LoadOptions.AesProvider"/> to read and
/// write AES-encrypted PDF documents on Blazor WebAssembly, where .NET has no AES implementation.
/// </summary>
/// <remarks>
/// It completes asynchronously, so it works only with the asynchronous document entry points -
/// <see cref="PortableDocument.SaveToStreamAsync"/>, <see cref="PortableDocument.ToArrayAsync"/> and
/// <see cref="PortableDocument.LoadFromStreamAsync(System.IO.Stream, LoadOptions)"/>.
/// </remarks>
public sealed class SubtleCryptoAesCbcProvider : IAesCbcProvider
{
    private const int BlockSize = 16;

    private readonly ISubtleCbcBridge bridge;

    /// <summary>
    /// Initializes a new instance that calls <c>crypto.subtle</c> through the given JavaScript runtime.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime to invoke.</param>
    public SubtleCryptoAesCbcProvider(IJSRuntime jsRuntime)
        : this(new JavaScriptSubtleCbcBridge(jsRuntime))
    {
    }

    internal SubtleCryptoAesCbcProvider(ISubtleCbcBridge bridge)
        => this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

    // W3C WebCryptoAPI 29.5.1: AES-CBC encryption always appends a PKCS#7 block.
    /// <inheritdoc />
    public async ValueTask<byte[]> EncryptCbcAsync(
        ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> plaintext)
    {
        if (plaintext.Length == 0)
        {
            return [];
        }

        var padded = await bridge.EncryptAsync(key.ToArray(), iv.ToArray(), plaintext.ToArray())
            .ConfigureAwait(false);
        return DropPadBlock(padded, plaintext.Length);
    }

    // W3C WebCryptoAPI 29.5.2: AES-CBC decryption always removes and validates a PKCS#7 block.
    /// <inheritdoc />
    public async ValueTask<byte[]> DecryptCbcAsync(
        ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> ciphertext)
    {
        if (ciphertext.Length == 0)
        {
            return [];
        }

        var bytes = ciphertext.ToArray();
        var keyBytes = key.ToArray();
        var trailer = await bridge.EncryptAsync(keyBytes, LastBlock(bytes), []).ConfigureAwait(false);
        var plaintext = await bridge.DecryptAsync(keyBytes, iv.ToArray(), Append(bytes, trailer))
            .ConfigureAwait(false);
        return Verify(plaintext, bytes.Length);
    }

    internal static byte[] DropPadBlock(byte[] padded, int length)
    {
        if (padded is null || padded.Length != length + BlockSize)
        {
            throw new InvalidOperationException(
                $"crypto.subtle returned {padded?.Length ?? -1} bytes for a {length}-byte AES-CBC encryption.");
        }

        return padded[..length];
    }

    internal static byte[] LastBlock(byte[] ciphertext)
    {
        if (ciphertext.Length < BlockSize || ciphertext.Length % BlockSize != 0)
        {
            throw new ArgumentException(
                "AES ciphertext length must be a whole number of 16-byte blocks.", nameof(ciphertext));
        }

        return ciphertext[^BlockSize..];
    }

    internal static byte[] Append(byte[] ciphertext, byte[] trailer)
    {
        if (trailer is null || trailer.Length != BlockSize)
        {
            throw new InvalidOperationException(
                $"crypto.subtle returned {trailer?.Length ?? -1} bytes for the AES-CBC padding block.");
        }

        var result = new byte[ciphertext.Length + BlockSize];
        Array.Copy(ciphertext, result, ciphertext.Length);
        Array.Copy(trailer, 0, result, ciphertext.Length, BlockSize);
        return result;
    }

    internal static byte[] Verify(byte[] plaintext, int length)
        => plaintext is not null && plaintext.Length == length
            ? plaintext
            : throw new InvalidOperationException(
                $"crypto.subtle returned {plaintext?.Length ?? -1} bytes for a {length}-byte AES-CBC decryption.");

    private sealed class JavaScriptSubtleCbcBridge(IJSRuntime jsRuntime) : ISubtleCbcBridge
    {
        private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

        public ValueTask<byte[]> EncryptAsync(byte[] key, byte[] iv, byte[] data)
            => Invoke("Radzen.aesCbcEncrypt", key, iv, data);

        public ValueTask<byte[]> DecryptAsync(byte[] key, byte[] iv, byte[] data)
            => Invoke("Radzen.aesCbcDecrypt", key, iv, data);

        private async ValueTask<byte[]> Invoke(string identifier, byte[] key, byte[] iv, byte[] data)
        {
            var result = await jsRuntime.InvokeAsync<string>(
                identifier, Convert.ToBase64String(key), Convert.ToBase64String(iv), Convert.ToBase64String(data))
                .ConfigureAwait(false);
            return Convert.FromBase64String(result);
        }
    }
}
