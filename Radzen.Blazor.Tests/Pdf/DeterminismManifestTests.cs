#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Signing;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class DeterminismManifestTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

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
            run.Font.Family = family;
        }

        run.Font.Size = size;
        return paragraph;
    }


    private static byte[] PlainText()
    {
        var document = new Document();
        document.Info.Title = "Plain text";
        var section = document.Sections.Add();
        section.Blocks.Add(Text("The quick brown fox jumps over the lazy dog."));
        section.Blocks.Add(Text("Second base-14 paragraph, no embedded font."));
        return new DocumentRenderer().ToArray(document);
    }

    private static byte[] TrueTypeSubset()
    {
        var document = new Document();
        document.Info.Title = "TrueType subset";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.Blocks.Add(Text("Embedded Liberation Sans subset AWAY VoTo.", BuildTestSupport.Latin));
        return new DocumentRenderer().ToArray(document);
    }

    private static byte[] Tables()
    {
        var document = new Document();
        document.Info.Title = "Table";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();

        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        header.IsHeaderRow = true;
        TableLayoutSupport.Fill(header.Cells[0], "Item");
        TableLayoutSupport.Fill(header.Cells[1], "Price");

        foreach (var (name, price) in new[] { ("Apple", "111"), ("Bread", "222"), ("Cherry", "333") })
        {
            var row = table.Rows.Add();
            TableLayoutSupport.Fill(row.Cells[0], name);
            TableLayoutSupport.Fill(row.Cells[1], price);
        }

        return new DocumentRenderer().ToArray(document);
    }

    private static byte[] Image()
    {
        var document = new Document();
        document.Info.Title = "Image";
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(120);
        image.Height = Unit.FromPoint(120);
        return new DocumentRenderer().ToArray(document);
    }

    private static byte[] Gradients()
    {
        var document = new Document();
        document.Info.Title = "Gradients";
        var section = document.Sections.Add();
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
        return new DocumentRenderer().ToArray(document);
    }

    private static byte[] OverlappingZOrder()
    {
        var document = new Document();
        document.Info.Title = "Overlapping z-order";
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(200));
        section.Margins.SetAll(Unit.FromPoint(20));
        var overlay = section.Blocks.Add(new Container { Layout = ContainerLayout.Overlay });
        overlay.Blocks.Add(Text("TEXT BELOW IMAGE"));
        var image = overlay.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(80);
        image.Height = Unit.FromPoint(40);
        return new DocumentRenderer().ToArray(document);
    }

    private static byte[] Encrypted()
    {
        var document = new PortableDocument();
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
        var document = new Document();
        document.Info.Title = "Signed";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.Blocks.Add(Text("Document body to be signed.", BuildTestSupport.Latin));
        return new DocumentRenderer().ToArray(document);
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
        yield return new object[] { "overlapping-z-order", (Func<byte[]>)OverlappingZOrder };
        yield return new object[] { "encrypted", (Func<byte[]>)Encrypted };
        yield return new object[] { "signed", (Func<byte[]>)Signed };
        yield return new object[] { "timestamped", (Func<byte[]>)Timestamped };
    }

    private static readonly Dictionary<string, string> ExpectedSha256 = new()
    {
        ["plain-text"] = "17509576c3839f8833478414a2d21189753951372311492c094100caf2010b34",
        ["truetype-subset"] = "2e28d4416c5c7925b9d581ebd7e99f20d23a491bd6d75a1cd2155393e481e963",
        ["tables"] = "6a16fe3b40d900cde01f93d12dd072eaddd1c8f3b6fd7f870c80fb1f038b249f",
        ["image"] = "d85431f2632cdccfc4699270f92007c5ce74cb1f9e96c10f0af9c140a6b3ef0f",
        ["gradients"] = "ea3c0eec05d7c73e4895c3b1a9a0605ee7903c5e626020e079743c259e217a2e",
        ["overlapping-z-order"] = "aad96ec8fd20b1124d003a8e8be6d2d71ffdcb5c3b03905f597b1b7825e21ae9",
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
            $"Corpus document '{name}' has no pinned SHA-256. {RePinInstructions(name, actual)}");
        Assert.True(
            expected == actual,
            $"Corpus document '{name}' no longer renders to its pinned SHA-256.{Environment.NewLine}"
                + $"pinned:   {expected}{Environment.NewLine}"
                + $"produced: {actual}{Environment.NewLine}"
                + RePinInstructions(name, actual));
    }

    private static string RePinInstructions(string name, string actual)
        => "Pinned hashes are not auto-generated: edit the ExpectedSha256 dictionary in "
            + $"{nameof(DeterminismManifestTests)} by hand, setting [\"{name}\"] = \"{actual}\". "
            + "Do that only for a reviewed, intended change to PDF output; a hash that moves "
            + "without such a change is a determinism regression and must be fixed at the source "
            + "instead of re-pinned.";
}
