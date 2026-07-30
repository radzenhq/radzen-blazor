#nullable enable
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// In-use cross-reference entry per ISO 32000-1 7.5.4
public class XrefEntryWriterTests
{
    private static string Write(long offset, int? generation = null)
    {
        using var buffer = new MemoryStream();
        if (generation is null)
        {
            PdfBytes.WriteXrefEntry(buffer, offset);
        }
        else
        {
            PdfBytes.WriteXrefEntry(buffer, offset, generation.Value);
        }

        return Encoding.ASCII.GetString(buffer.ToArray());
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(9L)]
    [InlineData(1234567890L)]
    [InlineData(9999999999L)]
    public void DefaultGeneration_MatchesTheLegacyHardcodedEntryByteForByte(long offset)
    {
        var legacy = offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n";

        Assert.Equal(legacy, Write(offset));
        Assert.Equal(legacy, Write(offset, 0));
    }

    [Theory]
    [InlineData(0, "00000")]
    [InlineData(1, "00001")]
    [InlineData(255, "00255")]
    [InlineData(65535, "65535")]
    public void GenerationIsWrittenAsFiveDigits(int generation, string expected)
    {
        Assert.Equal("0000000042 " + expected + " n \n", Write(42, generation));
    }

    [Theory]
    [InlineData(0L, 0)]
    [InlineData(9999999999L, 65535)]
    public void EntryIsAlwaysTwentyBytes(long offset, int generation)
    {
        Assert.Equal(20, Write(offset, generation).Length);
    }
}
