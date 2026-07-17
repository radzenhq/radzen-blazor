#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.4.10: a /Crypt filter names a crypt filter from /CF; /Identity means not encrypted
public class CryptFilterTests
{
    private static readonly byte[] DocumentId = CryptoFixtureSupport.FixedBytes(16, 5);

    private const string Xmp =
        "<?xpacket begin=\"\"?><x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF/></x:xmpmeta><?xpacket end=\"r\"?>";

    private const string Marker = "BT /F1 12 Tf 72 720 Td (crypt-filter-marker) Tj ET";

    // ISO 32000-1 7.6.3.3 step (f): 0xFFFFFFFF appended before the 50-round MD5 spin
    private static byte[] FileKeyNoMetadata(byte[] owner, int permissions, byte[] documentId)
    {
        var p = new[]
        {
            (byte)permissions,
            (byte)(permissions >> 8),
            (byte)(permissions >> 16),
            (byte)(permissions >> 24),
        };

        var seed = new byte[CryptoFixtureSupport.Pad32.Length + owner.Length + 4 + documentId.Length + 4];
        var pos = 0;
        Array.Copy(CryptoFixtureSupport.Pad32, 0, seed, pos, CryptoFixtureSupport.Pad32.Length);
        pos += CryptoFixtureSupport.Pad32.Length;
        Array.Copy(owner, 0, seed, pos, owner.Length);
        pos += owner.Length;
        Array.Copy(p, 0, seed, pos, 4);
        pos += 4;
        Array.Copy(documentId, 0, seed, pos, documentId.Length);
        pos += documentId.Length;
        seed[pos] = 0xFF;
        seed[pos + 1] = 0xFF;
        seed[pos + 2] = 0xFF;
        seed[pos + 3] = 0xFF;

        var hash = System.Security.Cryptography.MD5.HashData(seed);
        for (var i = 0; i < 50; i++)
        {
            hash = System.Security.Cryptography.MD5.HashData(hash[..16]);
        }

        return hash[..16];
    }

    private static byte[] BuildDocument(string metadataCryptName, string contentCryptName)
    {
        const int permissions = -3904;
        var owner = CryptoFixtureSupport.R4OwnerEntry();
        var fileKey = FileKeyNoMetadata(owner, permissions, DocumentId);
        var user = CryptoFixtureSupport.R4UserEntry(fileKey, DocumentId);

        var content = FlateFilter.Encode(Encoding.Latin1.GetBytes(Marker));
        var xmp = Encoding.ASCII.GetBytes(Xmp);

        var encrypt = "<< /Filter /Standard /V 4 /R 4 /Length 128"
            + " /CF << /StdCF << /CFM /AESV2 /AuthEvent /DocOpen /Length 16 >> >>"
            + " /StmF /StdCF /StrF /StdCF /EncryptMetadata false"
            + " /O " + CryptoFixtureSupport.Hex(owner)
            + " /U " + CryptoFixtureSupport.Hex(user)
            + " /P " + permissions + " >>";

        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Metadata 6 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length
                + " /Filter [/Crypt /FlateDecode]"
                + " /DecodeParms [<< /Type /CryptFilterDecodeParms /Name /" + contentCryptName + " >> null]"
                + " >>\nstream\n")
            .Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n" + encrypt + "\nendobj\n");
        pdf.Mark(6);
        pdf.Append("6 0 obj\n<< /Type /Metadata /Subtype /XML /Length " + xmp.Length
                + " /Filter [/Crypt]"
                + " /DecodeParms [<< /Type /CryptFilterDecodeParms /Name /" + metadataCryptName + " >>]"
                + " >>\nstream\n")
            .Append(xmp).Append("\nendstream\nendobj\n");

        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 7\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 7; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        var id = CryptoFixtureSupport.Hex(DocumentId);
        pdf.Append("trailer\n<< /Size 7 /Root 1 0 R /Encrypt 5 0 R /ID [" + id + " " + id + "] >>\n")
            .Append("startxref\n" + xrefOffset + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static StreamObject Metadata(DocumentReader reader)
    {
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        return Assert.IsType<StreamObject>(reader.Resolve(catalog["Metadata"]));
    }

    private static StreamObject Content(DocumentReader reader)
    {
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
        return Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
    }

    private static byte[] Decode(string name, DictionaryObject? parms)
        => StreamFilterRegistry.Get(name).Decode(
            Encoding.ASCII.GetBytes("payload"), parms, ReaderLimits.Default.MaxDecodedStreamBytes);

    private static DictionaryObject Parms(string name)
    {
        var parms = new DictionaryObject();
        parms["Name"] = new NameObject(name);
        return parms;
    }

    [Fact]
    public void Get_ResolvesCryptFilter()
    {
        Assert.NotNull(StreamFilterRegistry.Get("Crypt"));
    }

    [Fact]
    public void CryptFilter_IdentityName_PassesDataThrough()
    {
        Assert.Equal("payload", Encoding.ASCII.GetString(Decode("Crypt", Parms("Identity"))));
    }

    // ISO 32000-1 Table 14: /Name defaults to /Identity when absent
    [Fact]
    public void CryptFilter_WithoutParms_PassesDataThrough()
    {
        Assert.Equal("payload", Encoding.ASCII.GetString(Decode("Crypt", null)));
    }

    [Fact]
    public void CryptFilter_NamedFilter_ThrowsRatherThanReturningCiphertext()
    {
        var exception = Assert.Throws<DocumentParseException>(() => Decode("Crypt", Parms("StdCF")));

        Assert.Contains("StdCF", exception.Message);
    }

    [Fact]
    public void IdentityCryptMetadata_ReadsAsPlaintextXmp()
    {
        var reader = DocumentReader.Parse(BuildDocument("Identity", "Identity"), "");

        Assert.True(reader.IsEncrypted);
        Assert.Equal(Xmp, Encoding.ASCII.GetString(reader.DecodeStream(Metadata(reader))));
    }

    [Fact]
    public void IdentityCryptStream_IsNotDecryptedWithStmF()
    {
        var reader = DocumentReader.Parse(BuildDocument("Identity", "Identity"), "");

        Assert.Equal(Marker, Encoding.Latin1.GetString(reader.DecodeStream(Content(reader))));
    }

    [Fact]
    public void NamedCryptStream_FailsLoudInsteadOfYieldingCiphertext()
    {
        var reader = DocumentReader.Parse(BuildDocument("Identity", "StdCF"), "");

        Assert.Throws<DocumentParseException>(() => reader.DecodeStream(Content(reader)));
    }

    [Fact]
    public void LoadFromStream_IdentityCryptMetadata_ReadsXmpPacket()
    {
        using var stream = new MemoryStream(BuildDocument("Identity", "Identity"));

        var document = Document.LoadFromStream(stream, new LoadOptions { Password = "" });

        Assert.Equal(1, document.Pages.Count);
    }
}
