#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Signing;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

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

    private static byte[] FlatLists()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));

        var bullets = section.Blocks.AddList(ListStyle.Bullet);
        bullets.HangingIndent = Unit.FromPoint(20);
        bullets.AddItem("Alpha");
        bullets.AddItem("Beta");

        var numbers = section.Blocks.AddList(ListStyle.Number);
        numbers.LeftIndent = Unit.FromPoint(6);
        numbers.AddItem("One");
        numbers.AddItem("Two");
        numbers.AddItem("Three");

        return new DocumentRenderer().ToArray(document);
    }

    private static Document TaggedStructure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Tagged structure";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();

        var heading = section.Blocks.Add(Text("Invoice", BuildTestSupport.Latin, 18));
        heading.StyleName = "Heading1";
        section.Blocks.Add(Text("Billed to Acme Corp.", BuildTestSupport.Latin));

        var linked = section.Blocks.AddParagraph();
        var link = linked.Inlines.Add("Radzen");
        link.Font.Family = BuildTestSupport.Latin;
        link.Link = "https://www.radzen.com";

        var bullets = section.Blocks.AddList(ListStyle.Bullet);
        bullets.Font.Family = BuildTestSupport.Latin;
        bullets.AddItem("Alpha").Font.Family = BuildTestSupport.Latin;
        bullets.AddItem("Beta").Font.Family = BuildTestSupport.Latin;

        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();

        var header = table.Rows.Add();
        header.IsHeaderRow = true;
        TableLayoutSupport.Fill(header.Cells[0], "Item");
        TableLayoutSupport.Fill(header.Cells[1], "Price");

        foreach (var (name, price) in new[] { ("Apple", "111"), ("Bread", "222") })
        {
            var row = table.Rows.Add();
            TableLayoutSupport.Fill(row.Cells[0], name);
            TableLayoutSupport.Fill(row.Cells[1], price);
        }

        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg")).AlternateText = "A red square";

        return document;
    }

    private static byte[] TaggedAccessible()
        => new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.ToArray(TaggedStructure());

    private static byte[] TaggedPdfALevelA()
        => new DocumentRenderer { Conformance = PdfAConformance.PdfA2A }.ToArray(TaggedStructure());

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
        yield return new object[] { "flat-lists", (Func<byte[]>)FlatLists };
        yield return new object[] { "tagged-accessible", (Func<byte[]>)TaggedAccessible };
        yield return new object[] { "tagged-pdfa-level-a", (Func<byte[]>)TaggedPdfALevelA };
        yield return new object[] { "encrypted", (Func<byte[]>)Encrypted };
        yield return new object[] { "signed", (Func<byte[]>)Signed };
        yield return new object[] { "timestamped", (Func<byte[]>)Timestamped };
    }

    private static readonly Dictionary<string, string> ExpectedSha256 = new()
    {
        ["plain-text"] = "43dd319a72c366eb26685a4ab1820c4a1278d6e007ea8f5ac59a60518c0550d6",
        ["truetype-subset"] = "d34226429319058e3d0ef7462a32c60f2497e3f7eb252ade5f9a39785dd15a8d",
        ["tables"] = "980b99a7cbdd0c90405eaed869e27af378e0830f71ec6dc5f51d319350179a19",
        ["image"] = "f2eae06c720322e074bb1d8acdae654c37c1926807495c2c9442bae829451caa",
        ["gradients"] = "06b60969773939b6475f5447163b8228e6a85fb10711f9b1721db75006f48089",
        ["overlapping-z-order"] = "8c7f801a013aa0921962214d6d06326b357c07c613e0ecd76ed109a43e8e89b5",
        ["flat-lists"] = "e3cd6555c5ad23d53cba15e9f1ee3e99759afd98883fd34af9e4b681e2746c68",
        ["tagged-accessible"] = "53cf5b884b9075790e2f3a574e64fb820905872b1b2d25a4f85d21f169c16bb0",
        ["tagged-pdfa-level-a"] = "53bfaf5298a44578cf11bdcf4ec7e91fbd9d9c8e287e657b39de69e37da64fd2",
        ["encrypted"] = "b5275db20e46847a798c68d46ca9d3ab7f79afd1a50ae04e8ef375ff60befbf5",
        ["signed"] = "27350d26638aa12b6dc605b9cc052d41ac13bf6cdb9f8b7d502b555908d2f8e3",
        ["timestamped"] = "3acf8fa5b456a20985a4de33dd1d05dc98c742c07e65a2172495925866ca3dbc",
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

    public static IEnumerable<object[]> TaggedCorpus()
    {
        yield return new object[] { (Func<byte[]>)TaggedAccessible };
        yield return new object[] { (Func<byte[]>)TaggedPdfALevelA };
    }

    [Theory]
    [MemberData(nameof(TaggedCorpus))]
    public void TaggedCorpusDocument_CarriesAStructureTree(Func<byte[]> build)
    {
        var reader = DocumentReader.Parse(build());
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));

        Assert.True(catalog.TryGetValue("MarkInfo", out var markInfo), "catalog has /MarkInfo");
        Assert.True(
            Assert.IsType<BooleanObject>(
                reader.Resolve(Assert.IsType<DictionaryObject>(reader.Resolve(markInfo!))["Marked"])).Value,
            "/Marked is true");

        var structRoot = Assert.IsType<DictionaryObject>(
            reader.Resolve(catalog["StructTreeRoot"]));
        Assert.True(structRoot.ContainsKey("K"), "StructTreeRoot has kids");
        Assert.True(structRoot.ContainsKey("ParentTree"), "StructTreeRoot has /ParentTree");
    }

    private static string RePinInstructions(string name, string actual)
        => "Pinned hashes are not auto-generated: edit the ExpectedSha256 dictionary in "
            + $"{nameof(DeterminismManifestTests)} by hand, setting [\"{name}\"] = \"{actual}\". "
            + "Do that only for a reviewed, intended change to PDF output; a hash that moves "
            + "without such a change is a determinism regression and must be fixed at the source "
            + "instead of re-pinned.";
}
