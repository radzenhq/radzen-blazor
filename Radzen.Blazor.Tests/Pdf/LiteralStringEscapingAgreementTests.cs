#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class LiteralStringEscapingAgreementTests
{
    private static byte[] ContentLiteral(byte[] bytes)
    {
        using var writer = new ContentWriter();
        writer.WriteString(bytes);
        return writer.ToArray();
    }

    private static byte[] ObjectLiteral(string value)
    {
        using var stream = new MemoryStream();
        new StringObject(value).Write(stream);
        return stream.ToArray();
    }

    private static string Latin1(byte[] bytes) => new(bytes.Select(b => (char)b).ToArray());

    [Fact]
    public void ContentStreamAndObjectLiterals_AgreeOnPrintableAsciiIncludingDelimiters()
    {
        const string value = "Text (with) parens \\ and /slash [x]{y}<z>";
        Assert.Equal(ObjectLiteral(value), ContentLiteral(Encoding.Latin1.GetBytes(value)));
    }

    [Theory]
    [InlineData((byte)'(')]
    [InlineData((byte)')')]
    [InlineData((byte)'\\')]
    [InlineData((byte)0x7F)]
    [InlineData((byte)0x1F)]
    [InlineData((byte)0x00)]
    public void ContentStreamAndObjectLiterals_AgreeOnEscapedControlAndDelimiterBytes(byte value)
    {
        var bytes = new[] { value };
        Assert.Equal(ObjectLiteral(Latin1(bytes)), ContentLiteral(bytes));
    }

    [Fact]
    public void BinaryFlag_KeepsHighBytesRaw_WhileTextModeOctalEscapesThem()
    {
        var bytes = new byte[] { 0x80, 0xC3, 0xFF };

        Assert.Equal(new byte[] { (byte)'(', 0x80, 0xC3, 0xFF, (byte)')' }, ContentLiteral(bytes));
        Assert.Equal("(\\200\\303\\377)", Encoding.Latin1.GetString(ObjectLiteral(Latin1(bytes))));
    }

    [Fact]
    public void SharedEscaper_UsesNamedControlEscapesOnlyInTextMode()
    {
        var bytes = new byte[] { (byte)'\n', (byte)'\r', (byte)'\t' };

        Assert.Equal("(\\n\\r\\t)", Encoding.Latin1.GetString(ObjectLiteral(Latin1(bytes))));
        Assert.Equal("(\\012\\015\\011)", Encoding.Latin1.GetString(ContentLiteral(bytes)));
    }
}
