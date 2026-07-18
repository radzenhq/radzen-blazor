#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class EncryptedR5Tests
{
    private const string UserPassword = "user5";
    private const string OwnerPassword = "owner5";

    private static readonly byte[] DocumentId = CryptoFixtureSupport.FixedBytes(16, 5);
    private static readonly byte[] FileKey = CryptoFixtureSupport.FixedBytes(32, 7);

    private static byte[] Concat(params byte[][] parts)
    {
        var length = 0;
        foreach (var part in parts)
        {
            length += part.Length;
        }

        var result = new byte[length];
        var pos = 0;
        foreach (var part in parts)
        {
            Array.Copy(part, 0, result, pos, part.Length);
            pos += part.Length;
        }

        return result;
    }

    private static byte[] R5File(string userPassword = UserPassword)
    {
        var zeroIv = new byte[16];
        var userPw = Encoding.UTF8.GetBytes(userPassword);
        var ownerPw = Encoding.UTF8.GetBytes(OwnerPassword);

        var userValidationSalt = CryptoFixtureSupport.FixedBytes(8, 11);
        var userKeySalt = CryptoFixtureSupport.FixedBytes(8, 22);
        var u = Concat(SHA256.HashData(Concat(userPw, userValidationSalt)), userValidationSalt, userKeySalt);
        var ue = CryptoFixtureSupport.AesCbcNoPadding(
            SHA256.HashData(Concat(userPw, userKeySalt)), zeroIv, FileKey);

        var ownerValidationSalt = CryptoFixtureSupport.FixedBytes(8, 33);
        var ownerKeySalt = CryptoFixtureSupport.FixedBytes(8, 44);
        var o = Concat(SHA256.HashData(Concat(ownerPw, ownerValidationSalt, u)), ownerValidationSalt, ownerKeySalt);
        var oe = CryptoFixtureSupport.AesCbcNoPadding(
            SHA256.HashData(Concat(ownerPw, ownerKeySalt, u)), zeroIv, FileKey);

        var encrypt = "<< /Filter /Standard /V 5 /R 5 /Length 256"
            + " /CF << /StdCF << /CFM /AESV3 /AuthEvent /DocOpen /Length 32 >> >>"
            + " /StmF /StdCF /StrF /StdCF"
            + " /O " + CryptoFixtureSupport.Hex(o)
            + " /U " + CryptoFixtureSupport.Hex(u)
            + " /OE " + CryptoFixtureSupport.Hex(oe)
            + " /UE " + CryptoFixtureSupport.Hex(ue)
            + " /P -3904 >>";

        var plain = Encoding.Latin1.GetBytes("BT /F1 12 Tf 72 720 Td (r5-marker) Tj ET");
        var content = CryptoFixtureSupport.AesEncrypt(FileKey, CryptoFixtureSupport.FixedBytes(16, 55), plain);

        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R "
            + "/Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n")
            .Append(content)
            .Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n" + encrypt + "\nendobj\n");

        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 6; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        var id = CryptoFixtureSupport.Hex(DocumentId);
        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R /Encrypt 5 0 R /ID [" + id + " " + id + "] >>\n")
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
    public void R5UserPassword_Opens_AndDecryptsContent()
    {
        var reader = DocumentReader.Parse(R5File(), UserPassword);

        Assert.True(reader.IsEncrypted);
        Assert.Contains("(r5-marker) Tj", ContentText(reader), StringComparison.Ordinal);
    }

    [Fact]
    public void R5OwnerPassword_Opens_AndDecryptsContent()
    {
        var reader = DocumentReader.Parse(R5File(), OwnerPassword);

        Assert.True(reader.IsEncrypted);
        Assert.Contains("(r5-marker) Tj", ContentText(reader), StringComparison.Ordinal);
    }

    [Fact]
    public void R5PasswordUsesUtf8WithoutSaslprep()
    {
        const string Password = "sec\u00adret";

        var reader = DocumentReader.Parse(R5File(Password), Password);

        Assert.True(reader.IsEncrypted);
    }
}
