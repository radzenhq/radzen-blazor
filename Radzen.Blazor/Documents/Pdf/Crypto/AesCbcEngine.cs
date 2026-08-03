using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Radzen.Documents.Pdf.Crypto;

internal sealed class AesCbcEngine(IAesCbcProvider? supplied)
{
    internal const string NoProviderMessage =
        "PDF AES encryption needs an IAesCbcProvider on this platform. The .NET AES implementation throws "
        + "PlatformNotSupportedException on browser-wasm and Microsoft has stated it will not return "
        + "(dotnet/runtime#73858), so the library ships no default there. Set EncryptionOptions.AesProvider "
        + "or LoadOptions.AesProvider - Radzen.Blazor.SubtleCryptoAesCbcProvider is a crypto.subtle-backed "
        + "implementation - or perform the encryption outside the browser. RC4 and the revision 2 to 4 "
        + "handlers need no provider.";

    internal const string AsynchronousProviderMessage =
        "The supplied IAesCbcProvider did not complete synchronously. Use PortableDocument.SaveToStreamAsync, "
        + "PortableDocument.ToArrayAsync or PortableDocument.LoadFromStreamAsync with an asynchronous provider.";

    private IAesCbcProvider? resolved;

    internal static IAesCbcProvider Resolve(IAesCbcProvider? provider, bool isBrowser)
        => provider ?? (isBrowser
            ? throw new PlatformNotSupportedException(NoProviderMessage)
            : PlatformAesCbcProvider.Instance);

    public ValueTask<byte[]> EncryptNoPaddingAsync(byte[] key, byte[] iv, byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(iv);
        ArgumentNullException.ThrowIfNull(plaintext);
        RequireKey(key);
        RequireIv(iv);
        if (plaintext.Length % 16 != 0)
        {
            throw new ArgumentException(
                "AES plaintext length must be a whole number of 16-byte blocks.", nameof(plaintext));
        }

        return Sized(Provider.EncryptCbcAsync(key, iv, plaintext), plaintext.Length);
    }

    public ValueTask<byte[]> DecryptNoPaddingAsync(byte[] key, byte[] iv, byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(iv);
        ArgumentNullException.ThrowIfNull(ciphertext);
        RequireKey(key);
        RequireIv(iv);
        if (ciphertext.Length % 16 != 0)
        {
            throw new InvalidDataException("AES ciphertext length must be a whole number of 16-byte blocks.");
        }

        return Sized(Provider.DecryptCbcAsync(key, iv, ciphertext), ciphertext.Length);
    }

    public async ValueTask<byte[]> DecryptPaddedAsync(byte[] key, byte[] iv, byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (ciphertext.Length == 0)
        {
            throw new InvalidDataException("AES ciphertext is empty.");
        }

        return Pkcs7.Strip(await DecryptNoPaddingAsync(key, iv, ciphertext).ConfigureAwait(false));
    }

    public byte[] EncryptNoPadding(byte[] key, byte[] iv, byte[] plaintext)
        => Complete(EncryptNoPaddingAsync(key, iv, plaintext));

    public byte[] DecryptNoPadding(byte[] key, byte[] iv, byte[] ciphertext)
        => Complete(DecryptNoPaddingAsync(key, iv, ciphertext));

    public byte[] DecryptPadded(byte[] key, byte[] iv, byte[] ciphertext)
        => Complete(DecryptPaddedAsync(key, iv, ciphertext));

    public static T Complete<T>(ValueTask<T> operation)
        => operation.IsCompleted
            ? operation.GetAwaiter().GetResult()
            : throw new InvalidOperationException(AsynchronousProviderMessage);

    public static void Complete(ValueTask operation)
    {
        if (!operation.IsCompleted)
        {
            throw new InvalidOperationException(AsynchronousProviderMessage);
        }

        operation.GetAwaiter().GetResult();
    }

    private IAesCbcProvider Provider => resolved ??= Resolve(supplied, OperatingSystem.IsBrowser());

    private static async ValueTask<byte[]> Sized(ValueTask<byte[]> operation, int expected)
    {
        var result = await operation.ConfigureAwait(false);
        return result is not null && result.Length == expected
            ? result
            : throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "IAesCbcProvider returned {0} bytes for a {1}-byte block sequence.",
                result?.Length ?? -1,
                expected));
    }

    private static void RequireIv(byte[] iv)
    {
        if (iv.Length != 16)
        {
            throw new ArgumentException("AES initialization vector must be exactly 16 bytes.", nameof(iv));
        }
    }

    // FIPS 197 5.2: AES keys are 128/192/256-bit.
    private static void RequireKey(byte[] key)
    {
        if (key.Length is not (16 or 24 or 32))
        {
            throw new InvalidDataException("AES key length must be 16, 24, or 32 bytes.");
        }
    }
}
