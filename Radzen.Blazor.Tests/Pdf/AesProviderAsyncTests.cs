#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Crypto;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class AesProviderAsyncTests
{
    private const string Title = "Eagerly decrypted title";
    private const string Password = "opensesame";

    private sealed class YieldingAesCbcProvider : IAesCbcProvider
    {
        public int Calls { get; private set; }

        public async ValueTask<byte[]> EncryptCbcAsync(
            ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> plaintext)
        {
            Calls++;
            await Task.Yield();
            return await PlatformAesCbcProvider.Instance.EncryptCbcAsync(key, iv, plaintext);
        }

        public async ValueTask<byte[]> DecryptCbcAsync(
            ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> ciphertext)
        {
            Calls++;
            await Task.Yield();
            return await PlatformAesCbcProvider.Instance.DecryptCbcAsync(key, iv, ciphertext);
        }
    }

    private sealed class NeverCompletingAesCbcProvider : IAesCbcProvider
    {
        public ValueTask<byte[]> EncryptCbcAsync(
            ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> plaintext)
            => Pending();

        public ValueTask<byte[]> DecryptCbcAsync(
            ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> ciphertext)
            => Pending();

        private static ValueTask<byte[]> Pending()
            => new(new TaskCompletionSource<byte[]>().Task);
    }

    private sealed class Pkcs7Bridge : ISubtleCbcBridge
    {
        public ValueTask<byte[]> EncryptAsync(byte[] key, byte[] iv, byte[] data)
            => Transform(key, iv, data, encrypt: true);

        public ValueTask<byte[]> DecryptAsync(byte[] key, byte[] iv, byte[] data)
            => Transform(key, iv, data, encrypt: false);

        private static async ValueTask<byte[]> Transform(byte[] key, byte[] iv, byte[] data, bool encrypt)
        {
            await Task.Yield();
            using var aes = Aes.Create();
            aes.Key = key;
            return encrypt
                ? aes.EncryptCbc(data, iv, PaddingMode.PKCS7)
                : aes.DecryptCbc(data, iv, PaddingMode.PKCS7);
        }
    }

    private static PortableDocument Build(EncryptionAlgorithm algorithm, IAesCbcProvider? provider)
    {
        var document = new PortableDocument();
        document.Info.Title = Title;
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (secret) Tj ET"));
        document.Encryption = new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7, 9, 11]),
            Algorithm = algorithm,
            UserPassword = Password,
            AesProvider = provider,
        };
        return document;
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public async Task AsyncSave_WithAnAsynchronousProvider_MatchesTheSynchronousBytes(EncryptionAlgorithm algorithm)
    {
        var expected = Build(algorithm, null).ToArray();

        var provider = new YieldingAesCbcProvider();
        var actual = await Build(algorithm, provider).ToArrayAsync();

        Assert.True(provider.Calls > 0);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public async Task AsyncSaveToStream_MatchesAsyncToArray(EncryptionAlgorithm algorithm)
    {
        using var buffer = new MemoryStream();
        await Build(algorithm, new YieldingAesCbcProvider()).SaveToStreamAsync(buffer);

        Assert.Equal(Build(algorithm, null).ToArray(), buffer.ToArray());
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void SyncSave_WithAnAsynchronousProvider_DirectsToTheAsyncApi(EncryptionAlgorithm algorithm)
    {
        var document = Build(algorithm, new NeverCompletingAesCbcProvider());

        var exception = Assert.Throws<InvalidOperationException>(() => document.ToArray());
        Assert.Contains("SaveToStreamAsync", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ToArrayAsync", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public async Task AsyncLoad_DecryptsEagerly_AndTheDocumentStaysSynchronous(EncryptionAlgorithm algorithm)
    {
        var bytes = Build(algorithm, null).ToArray();

        using var source = new MemoryStream(bytes);
        var provider = new YieldingAesCbcProvider();
        var loaded = await PortableDocument.LoadFromStreamAsync(
            source, new LoadOptions { Password = Password, AesProvider = provider });

        Assert.True(provider.Calls > 0);
        Assert.Equal(Title, loaded.Info.Title);
        Assert.Single(loaded.Pages);
        Assert.Contains(
            "secret", Encoding.Latin1.GetString(loaded.Pages[0].GetContent() ?? []), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void SyncLoad_WithAnAsynchronousProvider_DirectsToTheAsyncApi(EncryptionAlgorithm algorithm)
    {
        var bytes = Build(algorithm, null).ToArray();

        using var source = new MemoryStream(bytes);
        var exception = Assert.Throws<InvalidOperationException>(() => PortableDocument.LoadFromStream(
            source, new LoadOptions { Password = Password, AesProvider = new NeverCompletingAesCbcProvider() }));
        Assert.Contains("LoadFromStreamAsync", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rc4_NeedsNoProvider_AndStaysSynchronous()
    {
        var document = Build(EncryptionAlgorithm.Rc4, new YieldingAesCbcProvider());
        var bytes = document.ToArray();

        using var source = new MemoryStream(bytes);
        var loaded = PortableDocument.LoadFromStream(source, new LoadOptions { Password = Password });
        Assert.Equal(Title, loaded.Info.Title);
    }

    [Fact]
    public void NoProviderOnBrowser_ThrowsAPlatformMessageNamingTheInterface()
    {
        var exception = Assert.Throws<PlatformNotSupportedException>(
            () => AesCbcEngine.Resolve(null, isBrowser: true));

        Assert.Contains("IAesCbcProvider", exception.Message, StringComparison.Ordinal);
        Assert.Contains("EncryptionOptions.AesProvider", exception.Message, StringComparison.Ordinal);
        Assert.Contains("LoadOptions.AesProvider", exception.Message, StringComparison.Ordinal);
        Assert.Contains("dotnet/runtime#73858", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoProviderOffBrowser_UsesThePlatformImplementation()
    {
        Assert.Same(PlatformAesCbcProvider.Instance, AesCbcEngine.Resolve(null, isBrowser: false));
    }

    [Fact]
    public void ASuppliedProviderWinsEverywhere()
    {
        var supplied = new YieldingAesCbcProvider();

        Assert.Same(supplied, AesCbcEngine.Resolve(supplied, isBrowser: true));
        Assert.Same(supplied, AesCbcEngine.Resolve(supplied, isBrowser: false));
    }

    [Theory]
    [InlineData(16, 16)]
    [InlineData(16, 64)]
    [InlineData(24, 32)]
    [InlineData(32, 16)]
    [InlineData(32, 160)]
    public async Task SubtleCryptoBlockTricks_MatchThePlatformProvider(int keyLength, int dataLength)
    {
        var key = Fixed(keyLength, 3);
        var iv = Fixed(16, 5);
        var plaintext = Fixed(dataLength, 7);
        var subtle = new SubtleCryptoAesCbcProvider(new Pkcs7Bridge());

        var expected = await PlatformAesCbcProvider.Instance.EncryptCbcAsync(key, iv, plaintext);
        var actual = await subtle.EncryptCbcAsync(key, iv, plaintext);
        Assert.Equal(expected, actual);
        Assert.Equal(plaintext, await subtle.DecryptCbcAsync(key, iv, expected));
    }

    [Fact]
    public async Task SubtleCryptoProvider_HandlesEmptyInput()
    {
        var subtle = new SubtleCryptoAesCbcProvider(new Pkcs7Bridge());

        Assert.Empty(await subtle.EncryptCbcAsync(Fixed(16, 1), Fixed(16, 2), Array.Empty<byte>()));
        Assert.Empty(await subtle.DecryptCbcAsync(Fixed(16, 1), Fixed(16, 2), Array.Empty<byte>()));
    }

    [Fact]
    public void SubtleCryptoBlockTricks_RejectMisshapenBridgeResults()
    {
        Assert.Throws<InvalidOperationException>(() => SubtleCryptoAesCbcProvider.DropPadBlock(new byte[16], 16));
        Assert.Throws<InvalidOperationException>(() => SubtleCryptoAesCbcProvider.Append(new byte[16], new byte[8]));
        Assert.Throws<InvalidOperationException>(() => SubtleCryptoAesCbcProvider.Verify(new byte[8], 16));
        Assert.Throws<ArgumentException>(() => SubtleCryptoAesCbcProvider.LastBlock(new byte[20]));
    }

    [Fact]
    public async Task SubtleCryptoProvider_DrivesTheDocumentPipeline()
    {
        var expected = Build(EncryptionAlgorithm.Aes256, null).ToArray();
        var subtle = new SubtleCryptoAesCbcProvider(new Pkcs7Bridge());

        var actual = await Build(EncryptionAlgorithm.Aes256, subtle).ToArrayAsync();
        Assert.Equal(expected, actual);

        using var source = new MemoryStream(actual);
        var loaded = await PortableDocument.LoadFromStreamAsync(
            source, new LoadOptions { Password = Password, AesProvider = subtle });
        Assert.Equal(Title, loaded.Info.Title);
    }

    private static byte[] Fixed(int length, int seed)
    {
        var result = new byte[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = (byte)((i * 31) + seed);
        }

        return result;
    }
}
