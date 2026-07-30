#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 algorithm 2 / 2.A: key derivation and password validation.
public class SecurityHandlerTests
{
    private static readonly byte[] Empty = [];

    private static StandardSecurityHandler Handler(string file, byte[] password)
    {
        var bytes = PdfTestResources.ReadAllBytes("Documents/" + file);
        var reader = DocumentReader.Parse(bytes, "owner");
        var encrypt = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Encrypt"]));
        var id = Assert.IsType<ArrayObject>(reader.Trailer["ID"]);
        var documentId = Encoding.Latin1.GetBytes(Assert.IsType<StringObject>(id[0]).Value);
        return new StandardSecurityHandler(encrypt, documentId, password);
    }

    [Theory]
    [InlineData("encrypted-rc4-40.pdf", 5)]
    [InlineData("encrypted-rc4-128.pdf", 16)]
    [InlineData("encrypted-aes-128.pdf", 16)]
    [InlineData("encrypted-aes-256.pdf", 32)]
    public void EmptyUserPassword_Validates(string file, int keyLength)
    {
        var handler = Handler(file, Empty);
        Assert.True(handler.IsUserPassword);
        Assert.Equal(keyLength, handler.FileKey.Length);
    }

    [Theory]
    [InlineData("encrypted-rc4-40.pdf")]
    [InlineData("encrypted-rc4-128.pdf")]
    [InlineData("encrypted-aes-128.pdf")]
    [InlineData("encrypted-aes-256.pdf")]
    public void WrongPassword_FailsValidation(string file)
    {
        var handler = Handler(file, Encoding.ASCII.GetBytes("definitely-wrong"));
        Assert.False(handler.IsUserPassword);
        Assert.False(handler.IsOwnerPassword);
    }

    // ISO 32000-1 algorithm 2 step (a): owner path via the /O entry.
    [Fact]
    public void OwnerPassword_ValidatesRc4()
    {
        var handler = Handler("encrypted-rc4-40.pdf", Encoding.ASCII.GetBytes("owner"));
        Assert.True(handler.IsOwnerPassword);
        Assert.Equal(5, handler.FileKey.Length);
    }

    [Fact]
    public void UserPasswordFixture_RequiresPassword()
    {
        Assert.False(Handler("encrypted-rc4-128-user.pdf", Empty).IsUserPassword);

        var handler = Handler("encrypted-rc4-128-user.pdf", Encoding.ASCII.GetBytes("user"));
        Assert.True(handler.IsUserPassword);
        Assert.Equal(16, handler.FileKey.Length);
    }

    // ISO 32000-1 algorithm 2.A: R6 (AESV3) owner path.
    [Fact]
    public void OwnerPassword_ValidatesAes256()
    {
        var handler = Handler("encrypted-aes-256-user.pdf", Encoding.ASCII.GetBytes("owner"));
        Assert.True(handler.IsOwnerPassword);
        Assert.Equal(32, handler.FileKey.Length);
    }
}
