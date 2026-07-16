#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects.Encryption;
using Radzen.Documents.Pdf.Signing;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Broad determinism manifest. A representative corpus exercises distinct emit
// paths - plain text, tables, an embedded TrueType subset, an image, gradients,
// a seeded-encrypted document, and a seeded-signed/timestamped document. Each is
// generated TWICE in-process and asserted byte-identical (catching HashSet-order
// and other emit-path nondeterminism that per-feature ByteIdentical tests miss),
// and each one's SHA-256 is pinned to a checked-in expected value so the test
// also catches cross-change drift in real output. Test-only: no library or byte
// change to real output. Deterministic seams are used for the crypto paths
// (SeededEncryptionMaterial, a fixed ISigner/ITimestampProvider, caller-supplied
// dates) so those documents are reproducible without any RNG.
public class DeterminismManifestTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

    // Fixed, deterministic stand-in for a real detached CMS blob. The library
    // treats the ISigner output as opaque bytes, so a fixed payload is enough to
    // drive the full signing emit path reproducibly.
    private sealed class FixedSigner : ISigner
    {
        private readonly byte[] signature;

        public FixedSigner(int length)
        {
            signature = new byte[length];
            for (var i = 0; i < length; i++)
            {
                signature[i] = (byte)(i * 31 + 7);
            }
        }

        public byte[] Sign(SignedContent content) => (byte[])signature.Clone();
    }

    // Fixed, deterministic stand-in for an RFC 3161 token from a TSA.
    private sealed class FixedTimestampProvider : ITimestampProvider
    {
        private readonly byte[] token;

        public FixedTimestampProvider(int length)
        {
            token = new byte[length];
            for (var i = 0; i < length; i++)
            {
                token[i] = (byte)(i * 17 + 3);
            }
        }

        public byte[] GetTimestampToken(ReadOnlySpan<byte> hash) => (byte[])token.Clone();
    }

    private static Paragraph Text(string text, string? family = null, double size = 12)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        if (family is not null)
        {
            run.Font.Name = family;
        }

        run.Font.Size = size;
        return paragraph;
    }

    // --- Corpus builders. Each returns the bytes of one document. ---

    private static byte[] PlainText()
    {
        var builder = new DocumentBuilder();
        builder.Info.Title = "Plain text";
        var section = builder.Sections.Add();
        section.Blocks.Add(Text("The quick brown fox jumps over the lazy dog."));
        section.Blocks.Add(Text("Second base-14 paragraph, no embedded font."));
        return builder.ToArray();
    }

    private static byte[] TrueTypeSubset()
    {
        var builder = new DocumentBuilder();
        builder.Info.Title = "TrueType subset";
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        section.Blocks.Add(Text("Embedded Liberation Sans subset AWAY VoTo.", BuildTestSupport.Latin));
        return builder.ToArray();
    }

    private static byte[] Tables()
    {
        var builder = new DocumentBuilder();
        builder.Info.Title = "Table";
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();

        var header = table.Rows.Add();
        header.IsHeader = true;
        TableLayoutSupport.Fill(header.Cells[0], "Item");
        TableLayoutSupport.Fill(header.Cells[1], "Price");

        foreach (var (name, price) in new[] { ("Apple", "111"), ("Bread", "222"), ("Cherry", "333") })
        {
            var row = table.Rows.Add();
            TableLayoutSupport.Fill(row.Cells[0], name);
            TableLayoutSupport.Fill(row.Cells[1], price);
        }

        return builder.ToArray();
    }

    private static byte[] Image()
    {
        var builder = new DocumentBuilder();
        builder.Info.Title = "Image";
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(120);
        image.Height = Unit.FromPoint(120);
        return builder.ToArray();
    }

    private static byte[] Gradients()
    {
        var builder = new DocumentBuilder();
        builder.Info.Title = "Gradients";
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            BackgroundGradient = new LinearGradient(
                0, 0, 100, 0,
                new GradientStop(0, Color.FromRgb(255, 0, 0)),
                new GradientStop(1, Color.FromRgb(0, 0, 255))),
        });
        container.Blocks.Add(Text("Boxed over a linear gradient."));

        var radial = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            BackgroundGradient = new RadialGradient(
                50, 50, 0, 50, 50, 40,
                new GradientStop(0, Color.White),
                new GradientStop(1, Color.Black)),
        });
        radial.Blocks.Add(Text("Boxed over a radial gradient."));
        return builder.ToArray();
    }

    private static byte[] Encrypted()
    {
        var document = new Document();
        document.Info.Title = "Encrypted";
        document.Info.Producer = "Radzen determinism manifest";
        document.Pages.Add(PageSizes.A4).SetContent(
            System.Text.Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 720 Td (Encrypted body) Tj ET"));
        document.Encryption = new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([1, 2, 3, 4, 5, 6, 7, 8]),
            Algorithm = EncryptionAlgorithm.Aes256,
            UserPassword = "user",
            OwnerPassword = "owner",
        };
        return document.ToArray();
    }

    private static byte[] SignedBase()
    {
        var builder = new DocumentBuilder();
        builder.Info.Title = "Signed";
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        section.Blocks.Add(Text("Document body to be signed.", BuildTestSupport.Latin));
        return builder.ToArray();
    }

    private static byte[] Signed()
    {
        var options = new SignatureOptions
        {
            Reason = "Approval",
            Location = "Sofia",
            ContactInfo = "info@radzen.com",
            SignerName = "Radzen Determinism Signer",
            SigningTime = FixedTime,
        };
        return PdfSigner.Sign(SignedBase(), options, new FixedSigner(1000));
    }

    private static byte[] Timestamped()
        => PdfTimestamper.Timestamp(SignedBase(), new FixedTimestampProvider(1200));

    public static IEnumerable<object[]> Corpus()
    {
        yield return new object[] { "plain-text", (Func<byte[]>)PlainText };
        yield return new object[] { "truetype-subset", (Func<byte[]>)TrueTypeSubset };
        yield return new object[] { "tables", (Func<byte[]>)Tables };
        yield return new object[] { "image", (Func<byte[]>)Image };
        yield return new object[] { "gradients", (Func<byte[]>)Gradients };
        yield return new object[] { "encrypted", (Func<byte[]>)Encrypted };
        yield return new object[] { "signed", (Func<byte[]>)Signed };
        yield return new object[] { "timestamped", (Func<byte[]>)Timestamped };
    }

    // SHA-256 of each document's bytes, pinned so a change to any emit path that
    // alters real output surfaces here as a drift failure. Regenerate only when a
    // byte change is intended and understood.
    private static readonly Dictionary<string, string> ExpectedSha256 = new()
    {
        ["plain-text"] = "17509576c3839f8833478414a2d21189753951372311492c094100caf2010b34",
        ["truetype-subset"] = "2e28d4416c5c7925b9d581ebd7e99f20d23a491bd6d75a1cd2155393e481e963",
        ["tables"] = "6a16fe3b40d900cde01f93d12dd072eaddd1c8f3b6fd7f870c80fb1f038b249f",
        ["image"] = "d85431f2632cdccfc4699270f92007c5ce74cb1f9e96c10f0af9c140a6b3ef0f",
        ["gradients"] = "65e26f92ba5254fcd24616755deb28b02ce764f5d12985b4ff66779a6d3ff8f5",
        ["encrypted"] = "4d127aa5387dd6565d2da8083d765dc5fa85c57147ddfb1061a51cd17c58e611",
        ["signed"] = "ab5875e082b064a4ed84920dc14b9801da372bb9cbbe9036e7cb6a4289ec2fd7",
        ["timestamped"] = "bc6d540a6e43addae1e74627e6c972f3c8c23abb4bd72ef7e43f5c64acd7dd49",
    };

    private static string Sha256Hex(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    [Theory]
    [MemberData(nameof(Corpus))]
    public void Document_GeneratedTwice_IsByteIdentical(string name, Func<byte[]> build)
    {
        _ = name;
        var first = build();
        var second = build();
        Assert.Equal(first, second);
    }

    [Theory]
    [MemberData(nameof(Corpus))]
    public void Document_Sha256_MatchesPinnedValue(string name, Func<byte[]> build)
    {
        var actual = Sha256Hex(build());
        Assert.True(
            ExpectedSha256.TryGetValue(name, out var expected),
            $"no pinned SHA-256 for corpus document '{name}'");
        Assert.Equal(expected, actual);
    }
}
