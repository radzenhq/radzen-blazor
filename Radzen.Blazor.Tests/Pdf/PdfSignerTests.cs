#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Signing;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfSignerTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    private static byte[] BuildPdf()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Signed document body", BuildTestSupport.Latin);
        return new DocumentRenderer().ToArray(document);
    }

    private static SignatureOptions Options() => new()
    {
        Reason = "Approval",
        Location = "Sofia",
        ContactInfo = "info@radzen.com",
        SignerName = "Radzen Test Signer",
        SigningTime = FixedTime,
    };

    private static TestSigningIdentity CreateCertificate() => TestSigningIdentity.Create();

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

    private static DelegateSigner CmsSigner(TestSigningIdentity certificate)
        => new(content => Pkcs.SignDetached(certificate, content));

    private static class Pkcs
    {
        public static byte[] SignDetached(TestSigningIdentity certificate, byte[] content)
        {
            var cms = new SignedCms(new ContentInfo(content), detached: true);
            cms.ComputeSignature(certificate.CmsSigner());
            return cms.Encode();
        }

        public static void CheckSignature(byte[] content, byte[] der)
        {
            var cms = new SignedCms(new ContentInfo(content), detached: true);
            cms.Decode(der);
            cms.CheckSignature(true);
        }
    }

    private static string Emitted(byte[] bytes) => Encoding.Latin1.GetString(bytes);

    private static string SignatureOf(string emission, string fieldName)
    {
        var field = Line(emission, $"/T ({fieldName})");
        var value = Shaped($"field {fieldName}", @"/V (\d+) 0 R", field);
        return IndirectObject(emission, value.Groups[1].Value);
    }

    private static (int GapStart, int GapEnd, int Tail) ByteRange(string emission, string fieldName)
    {
        var match = Shaped(
            $"signature of {fieldName}",
            @"/ByteRange \[0 (\d+) (\d+) (\d+) *\]",
            SignatureOf(emission, fieldName));
        return (
            int.Parse(match.Groups[1].Value),
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value));
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

    private static void VerifySignature(byte[] fileBytes, string fieldName)
    {
        var (gapStart, gapEnd, tail) = ByteRange(Emitted(fileBytes), fieldName);
        var content = CoveredContent(fileBytes, gapStart, gapEnd, tail);
        var padded = DecodeContentsHex(fileBytes, gapStart, gapEnd);
        var der = padded[..DerTotalLength(padded)];
        Pkcs.CheckSignature(content, der);
    }

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

    private static byte[] DeepPageTree(int depth)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        var leaf = depth + 2;
        for (var number = 2; number < leaf; number++)
        {
            var parent = number == 2 ? "" : " /Parent " + (number - 1) + " 0 R";
            pdf.Object(number,
                number + " 0 obj\n<< /Type /Pages" + parent + " /Kids [" + (number + 1) + " 0 R] /Count 1 >>\nendobj\n");
        }

        pdf.Object(leaf,
            leaf + " 0 obj\n<< /Type /Page /Parent " + (leaf - 1) + " 0 R /MediaBox [0 0 612 792] >>\nendobj\n");

        var count = leaf + 1;
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] DirectPageKid()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>] /Count 1 >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 3\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append("trailer\n<< /Size 3 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] CyclicPageTree()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Pages /Kids [2 0 R] /Count 1 >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 4\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < 4; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void Sign_DirectPageDictionaryInKids_LoadsAndSigns()
    {
        var original = DirectPageKid();

        Assert.Single(PortableDocument.LoadFromStream(new MemoryStream(original)).Pages);

        var signed = PdfSigner.Sign(original, Options(), new DelegateSigner(_ => [1, 2, 3]));
        var emission = Emitted(signed);

        var pages = Shaped("rewritten page tree", @"/Type /Pages /Kids \[(\d+) 0 R\] /Count 1", emission);
        References("signed page", "Annots", 1, IndirectObject(emission, pages.Groups[1].Value));
    }

    [Fact]
    public void Sign_CyclicPageTree_Throws()
    {
        var error = Assert.Throws<DocumentParseException>(
            () => PdfSigner.Sign(CyclicPageTree(), Options(), new DelegateSigner(_ => [1])));

        Assert.Contains("Cyclic page tree", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sign_PageTreeDeeperThanSixtyFive_MatchesTheLoaderPolicy()
    {
        var original = DeepPageTree(70);
        using var certificate = CreateCertificate();

        var loaded = PortableDocument.LoadFromStream(new MemoryStream(original));
        Assert.Single(loaded.Pages);

        var signed = PdfSigner.Sign(original, Options(), CmsSigner(certificate));

        Assert.True(signed.AsSpan(0, original.Length).SequenceEqual(original));
    }

    [Fact]
    public void Sign_PageTreeDeeperThanTheReaderLimit_Throws()
    {
        var original = DeepPageTree(ReaderLimits.Default.MaxPageTreeDepth + 50);
        using var certificate = CreateCertificate();

        var error = Assert.Throws<DocumentParseException>(
            () => PdfSigner.Sign(original, Options(), CmsSigner(certificate)));
        Assert.Contains("page tree depth", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sign_HostileContentsDecoyInInput_DoesNotMisscopeSignature()
    {
        var original = HostilePdf();
        using var certificate = CreateCertificate();

        var signed = PdfSigner.Sign(original, Options(), CmsSigner(certificate));

        Assert.True(signed.AsSpan(0, original.Length).SequenceEqual(original));

        VerifySignature(signed, "Signature1");

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

        var emission = Emitted(signed);
        var acroForm = Line(emission, "/SigFlags");
        Assert.Equal(3, NumberIn(acroForm, "SigFlags"));

        var fields = References("AcroForm", "Fields", 1, acroForm);
        var field = IndirectObject(emission, fields[0]);
        Carries("signature field", "/FT /Sig", field);
        Carries("signature field", "/T (Signature1)", field);
        Carries("signature field", "/Subtype /Widget", field);
        Assert.Equal(132, NumberIn(field, "F"));

        var signature = SignatureOf(emission, "Signature1");
        Carries("signature", "/Type /Sig", signature);
        Carries("signature", "/Filter /Adobe.PPKLite", signature);
        Carries("signature", "/SubFilter /adbe.pkcs7.detached", signature);
        Carries("signature", "/ByteRange [", signature);
        Carries("signature", "/Contents <", signature);
        Carries("signature", "/Reason (Approval)", signature);
        Carries("signature", "/Location (Sofia)", signature);
        Carries("signature", "/ContactInfo (info@radzen.com)", signature);
        Carries("signature", "/Name (Radzen Test Signer)", signature);
        Carries("signature", "/M (D:20260315120000+00'00')", signature);

        var annots = References("signed page", "Annots", 1, Line(emission, "/Annots ["));
        Assert.Equal(fields[0], annots[0]);
    }

    [Fact]
    public void Sign_ByteRangeCoversFileExceptContents()
    {
        var original = BuildPdf();
        using var certificate = CreateCertificate();
        var signer = CmsSigner(certificate);
        var options = Options();

        var signed = PdfSigner.Sign(original, options, signer);
        var (gapStart, gapEnd, tail) = ByteRange(Emitted(signed), "Signature1");

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

        VerifySignature(signed, "Signature1");
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

        var emission = Emitted(twice);
        var fields = References("AcroForm", "Fields", 2, emission);
        Carries("first signature field", "/T (Signature1)", IndirectObject(emission, fields[0]));
        Carries("second signature field", "/T (Signature2)", IndirectObject(emission, fields[1]));

        VerifySignature(twice, "Signature1");
        VerifySignature(twice, "Signature2");
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

    private sealed class UnusedSigner : ISigner
    {
        public byte[] Sign(SignedContent content) => throw new InvalidOperationException("must not be called");
    }

    private sealed class UnusedTimestampProvider : ITimestampProvider
    {
        public byte[] GetTimestampToken(ReadOnlySpan<byte> hash) => throw new InvalidOperationException("must not be called");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(16 * 1024 * 1024 + 1)]
    public void Sign_SignatureMaxSizeBytesOutOfRange_Throws(int size)
    {
        var options = Options();
        options.SignatureMaxSizeBytes = size;

        var e = Assert.Throws<ArgumentOutOfRangeException>(
            () => PdfSigner.Sign(BuildPdf(), options, new UnusedSigner()));

        Assert.Contains("must be between 1 and 16777216", e.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(16 * 1024 * 1024 + 1)]
    public void Timestamp_ReservedBytesOutOfRange_Throws(int reservedBytes)
    {
        var e = Assert.Throws<ArgumentOutOfRangeException>(
            () => PdfTimestamper.Timestamp(BuildPdf(), new UnusedTimestampProvider(), reservedBytes));

        Assert.Contains("must be between 1 and 16777216", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sign_DefaultSubFilter_IsTheDetachedPkcs7Name()
    {
        var signed = PdfSigner.Sign(BuildPdf(), Options(), new DelegateSigner(_ => [1, 2, 3]));

        Carries("signature", "/SubFilter /adbe.pkcs7.detached", SignatureOf(Emitted(signed), "Signature1"));
    }

    [Theory]
    [InlineData("adbe.pkcs7.sha1")]
    [InlineData("ETSI.CAdES.detached")]
    public void Sign_CustomSubFilter_IsWrittenVerbatim(string subFilter)
    {
        var options = Options();
        options.SubFilter = subFilter;

        var signed = PdfSigner.Sign(BuildPdf(), options, new DelegateSigner(_ => [1, 2, 3]));

        Carries("signature", $"/SubFilter /{subFilter}", SignatureOf(Emitted(signed), "Signature1"));
    }

    [Fact]
    public void Sign_EmptySubFilter_Throws()
    {
        var options = Options();
        options.SubFilter = string.Empty;

        var error = Assert.Throws<ArgumentException>(
            () => PdfSigner.Sign(BuildPdf(), options, new DelegateSigner(_ => [1, 2, 3])));

        Assert.Contains("SubFilter", error.Message, StringComparison.Ordinal);
    }
}
