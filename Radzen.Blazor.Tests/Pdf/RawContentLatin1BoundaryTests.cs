#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class RawContentLatin1BoundaryTests
{
    [Fact]
    public void WriteRaw_CharAtLatin1Ceiling_EmitsThatByte()
    {
        using var writer = new ContentWriter();
        writer.WriteRaw("ÿ");

        Assert.Equal(new byte[] { 0xFF }, InvokeToArray(writer));
    }

    [Fact]
    public void WriteRaw_CharAboveLatin1_ThrowsInsteadOfTruncating()
    {
        using var writer = new ContentWriter();

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteRaw("Ā"));
    }

    [Fact]
    public void XObjectName_AboveLatin1_ThrowsOnSave()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new XObjectContent("Ł1"));

        Assert.Throws<NotSupportedException>(() => document.ToArray());
    }

    [Fact]
    public void XObjectName_HighLatin1_StillEncodesAsEscape()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new XObjectContent("©x"));

        var bytes = document.ToArray();
        Assert.Contains("/#A9x Do", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
    }

    private static byte[] InvokeToArray(ContentWriter writer)
        => (byte[])typeof(ContentWriter)
            .GetMethod("ToArray", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(writer, null)!;
}
