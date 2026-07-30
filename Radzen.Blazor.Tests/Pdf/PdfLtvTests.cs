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
            raw[i] = Convert.ToByte(Encoding.ASCII.GetString(bytes, gapStart + 1 + 2 * i, 2), 16);
        }

        return raw;
    }

    private static DictionaryObject Dss(DocumentReader reader)
        => (DictionaryObject)reader.Resolve(Catalog(reader)["DSS"]);

    private static byte[][] StreamBytes(DocumentReader reader, DictionaryObject dss, string key)
    {
        if (!dss.TryGetValue(key, out var value) || value is null)
        {
            return Array.Empty<byte[]>();
        }

        var array = (ArrayObject)reader.Resolve(value);
        return array.Select(item => reader.DecodeStream((StreamObject)reader.Resolve(item))).ToArray();
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

        var reader = DocumentReader.Parse(stamped);
        var signature = SignatureValue(reader, 0);
        Assert.Equal("DocTimeStamp", ((NameObject)signature["Type"]).Value);
        Assert.Equal("Adobe.PPKLite", ((NameObject)signature["Filter"]).Value);
        Assert.Equal("ETSI.RFC3161", ((NameObject)signature["SubFilter"]).Value);
        Assert.Equal("Sig", ((NameObject)Field(reader, 0)["FT"]).Value);
    }

    [Fact]
    public void Timestamp_ByteRangeCoversFileExceptContentsAndHashesIt()
    {
        var original = BuildPdf();
        var token = Enumerable.Range(0, 512).Select(i => (byte)i).ToArray();
        var provider = new RecordingTimestampProvider(token);

        var stamped = PdfTimestamper.Timestamp(original, provider);
        var reader = DocumentReader.Parse(stamped);
        var (gapStart, gapEnd, tail) = ByteRange(reader, SignatureValue(reader, 0));

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

        var reader = DocumentReader.Parse(ltv);
        var dss = Dss(reader);

        var certs = StreamBytes(reader, dss, "Certs");
        Assert.Equal(2, certs.Length);
        Assert.True(certs[0].SequenceEqual(cert1));
        Assert.True(certs[1].SequenceEqual(cert2));

        var ocsps = StreamBytes(reader, dss, "OCSPs");
        Assert.Single(ocsps);
        Assert.True(ocsps[0].SequenceEqual(ocsp));

        var crls = StreamBytes(reader, dss, "CRLs");
        Assert.Single(crls);
        Assert.True(crls[0].SequenceEqual(crl));

        Assert.False(dss.ContainsKey("VRI"));
    }

    [Fact]
    public void AddValidationData_WritesVriKeyedBySha1OfSignatureContents()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());

        var signedReader = DocumentReader.Parse(signed);
        var (gapStart, gapEnd, _) = ByteRange(signedReader, SignatureValue(signedReader, 0));
        var contents = DecodeContentsHex(signed, gapStart, gapEnd);

        var cert = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();
        var ltv = DssBuilder.AddValidationData(signed, [cert], null, null, contents);

        var reader = DocumentReader.Parse(ltv);
        var dss = Dss(reader);
        var vri = (DictionaryObject)reader.Resolve(dss["VRI"]);

        var expectedKey = Convert.ToHexString(SHA1.HashData(contents));
        Assert.Contains(expectedKey, vri.Keys);

        var entry = (DictionaryObject)reader.Resolve(vri[expectedKey]);
        var vriCerts = (ArrayObject)reader.Resolve(entry["Cert"]);
        Assert.Single(vriCerts);
        Assert.True(reader.DecodeStream((StreamObject)reader.Resolve(vriCerts[0])).SequenceEqual(cert));
    }

    [Fact]
    public void AddValidationData_MergesWithExistingDss()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        var cert1 = Enumerable.Range(0, 40).Select(i => (byte)(i + 1)).ToArray();
        var cert2 = Enumerable.Range(0, 44).Select(i => (byte)(i + 9)).ToArray();

        var contentsReader = DocumentReader.Parse(signed);
        var (gapStart, gapEnd, _) = ByteRange(contentsReader, SignatureValue(contentsReader, 0));
        var contents = DecodeContentsHex(signed, gapStart, gapEnd);

        var first = DssBuilder.AddValidationData(signed, [cert1], null, null, contents);
        var second = DssBuilder.AddValidationData(first, [cert2], null, null, contents);

        var reader = DocumentReader.Parse(second);
        var dss = Dss(reader);

        var certs = StreamBytes(reader, dss, "Certs");
        Assert.Equal(2, certs.Length);
        Assert.True(certs[0].SequenceEqual(cert1));
        Assert.True(certs[1].SequenceEqual(cert2));

        var vri = (DictionaryObject)reader.Resolve(dss["VRI"]);
        var key = Convert.ToHexString(SHA1.HashData(contents));
        var entry = (DictionaryObject)reader.Resolve(vri[key]);
        var vriCerts = (ArrayObject)reader.Resolve(entry["Cert"]);
        Assert.Equal(2, vriCerts.Count);
        Assert.True(reader.DecodeStream((StreamObject)reader.Resolve(vriCerts[0])).SequenceEqual(cert1));
        Assert.True(reader.DecodeStream((StreamObject)reader.Resolve(vriCerts[1])).SequenceEqual(cert2));
    }

    [Fact]
    public void AddValidationData_MergingSameSignatureUnionsVriInsteadOfReplacing()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        var certA = Enumerable.Range(0, 40).Select(i => (byte)(i + 1)).ToArray();
        var crlB = Enumerable.Range(0, 50).Select(i => (byte)(i + 2)).ToArray();

        var contentsReader = DocumentReader.Parse(signed);
        var (gapStart, gapEnd, _) = ByteRange(contentsReader, SignatureValue(contentsReader, 0));
        var contents = DecodeContentsHex(signed, gapStart, gapEnd);

        var first = DssBuilder.AddValidationData(signed, [certA], null, null, contents);
        var second = DssBuilder.AddValidationData(first, null, null, [crlB], contents);

        var reader = DocumentReader.Parse(second);
        var dss = Dss(reader);
        var vri = (DictionaryObject)reader.Resolve(dss["VRI"]);
        var key = Convert.ToHexString(SHA1.HashData(contents));
        var entry = (DictionaryObject)reader.Resolve(vri[key]);

        var vriCerts = (ArrayObject)reader.Resolve(entry["Cert"]);
        Assert.Single(vriCerts);
        Assert.True(reader.DecodeStream((StreamObject)reader.Resolve(vriCerts[0])).SequenceEqual(certA));

        var vriCrls = (ArrayObject)reader.Resolve(entry["CRL"]);
        Assert.Single(vriCrls);
        Assert.True(reader.DecodeStream((StreamObject)reader.Resolve(vriCrls[0])).SequenceEqual(crlB));
    }

    [Fact]
    public void AddValidationData_ReAddingIdenticalMaterialDoesNotDuplicateStreams()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());
        var cert = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();

        var first = DssBuilder.AddValidationData(signed, [cert], null, null);
        var second = DssBuilder.AddValidationData(first, [cert], null, null);

        var reader = DocumentReader.Parse(second);
        var certs = StreamBytes(reader, Dss(reader), "Certs");
        Assert.Single(certs);
        Assert.True(certs[0].SequenceEqual(cert));
    }

    [Fact]
    public void AddValidationData_PreservesNonStandardKeysOfExistingDss()
    {
        var signed = SignFixed(BuildPdf(), Enumerable.Range(0, 200).Select(i => (byte)i).ToArray());

        var withDss = InjectDssWithCustomKey(signed);

        var augmented = DssBuilder.AddValidationData(withDss, [Enumerable.Range(0, 10).Select(i => (byte)i).ToArray()], null, null);

        var augmentedReader = DocumentReader.Parse(augmented);
        var dss = Dss(augmentedReader);
        Assert.True(dss.ContainsKey("TU"));
        Assert.Equal(7, ((NumberObject)augmentedReader.Resolve(dss["TU"])).IntValue);
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

        var reader = DocumentReader.Parse(lta);

        var fields = (ArrayObject)reader.Resolve(AcroForm(reader)["Fields"]);
        Assert.Equal(2, fields.Count);
        Assert.Equal("adbe.pkcs7.detached", ((NameObject)SignatureValue(reader, 0)["SubFilter"]).Value);
        Assert.Equal("ETSI.RFC3161", ((NameObject)SignatureValue(reader, 1)["SubFilter"]).Value);
        Assert.True(Dss(reader).ContainsKey("Certs"));

        var (_, gapEnd, tail) = ByteRange(reader, SignatureValue(reader, 0));
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

        Assert.Equal(37647, signed.Length);
        Assert.Equal(
            "6280C446B666D888BDD412434412E2C04325955078A35EDED010D65A4351CB99",
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
