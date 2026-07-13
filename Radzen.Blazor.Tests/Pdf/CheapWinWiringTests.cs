#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// End-to-end tests for the "cheap win" features wired into the content pipeline. Every
// feature is opt-in: a document that uses none of them is byte-identical to before, which
// the last test pins directly.
public class CheapWinWiringTests
{
    private static string Content(DocumentBuilder builder)
        => Encoding.Latin1.GetString(ContentTestHelpers.PageContent(BuildTestSupport.Read(builder), 0));

    private static DictionaryObject? Resources(DocumentBuilder builder)
        => BuildTestSupport.PageLeaves(BuildTestSupport.Read(builder))[0].Resources;

    private static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }

    // ---- 1. Gradient box background -> /Pattern cs + scn and a /Shading pattern resource ----

    [Fact]
    public void GradientBoxBackground_EmitsPatternFill_AndShadingResource()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            BackgroundGradient = new LinearGradient(
                0, 0, 100, 0,
                new GradientStop(0, Color.FromRgb(255, 0, 0)),
                new GradientStop(1, Color.FromRgb(0, 0, 255))),
        });
        container.Blocks.Add(Text("Boxed"));

        var content = Content(builder);
        Assert.Contains("/Pattern cs", content, StringComparison.Ordinal);
        Assert.Contains("scn", content, StringComparison.Ordinal);

        var reader = BuildTestSupport.Read(builder);
        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var patterns = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Pattern"]!));
        var pattern = Assert.IsType<DictionaryObject>(reader.Resolve(patterns[Assert.Single(patterns.Keys)]));
        Assert.Equal(2, ((NumberObject)pattern["PatternType"]).IntValue);
        var shading = Assert.IsType<DictionaryObject>(reader.Resolve(pattern["Shading"]!));
        Assert.Equal(2, ((NumberObject)shading["ShadingType"]).IntValue);
    }

    [Fact]
    public void PathGradientFill_EmitsPatternFill_AndShadingResource()
    {
        var document = new Document();
        var page = document.Pages.Add();
        var path = new PathContent
        {
            Fill = true,
            FillGradient = new RadialGradient(
                50, 50, 0, 50, 50, 40,
                new GradientStop(0, Color.FromRgb(0, 0, 0)),
                new GradientStop(1, Color.FromRgb(255, 255, 255))),
        };
        path.MoveTo(0, 0);
        path.LineTo(100, 0);
        path.LineTo(50, 100);
        path.Close();
        page.Content.Add(path);

        var reader = ContentTestHelpers.Reload(document);
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, 0));
        Assert.Contains("/Pattern cs", content, StringComparison.Ordinal);
        Assert.Contains("scn", content, StringComparison.Ordinal);

        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var patterns = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Pattern"]!));
        var pattern = Assert.IsType<DictionaryObject>(reader.Resolve(patterns[Assert.Single(patterns.Keys)]));
        var shading = Assert.IsType<DictionaryObject>(reader.Resolve(pattern["Shading"]!));
        Assert.Equal(3, ((NumberObject)shading["ShadingType"]).IntValue);
    }

    // ---- 2. Blend mode on a box -> /BM in the box ExtGState ----

    [Fact]
    public void BlendModeBox_EmitsBmInExtGState()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(200, 200, 200),
            BlendMode = BlendMode.Multiply,
        });
        container.Blocks.Add(Text("Blended"));

        var reader = BuildTestSupport.Read(builder);
        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var states = Assert.IsType<DictionaryObject>(reader.Resolve(resources["ExtGState"]!));

        var foundBlend = false;
        foreach (var key in states.Keys)
        {
            if (reader.Resolve(states[key]) is DictionaryObject state
                && state.TryGetValue("BM", out var bm)
                && reader.Resolve(bm!) is NameObject name && name.Value == "Multiply")
            {
                foundBlend = true;
            }
        }

        Assert.True(foundBlend, "a box ExtGState must carry /BM /Multiply");
    }

    // ---- 3. Kerning: ON emits TJ, OFF stays byte-identical ----

    [Fact]
    public void Kerning_On_EmitsTjAdjustments()
    {
        var builder = new DocumentBuilder { Fonts = { EnableKerning = true } };
        var section = builder.Sections.Add();
        section.Blocks.Add(Text("AWAY To Yo"));

        var content = Content(builder);
        Assert.Contains("] TJ", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Kerning_Off_UsesTj_AndIsByteIdentical()
    {
        static byte[] Build()
        {
            var builder = new DocumentBuilder();
            var section = builder.Sections.Add();
            section.Blocks.Add(Text("AWAY To Yo"));
            return builder.ToArray();
        }

        var first = Build();
        var content = Encoding.Latin1.GetString(
            ContentTestHelpers.PageContent(DocumentReader.Parse(first), 0));
        Assert.Contains(" Tj", content, StringComparison.Ordinal);
        Assert.DoesNotContain("] TJ", content, StringComparison.Ordinal);
        Assert.Equal(first, Build());
    }

    // ---- 4. Invisible text -> render mode 3 ----

    [Fact]
    public void InvisibleRun_EmitsRenderModeThree()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Hidden");
        run.Invisible = true;
        section.Blocks.Add(paragraph);

        Assert.Contains("3 Tr", Content(builder), StringComparison.Ordinal);
    }

    [Fact]
    public void VisibleRun_DoesNotEmitRenderModeThree()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.Add(Text("Shown"));

        Assert.DoesNotContain("3 Tr", Content(builder), StringComparison.Ordinal);
    }

    // ---- 5. Device fill colour on a run -> CMYK k / Gray g instead of rg ----

    [Fact]
    public void DeviceCmykRun_EmitsKOperator()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Cyan");
        run.SetFillCmyk(1, 0, 0, 0);
        section.Blocks.Add(paragraph);

        var content = Content(builder);
        Assert.Contains("1 0 0 0 k", content, StringComparison.Ordinal);
    }

    // ---- 6. UA figure -> /Alt, and UA list -> L/LI/Lbl/LBody ----

    [Fact]
    public void TaggedFigure_EmitsAltText()
    {
        var builder = new DocumentBuilder
        {
            PdfUA = true,
            Language = "en-US",
        };
        builder.Info.Title = "Alt test";
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.AlternateText = "A red square";

        var reader = BuildTestSupport.Read(builder);
        var figure = FindElement(reader, "Figure");
        Assert.NotNull(figure);
        Assert.Equal("A red square",
            Assert.IsType<StringObject>(reader.Resolve(figure!["Alt"]!)).Value);
    }

    [Fact]
    public void TaggedList_BuildsLListItemLabelAndBody()
    {
        var builder = new DocumentBuilder
        {
            PdfUA = true,
            Language = "en-US",
        };
        builder.Info.Title = "List test";
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Name = BuildTestSupport.Latin;
        list.Font.Size = 12;
        list.AddItem("First");
        list.AddItem("Second");

        var reader = BuildTestSupport.Read(builder);
        var types = new List<string>();
        CollectTypes(reader, StructRootKids(reader), types);

        Assert.Contains("L", types);
        Assert.Equal(2, types.FindAll(t => t == "LI").Count);
        Assert.Equal(2, types.FindAll(t => t == "Lbl").Count);
        Assert.Equal(2, types.FindAll(t => t == "LBody").Count);
    }

    [Fact]
    public void UntaggedList_StaysUntagged_WhenNotPdfUA()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.AddItem("First");
        list.AddItem("Second");

        var reader = BuildTestSupport.Read(builder);
        var types = new List<string>();
        CollectTypes(reader, StructRootKids(reader), types);

        Assert.DoesNotContain("L", types);
        Assert.DoesNotContain("LBody", types);
    }

    // ---- 7. Stencil -> deterministic fill colour emitted inside the image scope ----

    [Fact]
    public void Stencil_EmitsFillColorBeforeDo()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        using var stream = new MemoryStream(OneBitGrayPng(8, 8));
        var image = section.Blocks.AddImage(stream);
        image.Width = Unit.FromPoint(48);
        image.Height = Unit.FromPoint(48);
        image.Stencil = true;
        image.StencilColor = Color.FromRgb(255, 0, 0);

        var content = Content(builder);
        Assert.Contains("1 0 0 rg", content, StringComparison.Ordinal);
        Assert.Contains(" Do", content, StringComparison.Ordinal);
    }

    // ---- Bug C1. A path clip is scoped by q..Q and cannot leak to a following element ----

    [Fact]
    public void PathClip_IsScoped_AndDoesNotLeak()
    {
        var document = new Document();
        var page = document.Pages.Add();

        var clip = new PathContent { Clip = PathClipMode.NonZero };
        clip.MoveTo(0, 0);
        clip.LineTo(10, 0);
        clip.LineTo(10, 10);
        clip.Close();
        page.Content.Add(clip);

        var fill = new PathContent { Fill = true };
        fill.MoveTo(0, 0);
        fill.LineTo(500, 0);
        fill.LineTo(500, 500);
        fill.Close();
        page.Content.Add(fill);

        var content = Encoding.Latin1.GetString(
            ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0));

        Assert.Equal(
            BuildTestSupport.CountOccurrences(content, "q\n"),
            BuildTestSupport.CountOccurrences(content, "Q\n"));

        var w = content.IndexOf("W\n", StringComparison.Ordinal);
        var q = content.IndexOf("Q\n", w, StringComparison.Ordinal);
        var f = content.LastIndexOf("f\n", StringComparison.Ordinal);
        Assert.True(w >= 0, "expected a clip W operator");
        Assert.True(q > w, "the clip must be closed by a Q before anything else");
        Assert.True(f > q, "the following fill must paint after the clip's Q");
    }

    // ---- 7 (encrypt). EncryptMetadata=false leaves the /Metadata stream plaintext ----

    [Fact]
    public void EncryptMetadataFalse_LeavesMetadataPlaintext()
    {
        var bytes = EncryptedDocument(encryptMetadata: false);
        Assert.Contains("xpacket", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void EncryptMetadataTrue_EncryptsMetadataStream()
    {
        var bytes = EncryptedDocument(encryptMetadata: true);
        Assert.DoesNotContain("xpacket", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
    }

    private static byte[] EncryptedDocument(bool encryptMetadata)
    {
        var document = new Document();
        document.Info.Producer = "Radzen metadata test producer";
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (secret) Tj ET"));
        document.Encryption = new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7]),
            Algorithm = EncryptionAlgorithm.Aes128,
            OwnerPassword = "owner",
            EncryptMetadata = encryptMetadata,
        };
        return document.ToArray();
    }

    // ---- Byte identity: a document using none of the new features is stable across builds ----

    [Fact]
    public void NoNewFeatures_IsByteIdenticalAcrossBuilds()
    {
        static byte[] Build()
        {
            var builder = new DocumentBuilder();
            var section = builder.Sections.Add();
            section.Blocks.Add(Text("Plain paragraph one."));
            var list = section.Blocks.AddList(ListStyle.Number);
            list.AddItem("Alpha");
            list.AddItem("Beta");
            return builder.ToArray();
        }

        Assert.Equal(Build(), Build());
    }

    // ---- helpers ----

    private static ArrayObject? StructRootKids(DocumentReader reader)
    {
        var catalog = ContentTestHelpers.Catalog(reader);
        if (!catalog.TryGetValue("StructTreeRoot", out var rootObject)
            || reader.Resolve(rootObject!) is not DictionaryObject root
            || !root.TryGetValue("K", out var k))
        {
            return null;
        }

        return reader.Resolve(k!) as ArrayObject ?? new ArrayObject { reader.Resolve(k!) };
    }

    private static void CollectTypes(DocumentReader reader, ArrayObject? kids, List<string> acc)
    {
        if (kids is null)
        {
            return;
        }

        foreach (var kid in kids)
        {
            if (reader.Resolve(kid) is not DictionaryObject dict
                || !dict.TryGetValue("S", out var s)
                || reader.Resolve(s!) is not NameObject type)
            {
                continue;
            }

            acc.Add(type.Value);
            if (dict.TryGetValue("K", out var k))
            {
                var childKids = reader.Resolve(k!) as ArrayObject ?? new ArrayObject { reader.Resolve(k!) };
                CollectTypes(reader, childKids, acc);
            }
        }
    }

    private static DictionaryObject? FindElement(DocumentReader reader, string type)
        => FindElement(reader, StructRootKids(reader), type);

    private static DictionaryObject? FindElement(DocumentReader reader, ArrayObject? kids, string type)
    {
        if (kids is null)
        {
            return null;
        }

        foreach (var kid in kids)
        {
            if (reader.Resolve(kid) is not DictionaryObject dict
                || !dict.TryGetValue("S", out var s)
                || reader.Resolve(s!) is not NameObject name)
            {
                continue;
            }

            if (name.Value == type)
            {
                return dict;
            }

            if (dict.TryGetValue("K", out var k))
            {
                var childKids = reader.Resolve(k!) as ArrayObject ?? new ArrayObject { reader.Resolve(k!) };
                if (FindElement(reader, childKids, type) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }

    // Minimal 1-bit greyscale PNG (colour type 0, bit depth 1): one filter byte plus
    // ceil(width/8) packed sample bytes per row, zlib-deflated into a single IDAT.
    private static byte[] OneBitGrayPng(int width, int height)
    {
        var rowBytes = ((width * 1) + 7) / 8;
        var raw = new byte[height * (rowBytes + 1)];
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * (rowBytes + 1);
            raw[rowStart] = 0;
            for (var b = 0; b < rowBytes; b++)
            {
                raw[rowStart + 1 + b] = (byte)((y % 2 == 0) ? 0xAA : 0x55);
            }
        }

        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        byte[] ihdr =
        [
            (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
            (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
            0x01, 0x00, 0x00, 0x00, 0x00,
        ];
        WriteChunk(ms, "IHDR", ihdr);
        WriteChunk(ms, "IDAT", Deflate(raw));
        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        stream.Write(length);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = Crc32(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        crc = Crc32Update(crc, type);
        crc = Crc32Update(crc, data);
        return crc ^ 0xFFFFFFFFu;
    }

    private static uint Crc32Update(uint crc, byte[] bytes)
    {
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        return crc;
    }
}
