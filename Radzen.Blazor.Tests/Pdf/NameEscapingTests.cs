using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// ISO 32000-1 7.3.5 name object escaping.
public class NameEscapingTests
{
    private static string Serialize(string name)
    {
        using var ms = new MemoryStream();
        new NameObject(name).Write(ms);
        return Encoding.Latin1.GetString(ms.ToArray());
    }

    [Fact]
    public void Simple_PrefixedWithSlash()
    {
        Assert.Equal("/Name", Serialize("Name"));
    }

    [Fact]
    public void Empty_WritesLoneSlash()
    {
        Assert.Equal("/", Serialize(""));
    }

    [Fact]
    public void Space_EscapedAsHex()
    {
        Assert.Equal("/A#20B", Serialize("A B"));
    }

    [Fact]
    public void NumberSign_EscapedAsHex()
    {
        Assert.Equal("/A#23B", Serialize("A#B"));
    }

    [Fact]
    public void OpenParen_EscapedAsHex()
    {
        Assert.Equal("/#28", Serialize("("));
    }

    [Fact]
    public void Slash_EscapedAsHex()
    {
        Assert.Equal("/a#2Fb", Serialize("a/b"));
    }

    [Fact]
    public void Tab_EscapedAsHex()
    {
        Assert.Equal("/A#09B", Serialize("A\tB"));
    }

    [Fact]
    public void Delete_EscapedAsHex()
    {
        Assert.Equal("/x#7F", Serialize("x\u007F"));
    }

    [Fact]
    public void HighByte_EscapedAsUppercaseHex()
    {
        Assert.Equal("/#A9", Serialize("\u00A9"));
    }

    [Fact]
    public void RegularCharacters_NotEscaped()
    {
        Assert.Equal("/Type1-Bold.007", Serialize("Type1-Bold.007"));
    }

    [Fact]
    public void HighByteBoundary_EscapedNotAliased()
    {
        Assert.Equal("/#80#FF", Serialize("\u0080\u00FF"));
    }

    [Fact]
    public void CodePointAboveLatin1_Throws()
    {
        Assert.Throws<NotSupportedException>(() => Serialize("A\u0141"));
    }

    [Fact]
    public void CodePointAboveLatin1_AfterEscapableChar_Throws()
    {
        Assert.Throws<NotSupportedException>(() => Serialize(" \u0141"));
    }
}
