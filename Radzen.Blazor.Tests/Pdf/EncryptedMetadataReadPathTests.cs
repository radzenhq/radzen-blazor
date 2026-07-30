#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.6.3.2: /Metadata stored as plaintext XMP when /EncryptMetadata is false; the reader must not run it through the stream cipher.
public class EncryptedMetadataReadPathTests
{
    private static readonly byte[] DocumentId = CryptoFixtureSupport.FixedBytes(16, 5);

    private const string Xmp =
        "<?xpacket begin=\"\"?><x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF/></x:xmpmeta><?xpacket end=\"r\"?>";

    // ISO 32000-1 7.6.3.3 step (f): 0xFFFFFFFF appended before the 50-round MD5 spin when /EncryptMetadata is false.
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

    private static byte[] BuildDocument(bool encryptMetadata)
    {
        const int permissions = -3904;
        var owner = CryptoFixtureSupport.R4OwnerEntry();
        var fileKey = encryptMetadata
            ? CryptoFixtureSupport.R4FileKey(owner, permissions, DocumentId)
            : FileKeyNoMetadata(owner, permissions, DocumentId);
        var user = CryptoFixtureSupport.R4UserEntry(fileKey, DocumentId);

        var plainContent = Encoding.Latin1.GetBytes("BT /F1 12 Tf 72 720 Td (metadata-read-marker) Tj ET");
        var content = CryptoFixtureSupport.AesEncrypt(
            CryptoFixtureSupport.AesV2ObjectKey(fileKey, 4, 0),
            CryptoFixtureSupport.FixedBytes(16, 71),
            plainContent);

        var xmp = Encoding.ASCII.GetBytes(Xmp);
        var metadata = encryptMetadata
            ? CryptoFixtureSupport.AesEncrypt(
                CryptoFixtureSupport.AesV2ObjectKey(fileKey, 6, 0),
                CryptoFixtureSupport.FixedBytes(16, 91),
                xmp)
            : xmp;

        var encrypt = "<< /Filter /Standard /V 4 /R 4 /Length 128"
            + " /CF << /StdCF << /CFM /AESV2 /AuthEvent /DocOpen /Length 16 >> >>"
            + " /StmF /StdCF /StrF /StdCF"
            + " /EncryptMetadata " + (encryptMetadata ? "true" : "false")
            + " /O " + CryptoFixtureSupport.Hex(owner)
            + " /U " + CryptoFixtureSupport.Hex(user)
            + " /P " + permissions + " >>";

        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Metadata 6 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n" + encrypt + "\nendobj\n");
        pdf.Mark(6);
        pdf.Append("6 0 obj\n<< /Type /Metadata /Subtype /XML /Length " + metadata.Length + " >>\nstream\n")
            .Append(metadata).Append("\nendstream\nendobj\n");

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

    private static string Content(DocumentReader reader)
    {
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
        var stream = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        return Encoding.Latin1.GetString(reader.DecodeStream(stream));
    }

    [Fact]
    public void EncryptMetadataFalse_MetadataStreamReadsAsPlaintextXmp()
    {
        var reader = DocumentReader.Parse(BuildDocument(encryptMetadata: false), "");

        Assert.True(reader.IsEncrypted);
        Assert.Equal(Xmp, Encoding.ASCII.GetString(Metadata(reader).Data.ToArray()));
    }

    [Fact]
    public void EncryptMetadataFalse_RegularStreamStillDecrypts()
    {
        var reader = DocumentReader.Parse(BuildDocument(encryptMetadata: false), "");

        Assert.Contains("(metadata-read-marker) Tj", Content(reader));
    }

    [Fact]
    public void EncryptMetadataTrue_MetadataStreamDecrypts()
    {
        var reader = DocumentReader.Parse(BuildDocument(encryptMetadata: true), "");

        Assert.Equal(Xmp, Encoding.ASCII.GetString(Metadata(reader).Data.ToArray()));
        Assert.Contains("(metadata-read-marker) Tj", Content(reader));
    }
}
