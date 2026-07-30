#nullable enable
using System;
using System.Text;
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

    private static byte[] InvokeToArray(ContentWriter writer)
        => (byte[])typeof(ContentWriter)
            .GetMethod("ToArray", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(writer, null)!;
}
