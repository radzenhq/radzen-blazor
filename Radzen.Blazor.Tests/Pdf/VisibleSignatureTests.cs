#nullable enable
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Radzen.Documents.Pdf.Signing;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A signature can be made visible: a non-zero /Rect on a chosen page plus a
// generated /AP /N appearance stream showing the signer name, reason and date.
// Omitting SignatureOptions.Appearance keeps the historic invisible [0 0 0 0]
// widget with no appearance so unchanged callers stay byte-stable, and either
// way the embedded detached CMS still verifies over the declared /ByteRange.
public class VisibleSignatureTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    private static byte[] BuildPdf(int pages = 1)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Signed document body", BuildTestSupport.Latin);
        for (var i = 1; i < pages; i++)
        {
            section.Blocks.AddPageBreak();
            BuildTestSupport.AddText(section, "Another page " + i, BuildTestSupport.Latin);
        }

        return builder.ToArray();
    }

    private static SignatureOptions Options(SignatureAppearance? appearance = null) => new()
    {
        Reason = "Approval",
        Location = "Sofia",
        SignerName = "Radzen Test Signer",
        SigningTime = FixedTime,
        Appearance = appearance,
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

    private sealed class CmsSigner2(X509Certificate2 certificate) : ISigner
    {
        public byte[] Sign(ReadOnlySpan<byte> content)
        {
            var cms = new SignedCms(new ContentInfo(content.ToArray()), detached: true);
            cms.ComputeSignature(new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate));
            return cms.Encode();
        }
    }

    private sealed class FixedSigner(byte[] blob) : ISigner
    {
        public byte[] Sign(ReadOnlySpan<byte> content) => blob;
    }

    private static DictionaryObject Catalog(DocumentReader reader)
        => (DictionaryObject)reader.Resolve(reader.Trailer["Root"]);

    private static DictionaryObject Field(DocumentReader reader, int index)
    {
        var acroForm = (DictionaryObject)reader.Resolve(Catalog(reader)["AcroForm"]);
        var fields = (ArrayObject)reader.Resolve(acroForm["Fields"]);
        return (DictionaryObject)reader.Resolve(fields[index]);
    }

    private static double[] RectOf(DocumentReader reader, DictionaryObject field)
    {
        var rect = (ArrayObject)reader.Resolve(field["Rect"]);
        return [.. rect.Select(entry => ((NumberObject)reader.Resolve(entry)).DoubleValue)];
    }

    private static string AppearanceText(DocumentReader reader, DictionaryObject field)
    {
        var ap = (DictionaryObject)reader.Resolve(field["AP"]);
        var stream = (StreamObject)reader.Resolve(ap["N"]);
        var data = stream.Data;
        if (stream.Dictionary.TryGetValue("Filter", out var filter)
            && filter is NameObject name && name.Value is "FlateDecode" or "Fl")
        {
            data = FlateFilter.Decode(data);
        }

        return Encoding.Latin1.GetString(data);
    }

    private static void VerifyCms(byte[] signed)
    {
        var reader = DocumentReader.Parse(signed);
        var signature = (DictionaryObject)reader.Resolve(Field(reader, 0)["V"]);
        var range = (ArrayObject)reader.Resolve(signature["ByteRange"]);
        int gapStart = ((NumberObject)range[1]).IntValue;
        int gapEnd = ((NumberObject)range[2]).IntValue;
        int tail = ((NumberObject)range[3]).IntValue;

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
        var original = BuildPdf();
        var signed = PdfSigner.Sign(original, Options(), new FixedSigner(new byte[100]));

        var reader = DocumentReader.Parse(signed);
        var field = Field(reader, 0);

        Assert.Equal([0, 0, 0, 0], RectOf(reader, field));
        Assert.False(field.ContainsKey("AP"));
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
        var original = BuildPdf();
        var appearance = new SignatureAppearance { X = 72, Y = 700, Width = 200, Height = 60 };
        var signed = PdfSigner.Sign(original, Options(appearance), new FixedSigner(new byte[100]));

        var reader = DocumentReader.Parse(signed);
        var field = Field(reader, 0);

        Assert.Equal([72, 700, 272, 760], RectOf(reader, field));

        var text = AppearanceText(reader, field);
        Assert.Contains("Radzen Test Signer", text);
        Assert.Contains("Approval", text);
        Assert.Contains("2026-03-15", text);
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
        var original = BuildPdf(pages: 2);
        var appearance = new SignatureAppearance { PageIndex = 1, X = 72, Y = 700, Width = 200, Height = 60 };
        var signed = PdfSigner.Sign(original, Options(appearance), new FixedSigner(new byte[100]));

        var reader = DocumentReader.Parse(signed);
        var pages = BuildTestSupport.PageLeaves(reader).Select(p => p.Page).ToList();
        Assert.Equal(2, pages.Count);

        var fieldNumber = ((ReferenceObject)((ArrayObject)reader.Resolve(
            ((DictionaryObject)reader.Resolve(Catalog(reader)["AcroForm"]))["Fields"]))[0]).ObjectNumber;

        Assert.True(pages[1].TryGetValue("Annots", out var annots1));
        Assert.Contains((ArrayObject)reader.Resolve(annots1!),
            a => a is ReferenceObject r && r.ObjectNumber == fieldNumber);

        var onFirst = pages[0].TryGetValue("Annots", out var annots0)
            && ((ArrayObject)reader.Resolve(annots0!)).Any(a => a is ReferenceObject r && r.ObjectNumber == fieldNumber);
        Assert.False(onFirst);
    }
}
