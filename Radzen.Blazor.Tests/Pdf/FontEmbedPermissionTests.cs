#nullable enable
using System;
using System.Buffers.Binary;
using System.Text;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

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
        Assert.False(face.EmbeddingRestricted);
        face.EnsureEmbeddable(allowRestricted: false);
    }

    [Fact]
    public void RestrictedLicenseBit_MakesFontRestricted()
    {
        var face = SfntFont.Parse(WithFsType(Liberation(), 0x0002));
        Assert.Equal(0x0002, face.FsType);
        Assert.True(face.EmbeddingRestricted);
    }

    [Fact]
    public void EnsureEmbeddable_ThrowsByDefaultForRestrictedFont()
    {
        var face = SfntFont.Parse(WithFsType(Liberation(), 0x0002));
        Assert.Throws<InvalidOperationException>(() => face.EnsureEmbeddable(allowRestricted: false));
    }

    [Fact]
    public void EnsureEmbeddable_OptInOverrideAllowsRestrictedFont()
    {
        var face = SfntFont.Parse(WithFsType(Liberation(), 0x0002));
        face.EnsureEmbeddable(allowRestricted: true);
    }

    [Fact]
    public void PreviewPrintAndEditableBits_AreNotRestricted()
    {
        Assert.False(SfntFont.Parse(WithFsType(Liberation(), 0x0004)).EmbeddingRestricted);
        Assert.False(SfntFont.Parse(WithFsType(Liberation(), 0x0008)).EmbeddingRestricted);
    }
}
