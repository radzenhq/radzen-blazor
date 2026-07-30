#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.6.3.2: a plaintext /Metadata stream (/EncryptMetadata false) must not be run through the cipher on the way out.
public class EncryptionMetadataStreamTests
{
    private const string Xmp =
        "<?xpacket begin=\"\"?><x:xmpmeta xmlns:x=\"adobe:ns:meta/\"></x:xmpmeta><?xpacket end=\"r\"?>";

    [Fact]
    public void MetadataStream_EncryptMetadataFalse_ReturnsPlaintextXmp()
    {
        var handler = new StandardSecurityHandler(BuildEncrypt(encryptMetadata: false), DocumentId(), string.Empty);
        var xmp = Encoding.ASCII.GetBytes(Xmp);

        var result = handler.DecryptStream(xmp, 4, 0, MetadataDictionary());

        Assert.Equal(xmp, result);
    }

    [Fact]
    public void MetadataStream_EncryptMetadataFalse_NonMetadataStreamStillDecrypts()
    {
        var handler = new StandardSecurityHandler(BuildEncrypt(encryptMetadata: false), DocumentId(), string.Empty);
        var data = Encoding.ASCII.GetBytes(Xmp);

        var result = handler.DecryptStream(data, 4, 0, new DictionaryObject());

        Assert.NotEqual(data, result);
    }

    [Fact]
    public void MetadataStream_EncryptMetadataTrue_DecryptsLikeAnyStream()
    {
        var handler = new StandardSecurityHandler(BuildEncrypt(encryptMetadata: true), DocumentId(), string.Empty);
        var data = Encoding.ASCII.GetBytes(Xmp);

        var withDictionary = handler.DecryptStream(data, 4, 0, MetadataDictionary());
        var plain = handler.DecryptStream(data, 4, 0);

        Assert.Equal(plain, withDictionary);
        Assert.NotEqual(data, withDictionary);
    }

    private static byte[] DocumentId() => Encoding.Latin1.GetBytes("0123456789abcdef");

    private static DictionaryObject MetadataDictionary()
    {
        var dictionary = new DictionaryObject();
        dictionary["Type"] = new NameObject("Metadata");
        dictionary["Subtype"] = new NameObject("XML");
        return dictionary;
    }

    private static DictionaryObject BuildEncrypt(bool encryptMetadata)
    {
        var std = new DictionaryObject();
        std["CFM"] = new NameObject("V2");
        var cf = new DictionaryObject();
        cf["StdCF"] = std;

        var encrypt = new DictionaryObject();
        encrypt["Filter"] = new NameObject("Standard");
        encrypt["V"] = new NumberObject(4);
        encrypt["R"] = new NumberObject(4);
        encrypt["Length"] = new NumberObject(128);
        encrypt["P"] = new NumberObject(-4);
        encrypt["O"] = new StringObject(new string('O', 32));
        encrypt["U"] = new StringObject(new string('U', 32));
        encrypt["CF"] = cf;
        encrypt["StmF"] = new NameObject("StdCF");
        encrypt["StrF"] = new NameObject("StdCF");
        encrypt["EncryptMetadata"] = new BooleanObject(encryptMetadata);
        return encrypt;
    }
}
