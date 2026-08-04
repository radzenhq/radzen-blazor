#nullable enable
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Radzen.Documents.Internal;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Signing;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfLtvTests
{
    private static byte[] BuildPdf()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Long-term-validation body", BuildTestSupport.Latin);
        return new DocumentRenderer().ToArray(document);
    }

    private sealed class FixedSigner(byte[] blob) : ISigner
    {
        public byte[] Sign(SignedContent content) => blob;
    }

    private sealed class RecordingTimestampProvider(byte[] token) : ITimestampProvider
    {
        public byte[]? LastHash { get; private set; }

        public byte[] GetTimestampToken(ReadOnlySpan<byte> hash)
        {
            LastHash = hash.ToArray();
            return token;
        }
    }

    private static byte[] SignFixed(byte[] pdf, byte[] cms)
        => PdfSigner.Sign(pdf, new SignatureOptions { SignerName = "Signer" }, new FixedSigner(cms));

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

    private static string StreamPayload(byte[] data)
        => "stream\n" + Encoding.Latin1.GetString(data) + "\nendstream";

    private static void CarriesStream(string subject, string emission, string number, byte[] expected)
        => Carries(subject, StreamPayload(expected), IndirectObject(emission, number));

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
            raw[i] = Convert.ToByte(Encoding.ASCII.GetString(bytes, gapStart + 1 + 2 * i, 2), 16);
        }

        return raw;
    }

    [Fact]
    public void Timestamp_AppendsDocTimeStampFieldAsIncrementalUpdate()
    {
        var original = BuildPdf();
        var token = Enumerable.Range(0, 300).Select(i => (byte)(i * 7)).ToArray();
        var provider = new RecordingTimestampProvider(token);

        var stamped = PdfTimestamper.Timestamp(original, provider);

        Assert.True(stamped.Length > original.Length);
        Assert.True(stamped.AsSpan(0, original.Length).SequenceEqual(original));

        var emission = Emitted(stamped);
        var signature = SignatureOf(emission, "Signature1");
        Carries("time stamp", "/Type /DocTimeStamp", signature);
        Carries("time stamp", "/Filter /Adobe.PPKLite", signature);
        Carries("time stamp", "/SubFilter /ETSI.RFC3161", signature);
        Carries("time stamp field", "/FT /Sig", Line(emission, "/T (Signature1)"));
    }

    [Fact]
    public void Timestamp_ByteRangeCoversFileExceptContentsAndHashesIt()
    {
        var original = BuildPdf();
        var token = Enumerable.Range(0, 512).Select(i => (byte)i).ToArray();
        var provider = new RecordingTimestampProvider(token);

        var stamped = PdfTimestamper.Timestamp(original, provider);
        var (gapStart, gapEnd, tail) = ByteRange(Emitted(stamped), "Signature1");

        Assert.Equal(stamped.Length, gapEnd + tail);

        var content = CoveredContent(stamped, gapStart, gapEnd, tail);
        Assert.NotNull(provider.LastHash);
        Assert.True(SHA256.HashData(content).SequenceEqual(provider.LastHash!));

        var stored = DecodeContentsHex(stamped, gapStart, gapEnd);
        Assert.True(stored[..token.Length].SequenceEqual(token));
        Assert.All(stored[token.Length..], b => Assert.Equal(0, b));
    }

    [Fact]
    public void Timestamp_IsDeterministic()
    {
        var original = BuildPdf();
        var token = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();

        var first = PdfTimestamper.Timestamp(original, new RecordingTimestampProvider(token));
        var second = PdfTimestamper.Timestamp(original, new RecordingTimestampProvider(token));

        Assert.True(first.SequenceEqual(second));
    }

    [Fact]
    public void Timestamp_ThrowsWhenTokenExceedsReservedSize()
    {
        var original = BuildPdf();
        var provider = new RecordingTimestampProvider(new byte[100]);

        Assert.Throws<InvalidOperationException>(() => PdfTimestamper.Timestamp(original, provider, 64));
    }


    [Fact]
    public void AddValidationData_WritesDssWithOpaqueStreams()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        var cert1 = Enumerable.Range(0, 40).Select(i => (byte)(i + 1)).ToArray();
        var cert2 = Enumerable.Range(0, 55).Select(i => (byte)(i + 2)).ToArray();
        var ocsp = Enumerable.Range(0, 33).Select(i => (byte)(i + 3)).ToArray();
        var crl = Enumerable.Range(0, 77).Select(i => (byte)(i + 4)).ToArray();

        var ltv = DssBuilder.AddValidationData(signed, [cert1, cert2], [ocsp], [crl]);

        Assert.True(ltv.AsSpan(0, signed.Length).SequenceEqual(signed));

        var emission = Emitted(ltv);
        var dss = Line(emission, "/Type /DSS");

        var certs = References("DSS", "Certs", 2, dss);
        CarriesStream("certificate 1", emission, certs[0], cert1);
        CarriesStream("certificate 2", emission, certs[1], cert2);

        var ocsps = References("DSS", "OCSPs", 1, dss);
        CarriesStream("OCSP response", emission, ocsps[0], ocsp);

        var crls = References("DSS", "CRLs", 1, dss);
        CarriesStream("CRL", emission, crls[0], crl);

        Lacks("DSS", "/VRI", dss);
    }

    [Fact]
    public void AddValidationData_WritesVriKeyedBySha1OfSignatureContents()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());

        var (gapStart, gapEnd, _) = ByteRange(Emitted(signed), "Signature1");
        var contents = DecodeContentsHex(signed, gapStart, gapEnd);

        var cert = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();
        var ltv = DssBuilder.AddValidationData(signed, [cert], null, null, contents);

        var emission = Emitted(ltv);
        var dss = Line(emission, "/Type /DSS");

        var expectedKey = Convert.ToHexString(SHA1.HashData(contents));
        Carries("DSS /VRI", $"/{expectedKey} << /Type /VRI ", dss);

        var vriCerts = References("VRI entry", "Cert", 1, dss);
        CarriesStream("VRI certificate", emission, vriCerts[0], cert);
    }

    [Fact]
    public void AddValidationData_MergesWithExistingDss()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        var cert1 = Enumerable.Range(0, 40).Select(i => (byte)(i + 1)).ToArray();
        var cert2 = Enumerable.Range(0, 44).Select(i => (byte)(i + 9)).ToArray();

        var (gapStart, gapEnd, _) = ByteRange(Emitted(signed), "Signature1");
        var contents = DecodeContentsHex(signed, gapStart, gapEnd);

        var first = DssBuilder.AddValidationData(signed, [cert1], null, null, contents);
        var second = DssBuilder.AddValidationData(first, [cert2], null, null, contents);

        var emission = Emitted(second);

        var certs = References("DSS", "Certs", 2, emission);
        CarriesStream("certificate 1", emission, certs[0], cert1);
        CarriesStream("certificate 2", emission, certs[1], cert2);

        var dss = Line(emission, $"/Certs [{certs[0]} 0 R {certs[1]} 0 R]");
        var key = Convert.ToHexString(SHA1.HashData(contents));
        Carries("merged DSS /VRI", $"/{key} << /Type /VRI ", dss);

        var vriCerts = References("VRI entry", "Cert", 2, dss);
        CarriesStream("VRI certificate 1", emission, vriCerts[0], cert1);
        CarriesStream("VRI certificate 2", emission, vriCerts[1], cert2);
    }

    [Fact]
    public void AddValidationData_MergingSameSignatureUnionsVriInsteadOfReplacing()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        var certA = Enumerable.Range(0, 40).Select(i => (byte)(i + 1)).ToArray();
        var crlB = Enumerable.Range(0, 50).Select(i => (byte)(i + 2)).ToArray();

        var (gapStart, gapEnd, _) = ByteRange(Emitted(signed), "Signature1");
        var contents = DecodeContentsHex(signed, gapStart, gapEnd);

        var first = DssBuilder.AddValidationData(signed, [certA], null, null, contents);
        var second = DssBuilder.AddValidationData(first, null, null, [crlB], contents);

        var emission = Emitted(second);
        var dss = Line(emission, "/CRLs [");
        var key = Convert.ToHexString(SHA1.HashData(contents));
        Carries("merged DSS /VRI", $"/{key} << /Type /VRI ", dss);

        var vriCerts = References("VRI entry", "Cert", 1, dss);
        CarriesStream("VRI certificate", emission, vriCerts[0], certA);

        var vriCrls = References("VRI entry", "CRL", 1, dss);
        CarriesStream("VRI CRL", emission, vriCrls[0], crlB);
    }

    [Fact]
    public void AddValidationData_ReAddingIdenticalMaterialDoesNotDuplicateStreams()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        var cert = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();

        var first = DssBuilder.AddValidationData(signed, [cert], null, null);
        var second = DssBuilder.AddValidationData(first, [cert], null, null);

        var emission = Emitted(second);
        var certs = References("DSS", "Certs", 1, Line(emission, "/Type /DSS"));
        CarriesStream("certificate", emission, certs[0], cert);

        var copies = BuildTestSupport.CountOccurrences(emission, StreamPayload(cert));
        Assert.True(copies == 1, $"Expected the certificate stream once in the emission, found {copies}.");
    }

    [Fact]
    public void AddValidationData_PreservesNonStandardKeysOfExistingDss()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());

        var withDss = InjectDssWithCustomKey(signed);

        var augmented = DssBuilder.AddValidationData(withDss, [Enumerable.Range(0, 10).Select(i => (byte)i).ToArray()], null, null);

        var dss = Line(Emitted(augmented), "/Certs [");
        Assert.Equal(7, NumberIn(dss, "TU"));
    }

    private static byte[] InjectDssWithCustomKey(byte[] pdf)
    {
        var reader = DocumentReader.Parse(pdf);
        var rootRef = (ReferenceObject)reader.Trailer["Root"];
        var catalog = (DictionaryObject)reader.Resolve(rootRef);

        var writer = new IncrementalUpdateWriter(pdf, reader);
        var dss = new DictionaryObject
        {
            ["Type"] = new NameObject("DSS"),
            ["TU"] = new NumberObject(7),
        };
        var dssRef = writer.Add(dss);

        var newCatalog = new DictionaryObject();
        foreach (var pair in catalog)
        {
            newCatalog[pair.Key] = pair.Value;
        }

        newCatalog["DSS"] = dssRef;
        writer.Override(rootRef.ObjectNumber, newCatalog);
        return writer.ToArray();
    }

    [Fact]
    public void AddValidationData_IsDeterministic()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        var cert = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();

        var first = DssBuilder.AddValidationData(signed, [cert], null, null);
        var second = DssBuilder.AddValidationData(signed, [cert], null, null);

        Assert.True(first.SequenceEqual(second));
    }

    [Fact]
    public void AddValidationData_ThrowsWhenNothingSupplied()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        Assert.Throws<ArgumentException>(() => DssBuilder.AddValidationData(signed, null, null, null));
    }


    [Fact]
    public void SignThenDssThenTimestamp_ProducesStackedIncrementalUpdates()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());

        var cert = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();
        var ltv = DssBuilder.AddValidationData(signed, [cert], null, null);
        Assert.True(ltv.AsSpan(0, signed.Length).SequenceEqual(signed));

        var token = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var lta = PdfTimestamper.Timestamp(ltv, new RecordingTimestampProvider(token));
        Assert.True(lta.AsSpan(0, ltv.Length).SequenceEqual(ltv));

        var emission = Emitted(lta);

        References("AcroForm", "Fields", 2, emission);
        Carries("approval signature", "/SubFilter /adbe.pkcs7.detached", SignatureOf(emission, "Signature1"));
        Carries("time stamp", "/SubFilter /ETSI.RFC3161", SignatureOf(emission, "Signature2"));
        Carries("DSS", "/Certs [", Line(emission, "/Type /DSS"));

        var (_, gapEnd, tail) = ByteRange(emission, "Signature1");
        Assert.Equal(signed.Length, gapEnd + tail);
    }


    [Fact]
    public void Signing_IsByteIdenticalToPreLtvBaseline()
    {
        var pdf = BuildPdf_ForBaseline();
        var options = new SignatureOptions
        {
            Reason = "Approval",
            Location = "Sofia",
            ContactInfo = "info@radzen.com",
            SignerName = "Radzen Test Signer",
            SigningTime = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero),
        };
        var cms = Enumerable.Range(0, 200).Select(i => (byte)i).ToArray();

        var signed = PdfSigner.Sign(pdf, options, new FixedSigner(cms));

        Assert.Equal(36989, signed.Length);
        Assert.Equal(
            "F8843C20D74E855A33F73C6E7BF846FF35FBCB39AD077C74ADD5B582497F012C",
            Convert.ToHexString(SHA256.HashData(signed)));
    }

    private static byte[] BuildPdf_ForBaseline()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Signed document body", BuildTestSupport.Latin);
        return new DocumentRenderer().ToArray(document);
    }
}
