#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class EncryptedDocumentTests
{
    private static byte[] Bytes(string file) => PdfTestResources.ReadAllBytes("Documents/" + file);

    private static DictionaryObject Dict(DocumentReader reader, DocumentObject value)
        => Assert.IsType<DictionaryObject>(reader.Resolve(value));

    private static string Name(DocumentObject value) => Assert.IsType<NameObject>(value).Value;

    private static DictionaryObject Page(DocumentReader reader)
    {
        var root = Dict(reader, reader.Trailer["Root"]);
        Assert.Equal("Catalog", Name(root["Type"]));
        var pages = Dict(reader, root["Pages"]);
        Assert.Equal("Pages", Name(pages["Type"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Dict(reader, kids[0]);
        Assert.Equal("Page", Name(page["Type"]));
        return page;
    }

    private static string ContentText(DocumentReader reader, DictionaryObject page)
    {
        var contents = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        var data = contents.Data.ToArray();
        if (contents.Dictionary.TryGetValue("Filter", out var filter)
            && filter is NameObject name && name.Value == "FlateDecode")
        {
            data = FlateFilter.Decode(data);
        }

        return Encoding.Latin1.GetString(data);
    }

    private static void AssertFixtureOpens(DocumentReader reader)
    {
        Assert.True(reader.IsEncrypted);
        var page = Page(reader);

        Assert.Contains("Hello encrypted world!", ContentText(reader, page));

        var resources = Dict(reader, page["Resources"]);
        var fonts = Dict(reader, resources["Font"]);
        var font = Dict(reader, fonts["F1"]);
        Assert.Equal("Helvetica", Name(font["BaseFont"]));
    }

    [Theory]
    [InlineData("encrypted-rc4-40.pdf")]
    [InlineData("encrypted-rc4-128.pdf")]
    [InlineData("encrypted-aes-128.pdf")]
    [InlineData("encrypted-aes-256.pdf")]
    public void EmptyUserPassword_DecryptsTransparently(string file)
    {
        AssertFixtureOpens(DocumentReader.Parse(Bytes(file), ""));
    }

    [Fact]
    public void PasswordlessOverload_OpensEmptyUserFile()
    {
        AssertFixtureOpens(DocumentReader.Parse(Bytes("encrypted-rc4-40.pdf")));
    }

    [Fact]
    public void StreamOverload_WithPassword_Decrypts()
    {
        using var stream = new MemoryStream(Bytes("encrypted-aes-128.pdf"));
        AssertFixtureOpens(DocumentReader.Parse(stream, ""));
    }

    [Fact]
    public void UserPasswordFixture_CorrectPassword_Opens()
    {
        AssertFixtureOpens(DocumentReader.Parse(Bytes("encrypted-rc4-128-user.pdf"), "user"));
    }

    [Fact]
    public void UserPasswordFixture_NoPassword_Throws()
    {
        Assert.Throws<InvalidPasswordException>(
            () => DocumentReader.Parse(Bytes("encrypted-rc4-128-user.pdf")));
    }

    [Fact]
    public void UserPasswordFixture_EmptyPassword_Throws()
    {
        Assert.Throws<InvalidPasswordException>(
            () => DocumentReader.Parse(Bytes("encrypted-rc4-128-user.pdf"), ""));
    }

    [Fact]
    public void WrongPassword_Throws()
    {
        Assert.Throws<InvalidPasswordException>(
            () => DocumentReader.Parse(Bytes("encrypted-rc4-40.pdf"), "definitely-wrong"));
    }

    [Fact]
    public void Aes256UserFixture_UserPassword_Opens()
    {
        AssertFixtureOpens(DocumentReader.Parse(Bytes("encrypted-aes-256-user.pdf"), "user"));
    }

    [Fact]
    public void Aes256UserFixture_OwnerPassword_Opens()
    {
        AssertFixtureOpens(DocumentReader.Parse(Bytes("encrypted-aes-256-user.pdf"), "owner"));
    }

    [Fact]
    public void UnencryptedFile_WithPassword_ParsesAndIsNotEncrypted()
    {
        var reader = DocumentReader.Parse(Bytes("simple-classic.pdf"), "ignored");
        Assert.False(reader.IsEncrypted);
    }

    [Fact]
    public void UnencryptedFile_Passwordless_IsNotEncrypted()
    {
        Assert.False(DocumentReader.Parse(Bytes("simple-classic.pdf")).IsEncrypted);
    }
}
