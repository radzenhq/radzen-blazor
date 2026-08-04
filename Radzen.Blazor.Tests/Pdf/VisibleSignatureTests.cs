#nullable enable
using System;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Signing;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class VisibleSignatureTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    private static byte[] BuildPdf(int pages = 1)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Signed document body", BuildTestSupport.Latin);
        for (var i = 1; i < pages; i++)
        {
            section.Blocks.AddPageBreak();
            BuildTestSupport.AddText(section, "Another page " + i, BuildTestSupport.Latin);
        }

        return new DocumentRenderer().ToArray(document);
    }

    private static SignatureOptions Options(SignatureAppearance? appearance = null) => new()
    {
        Reason = "Approval",
        Location = "Sofia",
        SignerName = "Radzen Test Signer",
        SigningTime = FixedTime,
        Appearance = appearance,
    };

    private static TestSigningIdentity CreateCertificate() => TestSigningIdentity.Create();

    private sealed class CmsSigner2(TestSigningIdentity certificate) : ISigner
    {
        public byte[] Sign(SignedContent content)
        {
            var cms = new SignedCms(new ContentInfo(content.ToArray()), detached: true);
            cms.ComputeSignature(certificate.CmsSigner());
            return cms.Encode();
        }
    }

    private sealed class FixedSigner(byte[] blob) : ISigner
    {
        public byte[] Sign(SignedContent content) => blob;
    }

    private static string Emission(byte[] pdf) => Encoding.Latin1.GetString(pdf);

    private static string SignatureFieldNumber(string emission)
        => References("AcroForm", "Fields", 1, Line(emission, "/SigFlags"))[0];

    private static string SignatureField(string emission)
        => IndirectObject(emission, SignatureFieldNumber(emission));

    private static void VerifyCms(byte[] signed)
    {
        var range = Shaped(
            "signature /ByteRange",
            @"/ByteRange \[(\d+) (\d+) (\d+) (\d+) *\]",
            Emission(signed));
        var gapStart = int.Parse(range.Groups[2].Value);
        var gapEnd = int.Parse(range.Groups[3].Value);
        var tail = int.Parse(range.Groups[4].Value);

        var content = new byte[gapStart + tail];
        Array.Copy(signed, 0, content, 0, gapStart);
        Array.Copy(signed, gapEnd, content, gapStart, tail);

        var digits = gapEnd - gapStart - 2;
        var padded = new byte[digits / 2];
        for (var i = 0; i < padded.Length; i++)
        {
            padded[i] = Convert.ToByte(Encoding.ASCII.GetString(signed, gapStart + 1 + 2 * i, 2), 16);
        }

        var der = padded[..DerLength(padded)];
        var cms = new SignedCms(new ContentInfo(content), detached: true);
        cms.Decode(der);
        cms.CheckSignature(true);
    }

    private static int DerLength(byte[] der)
    {
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

    [Fact]
    public void DefaultSignatureKeepsInvisibleZeroRectAndNoAppearance()
    {
        var emission = Emission(PdfSigner.Sign(BuildPdf(), Options(), new FixedSigner(new byte[100])));
        var field = SignatureField(emission);

        Carries("signature field", "/Rect [0 0 0 0]", field);
        Lacks("signature field", "/AP", field);
    }

    [Fact]
    public void DefaultInvisibleSignatureIsDeterministic()
    {
        var original = BuildPdf();
        var blob = Enumerable.Range(0, 80).Select(i => (byte)i).ToArray();

        var first = PdfSigner.Sign(original, Options(), new FixedSigner(blob));
        var second = PdfSigner.Sign(original, Options(), new FixedSigner(blob));

        Assert.Equal(first, second);
    }

    [Fact]
    public void VisibleSignatureSetsRectAndAppearanceStream()
    {
        var appearance = new SignatureAppearance { X = 72, Y = 700, Width = 200, Height = 60 };
        var emission = Emission(PdfSigner.Sign(BuildPdf(), Options(appearance), new FixedSigner(new byte[100])));
        var field = SignatureField(emission);

        Carries("signature field", "/Rect [72 700 272 760]", field);

        var normal = Shaped("signature field /AP", @"/AP << /N (\d+) 0 R >>", field);
        var painted = IndirectObject(emission, normal.Groups[1].Value);

        Carries("signature appearance", "Radzen Test Signer", painted);
        Carries("signature appearance", "Approval", painted);
        Carries("signature appearance", "2026-03-15", painted);
    }

    [Fact]
    public void VisibleSignatureStillVerifies()
    {
        var original = BuildPdf();
        using var certificate = CreateCertificate();
        var appearance = new SignatureAppearance { X = 72, Y = 700, Width = 200, Height = 60 };

        var signed = PdfSigner.Sign(original, Options(appearance), new CmsSigner2(certificate));

        Assert.True(signed.AsSpan(0, original.Length).SequenceEqual(original));
        VerifyCms(signed);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(-20, 60)]
    [InlineData(200, 0)]
    [InlineData(200, -60)]
    [InlineData(double.NaN, 60)]
    [InlineData(200, double.PositiveInfinity)]
    public void VisibleSignatureRejectsNonPositiveOrNonFiniteDimensions(double width, double height)
    {
        var original = BuildPdf();
        var appearance = new SignatureAppearance { X = 72, Y = 700, Width = width, Height = height };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => PdfSigner.Sign(original, Options(appearance), new FixedSigner(new byte[100])));
    }

    [Theory]
    [InlineData(double.NaN, 700)]
    [InlineData(72, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 700)]
    public void VisibleSignatureRejectsNonFinitePosition(double x, double y)
    {
        var original = BuildPdf();
        var appearance = new SignatureAppearance { X = x, Y = y, Width = 200, Height = 60 };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => PdfSigner.Sign(original, Options(appearance), new FixedSigner(new byte[100])));
    }

    [Fact]
    public void VisibleSignatureLandsOnRequestedPage()
    {
        var appearance = new SignatureAppearance { PageIndex = 1, X = 72, Y = 700, Width = 200, Height = 60 };
        var emission = Emission(
            PdfSigner.Sign(BuildPdf(pages: 2), Options(appearance), new FixedSigner(new byte[100])));

        var pages = References("page tree", "Kids", 2, Line(emission, "/Type /Pages"));
        var annotated = Shaped(
            "annotated page",
            $@"\n(\d+) 0 obj\n<<[^\n]*/Annots \[{SignatureFieldNumber(emission)} 0 R\]",
            emission);

        Assert.Equal(pages[1], annotated.Groups[1].Value);
        Lacks($"page {pages[0]} 0 R", "/Annots", IndirectObject(emission, pages[0]));
    }
}
