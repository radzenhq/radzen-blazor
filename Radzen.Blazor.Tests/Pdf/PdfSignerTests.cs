#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Signing;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// End-to-end contract of PdfSigner: the original file stays a byte-for-byte
// prefix, /ByteRange covers everything except the <...> /Contents token
// (angle brackets included), and the embedded detached CMS verifies against
// the covered bytes. The real-CMS tests drive Microsoft's SignedCms directly
// (the test host is a desktop runtime; only the library itself must stay
// WASM-safe, hence the ISigner delegation boundary).
public class PdfSignerTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    private static byte[] BuildPdf()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Signed document body", BuildTestSupport.Latin);
        return builder.ToArray();
    }

    private static SignatureOptions Options() => new()
    {
        Reason = "Approval",
        Location = "Sofia",
        ContactInfo = "info@radzen.com",
        SignerName = "Radzen Test Signer",
        SigningTime = FixedTime,
    };

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Radzen PDF Signing Tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2036, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class DelegateSigner(Func<byte[], byte[]> sign) : ISigner
    {
        public byte[]? LastContent { get; private set; }

        public byte[]? LastSignature { get; private set; }

        public byte[] Sign(SignedContent content)
        {
            LastContent = content.ToArray();
            LastSignature = sign(LastContent);
            return LastSignature;
        }
    }

    private static DelegateSigner CmsSigner(X509Certificate2 certificate)
        => new(content => Pkcs.SignDetached(certificate, content));

    // Microsoft's SignedCms runs on the desktop test host; only the library
    // itself must stay WASM-safe, which is what the ISigner boundary enforces.
    private static class Pkcs
    {
        public static byte[] SignDetached(X509Certificate2 certificate, byte[] content)
        {
            var cms = new SignedCms(new ContentInfo(content), detached: true);
            cms.ComputeSignature(new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate));
            return cms.Encode();
        }

        // Throws CryptographicException when the signature does not verify.
        public static void CheckSignature(byte[] content, byte[] der)
        {
            var cms = new SignedCms(new ContentInfo(content), detached: true);
            cms.Decode(der);
            cms.CheckSignature(true);
        }
    }

    private static DictionaryObject Catalog(DocumentReader reader)
        => (DictionaryObject)reader.Resolve(reader.Trailer["Root"]);

    private static DictionaryObject AcroForm(DocumentReader reader)
        => (DictionaryObject)reader.Resolve(Catalog(reader)["AcroForm"]);

    private static DictionaryObject Field(DocumentReader reader, int index)
    {
        var fields = (ArrayObject)reader.Resolve(AcroForm(reader)["Fields"]);
        return (DictionaryObject)reader.Resolve(fields[index]);
    }

    private static DictionaryObject SignatureValue(DocumentReader reader, int index)
        => (DictionaryObject)reader.Resolve(Field(reader, index)["V"]);

    private static (int GapStart, int GapEnd, int Tail) ByteRange(DocumentReader reader, DictionaryObject signature)
    {
        var range = (ArrayObject)reader.Resolve(signature["ByteRange"]);
        Assert.Equal(4, range.Count);
        Assert.Equal(0, ((NumberObject)range[0]).IntValue);
        return (((NumberObject)range[1]).IntValue, ((NumberObject)range[2]).IntValue, ((NumberObject)range[3]).IntValue);
    }

    private static byte[] CoveredContent(byte[] bytes, int gapStart, int gapEnd, int tail)
    {
        var content = new byte[gapStart + tail];
        Array.Copy(bytes, 0, content, 0, gapStart);
        Array.Copy(bytes, gapEnd, content, gapStart, tail);
        return content;
    }

    private static byte[] DecodeContentsHex(byte[] bytes, int gapStart, int gapEnd)
    {
        Assert.Equal((byte)'<', bytes[gapStart]);
        Assert.Equal((byte)'>', bytes[gapEnd - 1]);
        var digits = gapEnd - gapStart - 2;
        var raw = new byte[digits / 2];
        for (var i = 0; i < raw.Length; i++)
        {
            var text = Encoding.ASCII.GetString(bytes, gapStart + 1 + 2 * i, 2);
            raw[i] = Convert.ToByte(text, 16);
        }

        return raw;
    }

    // The DER blob's own header determines where the CMS ends and the zero
    // padding of the reserved /Contents area begins.
    private static int DerTotalLength(byte[] der)
    {
        Assert.Equal(0x30, der[0]);
        var first = der[1];
        if (first < 0x80)
        {
            return 2 + first;
        }

        var count = first & 0x7F;
        var length = 0;
        for (var i = 0; i < count; i++)
        {
            length = length * 256 + der[2 + i];
        }

        return 2 + count + length;
    }

    private static void VerifySignature(byte[] fileBytes, DocumentReader reader, int fieldIndex)
    {
        var signature = SignatureValue(reader, fieldIndex);
        var (gapStart, gapEnd, tail) = ByteRange(reader, signature);
        var content = CoveredContent(fileBytes, gapStart, gapEnd, tail);
        var padded = DecodeContentsHex(fileBytes, gapStart, gapEnd);
        var der = padded[..DerTotalLength(padded)];
        Pkcs.CheckSignature(content, der);
    }

    // A one-page PDF whose page dictionary carries a string value that embeds a
    // decoy "/Contents <0...0>" and "/ByteRange [0 0 0 0 ...]" matching the exact
    // placeholder shapes the signer scans for. The page is copied verbatim into
    // the incremental update, so a naive whole-appended-region scan would lock
    // onto these decoys instead of the real signature dictionary.
    private static byte[] HostilePdf()
    {
        var zeros = new string('0', 16384 * 2);
        var byteRange = "[0 0 0 0" + new string(' ', 40 - "0 0 0 0".Length) + "]";
        var decoy = "/Contents <" + zeros + "> /ByteRange " + byteRange;
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Decoy (" + decoy + ") >>\nendobj\n");
        var count = 4;
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void Sign_HostileContentsDecoyInInput_DoesNotMisscopeSignature()
    {
        var original = HostilePdf();
        using var certificate = CreateCertificate();

        var signed = PdfSigner.Sign(original, Options(), CmsSigner(certificate));

        Assert.True(signed.AsSpan(0, original.Length).SequenceEqual(original));

        // The signature must be scoped to the REAL /Contents in the sig dict, so
        // the CMS verifies over the declared /ByteRange. Under the pre-fix scan
        // this would lock onto the decoy string and fail here.
        var reader = DocumentReader.Parse(signed);
        VerifySignature(signed, reader, 0);

        // The decoy string in the copied page must remain untouched (all zeros).
        var decoyStart = IndexOfAscii(signed, "/Decoy (/Contents <");
        Assert.True(decoyStart >= 0);
        var hexStart = decoyStart + "/Decoy (/Contents <".Length;
        for (var i = 0; i < 32768; i++)
        {
            Assert.Equal((byte)'0', signed[hexStart + i]);
        }
    }

    private static int IndexOfAscii(byte[] bytes, string pattern)
    {
        var needle = Encoding.ASCII.GetBytes(pattern);
        for (var i = 0; i <= bytes.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (bytes[i + j] != needle[j]) { match = false; break; }
            }

            if (match) { return i; }
        }

        return -1;
    }

    [Fact]
    public void Sign_AddsSignatureFieldAsIncrementalUpdate()
    {
        var original = BuildPdf();
        using var certificate = CreateCertificate();

        var signed = PdfSigner.Sign(original, Options(), CmsSigner(certificate));

        Assert.True(signed.Length > original.Length);
        Assert.True(signed.AsSpan(0, original.Length).SequenceEqual(original));

        var reader = DocumentReader.Parse(signed);
        var acroForm = AcroForm(reader);
        Assert.Equal(3, ((NumberObject)reader.Resolve(acroForm["SigFlags"])).IntValue);
        Assert.Equal(1, ((ArrayObject)reader.Resolve(acroForm["Fields"])).Count);

        var field = Field(reader, 0);
        Assert.Equal("Sig", ((NameObject)field["FT"]).Value);
        Assert.Equal("Signature1", ((StringObject)field["T"]).Value);
        Assert.Equal("Widget", ((NameObject)field["Subtype"]).Value);
        Assert.Equal(132, ((NumberObject)field["F"]).IntValue);

        var signature = SignatureValue(reader, 0);
        Assert.Equal("Sig", ((NameObject)signature["Type"]).Value);
        Assert.Equal("Adobe.PPKLite", ((NameObject)signature["Filter"]).Value);
        Assert.Equal("adbe.pkcs7.detached", ((NameObject)signature["SubFilter"]).Value);
        Assert.True(signature.ContainsKey("ByteRange"));
        Assert.True(signature.ContainsKey("Contents"));
        Assert.Equal("Approval", ((StringObject)signature["Reason"]).Value);
        Assert.Equal("Sofia", ((StringObject)signature["Location"]).Value);
        Assert.Equal("info@radzen.com", ((StringObject)signature["ContactInfo"]).Value);
        Assert.Equal("Radzen Test Signer", ((StringObject)signature["Name"]).Value);
        Assert.Equal("D:20260315120000+00'00'", ((StringObject)signature["M"]).Value);

        var page = (DictionaryObject)reader.Resolve(
            ((ArrayObject)reader.Resolve(((DictionaryObject)reader.Resolve(Catalog(reader)["Pages"]))["Kids"]))[0]);
        var annots = (ArrayObject)reader.Resolve(page["Annots"]);
        var fieldNumber = ((ReferenceObject)((ArrayObject)reader.Resolve(acroForm["Fields"]))[0]).ObjectNumber;
        Assert.Contains(annots, annot => annot is ReferenceObject r && r.ObjectNumber == fieldNumber);
    }

    [Fact]
    public void Sign_ByteRangeCoversFileExceptContents()
    {
        var original = BuildPdf();
        using var certificate = CreateCertificate();
        var signer = CmsSigner(certificate);
        var options = Options();

        var signed = PdfSigner.Sign(original, options, signer);
        var reader = DocumentReader.Parse(signed);
        var (gapStart, gapEnd, tail) = ByteRange(reader, SignatureValue(reader, 0));

        Assert.Equal(signed.Length, gapEnd + tail);
        Assert.Equal(options.SignatureMaxSizeBytes * 2 + 2, gapEnd - gapStart);
        Assert.Equal((byte)'<', signed[gapStart]);
        Assert.Equal((byte)'>', signed[gapEnd - 1]);

        var content = CoveredContent(signed, gapStart, gapEnd, tail);
        Assert.True(content.SequenceEqual(signer.LastContent!));

        var cms = signer.LastSignature!;
        var decoded = DecodeContentsHex(signed, gapStart, gapEnd);
        Assert.True(decoded[..cms.Length].SequenceEqual(cms));
        Assert.All(decoded[cms.Length..], b => Assert.Equal(0, b));
    }

    [Fact]
    public void Sign_ProducesVerifiableCms()
    {
        var original = BuildPdf();
        using var certificate = CreateCertificate();

        var signed = PdfSigner.Sign(original, Options(), CmsSigner(certificate));
        var reader = DocumentReader.Parse(signed);

        // Throws CryptographicException unless SignedCms.CheckSignature(true) succeeds.
        VerifySignature(signed, reader, 0);
    }

    [Fact]
    public void Sign_SecondSignaturePreservesAndDoesNotInvalidateFirst()
    {
        var original = BuildPdf();
        using var certificate = CreateCertificate();

        var once = PdfSigner.Sign(original, Options(), CmsSigner(certificate));
        var optionsTwice = Options();
        optionsTwice.SigningTime = FixedTime.AddDays(1);
        var twice = PdfSigner.Sign(once, optionsTwice, CmsSigner(certificate));

        Assert.True(twice.AsSpan(0, once.Length).SequenceEqual(once));

        var reader = DocumentReader.Parse(twice);
        var fields = (ArrayObject)reader.Resolve(AcroForm(reader)["Fields"]);
        Assert.Equal(2, fields.Count);
        Assert.Equal("Signature1", ((StringObject)Field(reader, 0)["T"]).Value);
        Assert.Equal("Signature2", ((StringObject)Field(reader, 1)["T"]).Value);

        VerifySignature(twice, reader, 0);
        VerifySignature(twice, reader, 1);
    }

    [Fact]
    public void Sign_ThrowsWhenSignatureExceedsReservedSize()
    {
        var original = BuildPdf();
        var options = Options();
        options.SignatureMaxSizeBytes = 64;
        var signer = new DelegateSigner(_ => new byte[65]);

        var exception = Assert.Throws<InvalidOperationException>(() => PdfSigner.Sign(original, options, signer));
        Assert.Contains("SignatureMaxSizeBytes", exception.Message);
    }

    [Fact]
    public void Sign_IsDeterministic()
    {
        var original = BuildPdf();
        var cms = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

        var first = PdfSigner.Sign(original, Options(), new DelegateSigner(_ => cms));
        var second = PdfSigner.Sign(original, Options(), new DelegateSigner(_ => cms));

        Assert.True(first.SequenceEqual(second));
    }
}
