#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
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
// the covered bytes. The real-CMS tests drive Microsoft's SignedCms via
// reflection because the test project cannot take a compile-time reference on
// System.Security.Cryptography.Pkcs without modifying its csproj.
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

        public byte[] Sign(ReadOnlySpan<byte> content)
        {
            LastContent = content.ToArray();
            LastSignature = sign(LastContent);
            return LastSignature;
        }
    }

    private static DelegateSigner CmsSigner(X509Certificate2 certificate)
        => new(content => Pkcs.SignDetached(certificate, content));

    // Reflection facade over System.Security.Cryptography.Pkcs loaded from the
    // NuGet cache: the desktop test host may use it freely, only the library
    // itself must stay WASM-safe.
    private static class Pkcs
    {
        private static readonly Lazy<Assembly> Assembly = new(Load);

        private static Assembly Load()
        {
            var root = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            var package = Path.Combine(root, "system.security.cryptography.pkcs");
            var versions = Directory.Exists(package) ? Directory.GetDirectories(package) : [];
            foreach (var version in versions.OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var tfm in new[] { "net9.0", "net8.0", "netstandard2.1" })
                {
                    var path = Path.Combine(version, "lib", tfm, "System.Security.Cryptography.Pkcs.dll");
                    if (File.Exists(path))
                    {
                        return System.Reflection.Assembly.LoadFrom(path);
                    }
                }
            }

            throw new InvalidOperationException(
                "System.Security.Cryptography.Pkcs was not found in the NuGet cache; " +
                "run `dotnet add package System.Security.Cryptography.Pkcs` in any project once to populate it.");
        }

        private static Type Get(string name)
            => Assembly.Value.GetType("System.Security.Cryptography.Pkcs." + name, throwOnError: true)!;

        private static object CreateDetachedCms(byte[] content)
        {
            var contentInfo = Activator.CreateInstance(Get("ContentInfo"), [content])!;
            return Activator.CreateInstance(Get("SignedCms"), [contentInfo, true])!;
        }

        private static object? Invoke(object target, string method, Type[] signature, object?[] args)
        {
            try
            {
                return target.GetType().GetMethod(method, signature)!.Invoke(target, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        public static byte[] SignDetached(X509Certificate2 certificate, byte[] content)
        {
            var cms = CreateDetachedCms(content);
            var signerType = Get("CmsSigner");
            var identifier = Enum.Parse(Get("SubjectIdentifierType"), "IssuerAndSerialNumber");
            var signer = Activator.CreateInstance(signerType, [identifier, certificate])!;
            Invoke(cms, "ComputeSignature", [signerType], [signer]);
            return (byte[])Invoke(cms, "Encode", [], [])!;
        }

        // Throws CryptographicException when the signature does not verify.
        public static void CheckSignature(byte[] content, byte[] der)
        {
            var cms = CreateDetachedCms(content);
            Invoke(cms, "Decode", [typeof(byte[])], [der]);
            Invoke(cms, "CheckSignature", [typeof(bool)], [true]);
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
