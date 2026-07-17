#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.6.5: /StmF and /StrF name a crypt filter in /CF whose /CFM selects the cipher; an unknown /CFM or missing named filter must fail loud.
public class EncryptionCryptFilterMethodTests
{
    private static readonly byte[] DocumentId = Encoding.Latin1.GetBytes("0123456789ABCDEF");

    private static StandardSecurityHandler FixtureHandler(string file)
    {
        var bytes = PdfTestResources.ReadAllBytes("Documents/" + file);
        var reader = DocumentReader.Parse(bytes, "owner");
        var encrypt = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Encrypt"]));
        var id = Assert.IsType<ArrayObject>(reader.Trailer["ID"]);
        var documentId = Encoding.Latin1.GetBytes(Assert.IsType<StringObject>(id[0]).Value);
        return new StandardSecurityHandler(encrypt, documentId, string.Empty);
    }

    private static DictionaryObject BaseEncrypt(DictionaryObject cf, string streamFilter)
        => new()
        {
            ["Filter"] = new NameObject("Standard"),
            ["V"] = new NumberObject(4),
            ["R"] = new NumberObject(4),
            ["P"] = new NumberObject(-4),
            ["Length"] = new NumberObject(128),
            ["O"] = new StringObject(new string('\0', 32)),
            ["U"] = new StringObject(new string('\0', 32)),
            ["StmF"] = new NameObject(streamFilter),
            ["StrF"] = new NameObject(streamFilter),
            ["CF"] = cf,
        };

    [Fact]
    public void UnknownCryptFilterMethod_Throws()
    {
        var cf = new DictionaryObject
        {
            ["StdCF"] = new DictionaryObject { ["CFM"] = new NameObject("FooBar") },
        };
        var encrypt = BaseEncrypt(cf, "StdCF");
        Assert.Throws<DocumentParseException>(() => new StandardSecurityHandler(encrypt, DocumentId, string.Empty));
    }

    [Fact]
    public void NamedCryptFilterAbsentFromCf_Throws()
    {
        var cf = new DictionaryObject
        {
            ["StdCF"] = new DictionaryObject { ["CFM"] = new NameObject("AESV2") },
        };
        var encrypt = BaseEncrypt(cf, "MissingCF");
        Assert.Throws<DocumentParseException>(() => new StandardSecurityHandler(encrypt, DocumentId, string.Empty));
    }

    [Fact]
    public void IdentityFilterOpens()
    {
        var cf = new DictionaryObject
        {
            ["StdCF"] = new DictionaryObject { ["CFM"] = new NameObject("AESV2") },
        };
        var encrypt = BaseEncrypt(cf, "Identity");
        var handler = new StandardSecurityHandler(encrypt, DocumentId, string.Empty);
        Assert.NotNull(handler);
    }

    [Theory]
    [InlineData("encrypted-rc4-128.pdf")]
    [InlineData("encrypted-aes-128.pdf")]
    [InlineData("encrypted-aes-256.pdf")]
    public void SupportedCryptFilters_StillOpen(string file)
    {
        var handler = FixtureHandler(file);
        Assert.True(handler.IsUserPassword);
    }
}
