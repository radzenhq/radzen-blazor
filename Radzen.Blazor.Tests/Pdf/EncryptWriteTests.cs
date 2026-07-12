#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Write-side standard security handler. Uses the existing DocumentReader as an
// oracle: a small graph is written with EncryptionOptions, parsed back with the
// password, and the decrypted string and stream must equal the originals. The
// wrong password must be rejected, and a writer with no options must still
// produce byte-identical output.
public class EncryptWriteTests
{
    private const string StreamMarker = "BT /F1 12 Tf 72 720 Td (Hello encrypted world!) Tj ET";
    private const string StringMarker = "Encrypted string value";

    private static byte[] WriteGraph(EncryptionOptions? options)
    {
        using var buffer = new MemoryStream();
        var writer = new DocumentWriter(buffer) { Encryption = options };

        var catalog = new DictionaryObject { ["Type"] = new NameObject("Catalog") };
        var pages = new DictionaryObject { ["Type"] = new NameObject("Pages"), ["Count"] = new NumberObject(1) };
        var page = new DictionaryObject { ["Type"] = new NameObject("Page") };
        var content = new StreamObject(Encoding.Latin1.GetBytes(StreamMarker));

        var catalogRef = writer.Add(catalog);
        var pagesRef = writer.Add(pages);
        var pageRef = writer.Add(page);
        var contentRef = writer.Add(content);

        catalog["Pages"] = pagesRef;
        pages["Kids"] = new ArrayObject { pageRef };
        page["Parent"] = pagesRef;
        page["MediaBox"] = new ArrayObject
        {
            new NumberObject(0), new NumberObject(0), new NumberObject(612), new NumberObject(792),
        };
        page["Contents"] = contentRef;
        page["Marker"] = new StringObject(StringMarker);

        writer.Trailer["Root"] = catalogRef;
        writer.Close();
        return buffer.ToArray();
    }

    private static (DictionaryObject Page, DocumentReader Reader) OpenPage(byte[] pdf, string password)
    {
        var reader = DocumentReader.Parse(pdf, password);
        Assert.True(reader.IsEncrypted);
        var root = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(root["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
        return (page, reader);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Rc4)]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void EmptyUserPassword_RoundTripsStringAndStream(EncryptionAlgorithm algorithm)
    {
        var pdf = WriteGraph(new EncryptionOptions { Algorithm = algorithm });
        var (page, reader) = OpenPage(pdf, string.Empty);

        var content = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        Assert.Equal(StreamMarker, Encoding.Latin1.GetString(content.Data));

        var marker = Assert.IsType<StringObject>(reader.Resolve(page["Marker"]));
        Assert.Equal(StringMarker, marker.Value);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Rc4)]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void UserPassword_RoundTrips(EncryptionAlgorithm algorithm)
    {
        var pdf = WriteGraph(new EncryptionOptions { Algorithm = algorithm, UserPassword = "s3cret" });
        var (page, reader) = OpenPage(pdf, "s3cret");

        var content = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        Assert.Equal(StreamMarker, Encoding.Latin1.GetString(content.Data));
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Rc4)]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void OwnerPassword_Opens(EncryptionAlgorithm algorithm)
    {
        var pdf = WriteGraph(new EncryptionOptions
        {
            Algorithm = algorithm,
            UserPassword = "user-pw",
            OwnerPassword = "owner-pw",
        });

        var (page, reader) = OpenPage(pdf, "owner-pw");
        var content = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        Assert.Equal(StreamMarker, Encoding.Latin1.GetString(content.Data));
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Rc4)]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void WrongPassword_Throws(EncryptionAlgorithm algorithm)
    {
        var pdf = WriteGraph(new EncryptionOptions { Algorithm = algorithm, UserPassword = "correct" });
        Assert.Throws<InvalidPasswordException>(() => DocumentReader.Parse(pdf, "wrong"));
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Rc4, 2, 3)]
    [InlineData(EncryptionAlgorithm.Aes128, 4, 4)]
    [InlineData(EncryptionAlgorithm.Aes256, 5, 6)]
    public void EncryptDictionary_HasExpectedVersionRevisionAndPermissions(
        EncryptionAlgorithm algorithm, int version, int revision)
    {
        var pdf = WriteGraph(new EncryptionOptions
        {
            Algorithm = algorithm,
            AllowModification = false,
            AllowContentCopy = false,
        });

        var reader = DocumentReader.Parse(pdf, string.Empty);
        var encrypt = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Encrypt"]));

        Assert.Equal(version, IntValue(encrypt, "V"));
        Assert.Equal(revision, IntValue(encrypt, "R"));
        Assert.Equal("Standard", Assert.IsType<NameObject>(encrypt["Filter"]).Value);

        var permissions = IntValue(encrypt, "P");
        Assert.Equal(0, permissions & 0x008);
        Assert.Equal(0, permissions & 0x010);
        Assert.NotEqual(0, permissions & 0x004);
    }

    [Fact]
    public void NoEncryption_ProducesByteIdenticalOutput()
    {
        var first = WriteGraph(null);
        var second = WriteGraph(null);

        Assert.Equal(first, second);
        Assert.DoesNotContain("/Encrypt", Encoding.Latin1.GetString(first), StringComparison.Ordinal);
        Assert.False(DocumentReader.Parse(first).IsEncrypted);
    }

    private static int IntValue(DictionaryObject dictionary, string key)
        => Assert.IsType<NumberObject>(dictionary[key]).IntValue;
}
