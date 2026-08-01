#nullable enable
using System;
using System.Buffers.Binary;
using System.Text;
using Xunit;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 9.9 / OpenType OS/2 fsType: bit 1 (0x0002) RESTRICTED_LICENSE_EMBEDDING forbids embedding.
public class FontEmbedPermissionTests
{
    private static byte[] Liberation() => PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");

    private static int TableOffset(byte[] data, string tag)
    {
        var numTables = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(4));
        for (var i = 0; i < numTables; i++)
        {
            var rec = 12 + i * 16;
            var t = Encoding.ASCII.GetString(data, rec, 4);
            if (t == tag)
            {
                return (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(rec + 8));
            }
        }

        return -1;
    }

    private static byte[] WithFsType(byte[] data, ushort fsType)
    {
        var copy = (byte[])data.Clone();
        var os2 = TableOffset(copy, "OS/2");
        Assert.True(os2 >= 0, "OS/2 table present");
        BinaryPrimitives.WriteUInt16BigEndian(copy.AsSpan(os2 + 8), fsType);
        return copy;
    }

    [Fact]
    public void InstallableFont_IsNotRestricted()
    {
        var face = SfntFont.Parse(Liberation());
        Assert.Equal(0, face.FsType);
        Assert.False(FontEmbedding.EmbeddingRestricted(face));
        FontEmbedding.EnsureEmbeddable(face, allowRestricted: false);
    }

    [Fact]
    public void RestrictedLicenseBit_MakesFontRestricted()
    {
        var face = SfntFont.Parse(WithFsType(Liberation(), 0x0002));
        Assert.Equal(0x0002, face.FsType);
        Assert.True(FontEmbedding.EmbeddingRestricted(face));
    }

    [Fact]
    public void EnsureEmbeddable_ThrowsByDefaultForRestrictedFont()
    {
        var face = SfntFont.Parse(WithFsType(Liberation(), 0x0002));
        Assert.Throws<InvalidOperationException>(() => FontEmbedding.EnsureEmbeddable(face, allowRestricted: false));
    }

    [Fact]
    public void EnsureEmbeddable_OptInOverrideAllowsRestrictedFont()
    {
        var face = SfntFont.Parse(WithFsType(Liberation(), 0x0002));

        Assert.True(FontEmbedding.EmbeddingRestricted(face));
        FontEmbedding.EnsureEmbeddable(face, allowRestricted: true);
    }

    private static Document DocumentWith(byte[] font)
    {
        var document = new Document();
        document.Fonts.Register("Restricted", new System.IO.MemoryStream(font));
        var section = document.Sections.Add();
        section.Blocks.AddParagraph("Body").Font.Family = "Restricted";
        return document;
    }

    [Fact]
    public void RestrictedFont_RegistersButIsRejectedAtRender()
    {
        var document = DocumentWith(WithFsType(Liberation(), 0x0002));

        var error = Assert.Throws<InvalidOperationException>(
            () => new Radzen.Documents.Pdf.DocumentRenderer().Render(document));

        Assert.Contains("Restricted License Embedding", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RestrictedFont_IsEmbeddedWhenTheRendererAllowsIt()
    {
        var document = DocumentWith(WithFsType(Liberation(), 0x0002));
        var renderer = new Radzen.Documents.Pdf.DocumentRenderer { AllowRestrictedEmbedding = true };

        Assert.NotEmpty(renderer.ToArray(document));
    }

    [Fact]
    public void RegisteredButUnusedRestrictedFont_DoesNotBlockRendering()
    {
        var document = new Document();
        document.Fonts.Register("Restricted", new System.IO.MemoryStream(WithFsType(Liberation(), 0x0002)));
        var section = document.Sections.Add();
        section.Blocks.AddParagraph("Body drawn with a base-14 face");

        Assert.NotEmpty(new Radzen.Documents.Pdf.DocumentRenderer().ToArray(document));
    }

    [Fact]
    public void PreviewPrintAndEditableBits_AreNotRestricted()
    {
        Assert.False(FontEmbedding.EmbeddingRestricted(SfntFont.Parse(WithFsType(Liberation(), 0x0004))));
        Assert.False(FontEmbedding.EmbeddingRestricted(SfntFont.Parse(WithFsType(Liberation(), 0x0008))));
    }
}
