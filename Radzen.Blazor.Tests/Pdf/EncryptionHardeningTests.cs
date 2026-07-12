#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Hardening for the standard security handler and the hand-rolled AES against
// forged /Encrypt dictionaries that authenticate as encrypted but carry hostile
// key material: an out-of-range /Length (MD5 key path) and an empty or oversized
// /UE (R6 AESV3 key) must be rejected with DocumentParseException instead of
// crashing on an out-of-range slice or dividing by zero in key expansion. Valid
// RC4/AESV2/AESV3 material still authenticates.
public class EncryptionHardeningTests
{
    private static readonly byte[] Empty = [];
    private static readonly byte[] DocumentId = CryptoFixtureSupport.FixedBytes(16, 5);
    private static readonly byte[] ZeroIv = new byte[16];

    // /Length 1000000000 -> 125,000,000-byte slice of a 16-byte MD5 hash.
    [Fact]
    public void OutOfRangeLength_ThrowsDocumentParseException()
    {
        var encrypt = new DictionaryObject
        {
            ["Filter"] = new NameObject("Standard"),
            ["V"] = new NumberObject(2),
            ["R"] = new NumberObject(3),
            ["Length"] = new NumberObject(1000000000),
            ["P"] = new NumberObject(-4),
        };

        Assert.Throws<DocumentParseException>(() => new StandardSecurityHandler(encrypt, DocumentId, Empty));
    }

    [Fact]
    public void ValidAesV2Length_StillOpens()
    {
        var handler = HandlerFromResource("encrypted-aes-128.pdf");
        Assert.True(handler.IsUserPassword);
        Assert.Equal(16, handler.FileKey.Length);
    }

    // An empty /UE decrypts to a zero-length key (division by zero in AES key expansion).
    [Fact]
    public void Revision6_EmptyUserKey_ThrowsDocumentParseException()
    {
        var encrypt = R6Dictionary(userEncrypted: []);
        Assert.Throws<DocumentParseException>(() => new StandardSecurityHandler(encrypt, DocumentId, Empty));
    }

    // An oversized /UE decrypts to an oversized key (multi-second key schedule + OOM).
    [Fact]
    public void Revision6_OversizedUserKey_ThrowsDocumentParseException()
    {
        var encrypt = R6Dictionary(userEncrypted: CryptoFixtureSupport.FixedBytes(64, 1));
        Assert.Throws<DocumentParseException>(() => new StandardSecurityHandler(encrypt, DocumentId, Empty));
    }

    [Fact]
    public void Revision6_ValidUserKey_StillOpens()
    {
        var fileKey = CryptoFixtureSupport.FixedBytes(32, 7);
        var keySalt = CryptoFixtureSupport.FixedBytes(8, 22);
        var intermediate = CryptoFixtureSupport.Hash2B(Empty, keySalt, []);
        var userEncrypted = CryptoFixtureSupport.AesCbcNoPadding(intermediate, ZeroIv, fileKey);

        var encrypt = R6Dictionary(userEncrypted, keySalt);
        var handler = new StandardSecurityHandler(encrypt, DocumentId, Empty);

        Assert.True(handler.IsUserPassword);
        Assert.Equal(fileKey, handler.FileKey);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void AesExpandKey_AcceptsValidKeyLengths(int keyLength)
    {
        var exception = Record.Exception(
            () => AesCbc.DecryptCbcNoPadding(new byte[keyLength], ZeroIv, new byte[16]));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(64)]
    public void AesExpandKey_RejectsInvalidKeyLengths(int keyLength)
    {
        Assert.Throws<DocumentParseException>(
            () => AesCbc.DecryptCbcNoPadding(new byte[keyLength], ZeroIv, new byte[16]));
    }

    private static DictionaryObject R6Dictionary(byte[] userEncrypted)
        => R6Dictionary(userEncrypted, CryptoFixtureSupport.FixedBytes(8, 22));

    private static DictionaryObject R6Dictionary(byte[] userEncrypted, byte[] keySalt)
    {
        var validationSalt = CryptoFixtureSupport.FixedBytes(8, 11);
        var u = new byte[48];
        Array.Copy(CryptoFixtureSupport.Hash2B(Empty, validationSalt, []), u, 32);
        Array.Copy(validationSalt, 0, u, 32, 8);
        Array.Copy(keySalt, 0, u, 40, 8);

        return new DictionaryObject
        {
            ["Filter"] = new NameObject("Standard"),
            ["V"] = new NumberObject(5),
            ["R"] = new NumberObject(6),
            ["U"] = Str(u),
            ["UE"] = Str(userEncrypted),
            ["P"] = new NumberObject(-4),
        };
    }

    private static StringObject Str(byte[] bytes) => new(Encoding.Latin1.GetString(bytes));

    private static StandardSecurityHandler HandlerFromResource(string file)
    {
        var bytes = PdfTestResources.ReadAllBytes("Documents/" + file);
        var reader = DocumentReader.Parse(bytes, "owner");
        var encrypt = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Encrypt"]));
        var id = Assert.IsType<ArrayObject>(reader.Trailer["ID"]);
        var documentId = Encoding.Latin1.GetBytes(Assert.IsType<StringObject>(id[0]).Value);
        return new StandardSecurityHandler(encrypt, documentId, Empty);
    }
}
