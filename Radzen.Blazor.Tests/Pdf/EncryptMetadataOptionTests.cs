#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class EncryptMetadataOptionTests
{
    private const string StreamMarker = "BT /F1 12 Tf 72 720 Td (metadata flag test) Tj ET";
    private const string StringMarker = "Encrypted string marker";

    private static byte[] WriteGraph(EncryptionOptions options)
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

    private static DictionaryObject Encrypt(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Encrypt"]));

    private static DictionaryObject FirstPage(DocumentReader reader)
    {
        var root = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(root["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        return Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
    }

    [Fact]
    public void EncryptMetadataDefaultsToTrue()
    {
        Assert.True(new EncryptionOptions().EncryptMetadata);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void EncryptMetadataFalse_WritesFlagFalse(EncryptionAlgorithm algorithm)
    {
        var pdf = WriteGraph(new EncryptionOptions { Material = new SeededEncryptionMaterial([7]), Algorithm = algorithm, EncryptMetadata = false });
        var reader = DocumentReader.Parse(pdf, string.Empty);

        var encrypt = Encrypt(reader);
        Assert.True(encrypt.TryGetValue("EncryptMetadata", out var value));
        var flag = Assert.IsType<BooleanObject>(reader.Resolve(value!));
        Assert.False(flag.Value);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void EncryptMetadataFalse_StillDecrypts(EncryptionAlgorithm algorithm)
    {
        var pdf = WriteGraph(new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7]),
            Algorithm = algorithm,
            EncryptMetadata = false,
            UserPassword = "s3cret",
        });
        var reader = DocumentReader.Parse(pdf, "s3cret");
        Assert.True(reader.IsEncrypted);

        var page = FirstPage(reader);
        var content = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        Assert.Equal(StreamMarker, Encoding.Latin1.GetString(content.Data.ToArray()));

        var marker = Assert.IsType<StringObject>(reader.Resolve(page["Marker"]));
        Assert.Equal(StringMarker, marker.Value);
    }

    [Theory]
    [InlineData(EncryptionAlgorithm.Rc4)]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void EncryptMetadataTrue_OmitsFlag(EncryptionAlgorithm algorithm)
    {
        var pdf = WriteGraph(new EncryptionOptions { Material = new SeededEncryptionMaterial([7]), Algorithm = algorithm });
        var reader = DocumentReader.Parse(pdf, string.Empty);

        Assert.False(Encrypt(reader).ContainsKey("EncryptMetadata"));
    }
}
