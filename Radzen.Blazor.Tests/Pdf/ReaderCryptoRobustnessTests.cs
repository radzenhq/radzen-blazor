using System.IO;
#nullable enable
using System;
using System.Text;
using Radzen.Documents.Crypto;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// Revision 6 passwords are UTF-8 (ISO 32000-2 7.6.4.3.3).
// /Identity crypt filter means the data is not encrypted (ISO 32000-1 7.6.5).
public class ReaderCryptoRobustnessTests
{
    private static readonly byte[] DocumentId = CryptoFixtureSupport.FixedBytes(16, 5);


    [Theory]
    [InlineData(17)]
    [InlineData(20)]
    [InlineData(31)]
    public void AesCbcDecrypt_OddLengthCiphertext_Throws(int length)
    {
        var key = CryptoFixtureSupport.FixedBytes(32, 9);
        var iv = CryptoFixtureSupport.FixedBytes(16, 7);
        var data = CryptoFixtureSupport.FixedBytes(length, 3);

        Assert.Throws<InvalidDataException>(() => AesCbc.Decrypt(key, iv, data));
    }


    private static byte[] EncryptedFile(string encryptDict, byte[] content, string? infoBody)
    {
        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R "
            + "/Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n")
            .Append(content)
            .Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n" + encryptDict + "\nendobj\n");

        var size = 6;
        if (infoBody is not null)
        {
            pdf.Object(6, "6 0 obj\n" + infoBody + "\nendobj\n");
            size = 7;
        }

        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 " + size + "\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < size; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        var id = CryptoFixtureSupport.Hex(DocumentId);
        pdf.Append("trailer\n<< /Size " + size + " /Root 1 0 R /Encrypt 5 0 R /ID [" + id + " " + id + "]"
                + (infoBody is not null ? " /Info 6 0 R" : "") + " >>\n")
            .Append("startxref\n" + xrefOffset + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static string ContentText(DocumentReader reader)
    {
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
        var stream = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        return Encoding.Latin1.GetString(reader.DecodeStream(stream));
    }


    [Fact]
    public void IdentityStmFAndStrF_LeaveDataUntouched()
    {
        const int permissions = -3904;
        var owner = CryptoFixtureSupport.R4OwnerEntry();
        var fileKey = CryptoFixtureSupport.R4FileKey(owner, permissions, DocumentId);
        var user = CryptoFixtureSupport.R4UserEntry(fileKey, DocumentId);

        var encrypt = "<< /Filter /Standard /V 4 /R 4 /Length 128"
            + " /CF << /StdCF << /CFM /AESV2 /AuthEvent /DocOpen /Length 16 >> >>"
            + " /StmF /Identity /StrF /Identity"
            + " /O " + CryptoFixtureSupport.Hex(owner)
            + " /U " + CryptoFixtureSupport.Hex(user)
            + " /P " + permissions + " >>";

        var text = "BT /F1 12 Tf 72 720 Td (identity-marker) Tj ET";
        var content = Encoding.Latin1.GetBytes(text.PadRight((text.Length + 15) / 16 * 16));

        var bytes = EncryptedFile(encrypt, content, "<< /Title (Identity plain title) >>");
        var reader = DocumentReader.Parse(bytes, "");

        Assert.True(reader.IsEncrypted);
        Assert.Contains("(identity-marker) Tj", ContentText(reader));

        var info = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]));
        Assert.Equal("Identity plain title", Assert.IsType<StringObject>(info["Title"]).Value);
    }


    [Fact]
    public void NamedCryptFilter_AesV2StreamsDecrypt()
    {
        const int permissions = -3904;
        var owner = CryptoFixtureSupport.R4OwnerEntry();
        var fileKey = CryptoFixtureSupport.R4FileKey(owner, permissions, DocumentId);
        var user = CryptoFixtureSupport.R4UserEntry(fileKey, DocumentId);

        var encrypt = "<< /Filter /Standard /V 4 /R 4 /Length 128"
            + " /CF << /AesCF << /CFM /AESV2 /AuthEvent /DocOpen /Length 16 >> >>"
            + " /StmF /AesCF /StrF /AesCF"
            + " /O " + CryptoFixtureSupport.Hex(owner)
            + " /U " + CryptoFixtureSupport.Hex(user)
            + " /P " + permissions + " >>";

        var plain = Encoding.Latin1.GetBytes("BT /F1 12 Tf 72 720 Td (named-cf-marker) Tj ET");
        var objectKey = CryptoFixtureSupport.AesV2ObjectKey(fileKey, 4, 0);
        var content = CryptoFixtureSupport.AesEncrypt(objectKey, CryptoFixtureSupport.FixedBytes(16, 77), plain);

        var reader = DocumentReader.Parse(EncryptedFile(encrypt, content, null), "");

        Assert.True(reader.IsEncrypted);
        Assert.Contains("(named-cf-marker) Tj", ContentText(reader));
    }


    [Fact]
    public void Revision6_NonAsciiPassword_IsUtf8()
    {
        const string password = "m\u00FCnchen";
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var validationSalt = CryptoFixtureSupport.FixedBytes(8, 11);
        var keySalt = CryptoFixtureSupport.FixedBytes(8, 23);
        var fileKey = CryptoFixtureSupport.FixedBytes(32, 41);

        var userHash = CryptoFixtureSupport.Hash2B(passwordBytes, validationSalt, []);
        var user = new byte[48];
        Array.Copy(userHash, user, 32);
        Array.Copy(validationSalt, 0, user, 32, 8);
        Array.Copy(keySalt, 0, user, 40, 8);

        var intermediate = CryptoFixtureSupport.Hash2B(passwordBytes, keySalt, []);
        var userEncrypted = CryptoFixtureSupport.AesCbcNoPadding(intermediate, new byte[16], fileKey);

        var encrypt = "<< /Filter /Standard /V 5 /R 6 /Length 256"
            + " /CF << /StdCF << /CFM /AESV3 /AuthEvent /DocOpen /Length 32 >> >>"
            + " /StmF /StdCF /StrF /StdCF"
            + " /O " + CryptoFixtureSupport.Hex(CryptoFixtureSupport.FixedBytes(48, 2))
            + " /OE " + CryptoFixtureSupport.Hex(new byte[32])
            + " /U " + CryptoFixtureSupport.Hex(user)
            + " /UE " + CryptoFixtureSupport.Hex(userEncrypted)
            + " /P -4 >>";

        var plain = Encoding.Latin1.GetBytes("BT /F1 12 Tf 72 720 Td (utf8-password-marker) Tj ET");
        var content = CryptoFixtureSupport.AesEncrypt(fileKey, CryptoFixtureSupport.FixedBytes(16, 55), plain);

        var reader = DocumentReader.Parse(EncryptedFile(encrypt, content, null), password);

        Assert.True(reader.IsEncrypted);
        Assert.Contains("(utf8-password-marker) Tj", ContentText(reader));
    }


    private static byte[] BreakStartxref(byte[] bytes)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var at = text.LastIndexOf("startxref", StringComparison.Ordinal);
        Assert.True(at >= 0);
        return Encoding.Latin1.GetBytes(text[..at] + "startxref\n1\n%%EOF\n");
    }

    [Fact]
    public void Repair_PreservesInfoFromRealTrailer()
    {
        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Title (Repaired title) >>\nendobj\n");
        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 5\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(4)))
            .Append("trailer\n<< /Size 5 /Root 1 0 R /Info 4 0 R >>\n")
            .Append("startxref\n" + xrefOffset + "\n%%EOF\n");

        var reader = DocumentReader.Parse(BreakStartxref(pdf.ToArray()));

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);

        Assert.True(reader.Trailer.TryGetValue("Info", out var infoObject) && infoObject is not null,
            "Repaired trailer lost /Info.");
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(infoObject!));
        Assert.Equal("Repaired title", Assert.IsType<StringObject>(info["Title"]).Value);
    }

    [Fact]
    public void Repair_PreservesEncryptAndId_DecryptionStillWorks()
    {
        var bytes = BreakStartxref(PdfTestResources.ReadAllBytes("Documents/encrypted-rc4-40.pdf"));

        var reader = DocumentReader.Parse(bytes, "");

        Assert.True(reader.IsEncrypted, "Repair dropped /Encrypt.");
        Assert.Contains("Hello encrypted world!", ContentText(reader));
    }
}
