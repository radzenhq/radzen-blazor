#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class LiteralStringEscapingAgreementTests
{
    private static string ContentLiteral(byte[] bytes)
    {
        using var writer = new ContentWriter();
        writer.WriteString(bytes);
        return Encoding.Latin1.GetString(writer.ToArray());
    }

    private static string ObjectLiteral(string value)
    {
        using var stream = new MemoryStream();
        new StringObject(value).Write(stream);
        return Encoding.Latin1.GetString(stream.ToArray());
    }

    private static byte[] Bytes(string latin1) => latin1.Select(c => (byte)c).ToArray();

    // PDF 32000-1 7.3.4.2 literal strings.
    [Theory]
    [InlineData("", "()")]
    [InlineData("Text and /slash [x]{y}<z>", "(Text and /slash [x]{y}<z>)")]
    [InlineData("a\\b", "(a\\\\b)")]
    [InlineData("(", "(\\()")]
    [InlineData(")", "(\\))")]
    [InlineData("a(b)c", "(a\\(b\\)c)")]
    [InlineData("\n", "(\\n)")]
    [InlineData("\r", "(\\r)")]
    [InlineData("\t", "(\\t)")]
    [InlineData("\b", "(\\b)")]
    [InlineData("\f", "(\\f)")]
    [InlineData("\0", "(\\000)")]
    [InlineData("\u001F", "(\\037)")]
    [InlineData("\u007F", "(\\177)")]
    [InlineData("\u0080\u00C3\u00FF", "(\\200\\303\\377)")]
    public void ObjectLiteral_EscapesPerTheSpecification(string value, string expected)
    {
        Assert.Equal(expected, ObjectLiteral(value));
    }

    // PDF 32000-1 7.3.4.2 literal strings.
    [Theory]
    [InlineData("", "()")]
    [InlineData("Text and /slash [x]{y}<z>", "(Text and /slash [x]{y}<z>)")]
    [InlineData("a\\b", "(a\\\\b)")]
    [InlineData("(", "(\\()")]
    [InlineData(")", "(\\))")]
    [InlineData("a(b)c", "(a\\(b\\)c)")]
    [InlineData("\n", "(\\012)")]
    [InlineData("\r", "(\\015)")]
    [InlineData("\t", "(\\011)")]
    [InlineData("\b", "(\\010)")]
    [InlineData("\f", "(\\014)")]
    [InlineData("\0", "(\\000)")]
    [InlineData("\u001F", "(\\037)")]
    [InlineData("\u007F", "(\\177)")]
    [InlineData("\u0080\u00C3\u00FF", "(\u0080\u00C3\u00FF)")]
    public void ContentStreamLiteral_EscapesPerTheSpecification(string value, string expected)
    {
        Assert.Equal(expected, ContentLiteral(Bytes(value)));
    }

    [Fact]
    public void ContentStreamAndObjectLiterals_AgreeOnPrintableAsciiIncludingDelimiters()
    {
        const string value = "Text (with) parens \\ and /slash [x]{y}<z>";
        Assert.Equal(ObjectLiteral(value), ContentLiteral(Bytes(value)));
    }
}
