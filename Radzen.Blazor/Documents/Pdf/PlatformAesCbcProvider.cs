using System;
using System.Threading.Tasks;

namespace Radzen.Documents.Pdf;

/// <summary>
/// The <see cref="IAesCbcProvider"/> backed by <c>System.Security.Cryptography.Aes</c>. It is the
/// default on every target except browser-wasm and always completes synchronously, so the
/// synchronous entry points keep working.
/// </summary>
public sealed class PlatformAesCbcProvider : IAesCbcProvider
{
    /// <summary>Gets the shared instance. The type is stateless.</summary>
    public static PlatformAesCbcProvider Instance { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the platform AES implementation is usable here. It is
    /// <c>false</c> on browser-wasm, where a provider must be supplied explicitly.
    /// </summary>
    public static bool IsSupported => !OperatingSystem.IsBrowser();

    /// <inheritdoc />
    public ValueTask<byte[]> EncryptCbcAsync(
        ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> plaintext)
    {
#pragma warning disable RS0030
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key.ToArray();
        return ValueTask.FromResult(
            aes.EncryptCbc(plaintext.Span, iv.Span, System.Security.Cryptography.PaddingMode.None));
#pragma warning restore RS0030
    }

    /// <inheritdoc />
    public ValueTask<byte[]> DecryptCbcAsync(
        ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> ciphertext)
    {
#pragma warning disable RS0030
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key.ToArray();
        return ValueTask.FromResult(
            aes.DecryptCbc(ciphertext.Span, iv.Span, System.Security.Cryptography.PaddingMode.None));
#pragma warning restore RS0030
    }
}
