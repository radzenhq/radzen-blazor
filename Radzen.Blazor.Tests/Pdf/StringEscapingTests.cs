using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// StringObject literal escaping contract (ISO 32000-1 section 7.3.4.2).
// Our contract: escape both parens, the backslash, the named control
// escapes, and emit 3-digit octal for any byte outside printable ASCII.
public class StringEscapingTests
{
    private static string Serialize(string value)
    {
        using var ms = new MemoryStream();
        new StringObject(value).Write(ms);
        return Encoding.Latin1.GetString(ms.ToArray());
    }

    [Fact]
    public void Plain_WrappedInParens()
    {
        Assert.Equal("(Hello)", Serialize("Hello"));
    }

    [Fact]
    public void Empty_WritesEmptyParens()
    {
        Assert.Equal("()", Serialize(""));
    }

    [Fact]
    public void Backslash_Escaped()
    {
        Assert.Equal("(a\\\\b)", Serialize("a\\b"));
    }

    [Fact]
    public void OpenParen_Escaped()
    {
        Assert.Equal("(\\()", Serialize("("));
    }

    [Fact]
    public void CloseParen_Escaped()
    {
        Assert.Equal("(\\))", Serialize(")"));
    }

    [Fact]
    public void BalancedParens_BothEscaped()
    {
        Assert.Equal("(a\\(b\\)c)", Serialize("a(b)c"));
    }

    [Fact]
    public void Newline_EscapedAsBackslashN()
    {
        Assert.Equal("(\\n)", Serialize("\n"));
    }

    [Fact]
    public void CarriageReturn_EscapedAsBackslashR()
    {
        Assert.Equal("(\\r)", Serialize("\r"));
    }

    [Fact]
    public void Tab_EscapedAsBackslashT()
    {
        Assert.Equal("(\\t)", Serialize("\t"));
    }

    [Fact]
    public void Backspace_EscapedAsBackslashB()
    {
        Assert.Equal("(\\b)", Serialize("\b"));
    }

    [Fact]
    public void FormFeed_EscapedAsBackslashF()
    {
        Assert.Equal("(\\f)", Serialize("\f"));
    }

    [Fact]
    public void Null_EscapedAsThreeDigitOctal()
    {
        Assert.Equal("(\\000)", Serialize("\u0000"));
    }

    [Fact]
    public void Bell_EscapedAsThreeDigitOctal()
    {
        Assert.Equal("(\\007)", Serialize("\u0007"));
    }

    [Fact]
    public void Delete_EscapedAsThreeDigitOctal()
    {
        Assert.Equal("(\\177)", Serialize("\u007F"));
    }

    [Fact]
    public void HighByte_EscapedAsThreeDigitOctal()
    {
        Assert.Equal("(\\251)", Serialize("\u00A9"));
    }

    [Fact]
    public void PrintableAscii_LeftLiteral()
    {
        Assert.Equal("(Radzen 1.7!)", Serialize("Radzen 1.7!"));
    }
}
