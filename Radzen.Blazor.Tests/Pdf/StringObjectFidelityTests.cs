#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// PDF text string: UTF-16BE or UTF-8 with BOM (ISO 32000-2 7.9.2.2).
public class StringObjectFidelityTests
{
    private static string RoundTrip(string value)
    {
        using var stream = new MemoryStream();
        new StringObject(value).Write(stream);
        var parsed = Assert.IsType<StringObject>(ObjectParser.Parse(stream.ToArray(), 0));
        return DecodeTextString(parsed.Value);
    }

    private static string DecodeTextString(string raw)
    {
        var bytes = new byte[raw.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            bytes[i] = (byte)raw[i];
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        return raw;
    }

    [Theory]
    [InlineData("Фактура №5")]
    [InlineData("총 금액")]
    [InlineData("€ 1250,00")]
    public void CharactersAboveLatin1_SurviveWriteParseRoundTrip(string value)
    {
        Assert.Equal(value, RoundTrip(value));
    }

    [Fact]
    public void Latin1Characters_RemainByteExact()
    {
        Assert.Equal("Café (a\\b)\t", RoundTrip("Café (a\\b)\t"));
    }
}
