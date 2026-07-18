using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// ISO 32000-1 7.3 object grammar.
public class ObjectParserTests
{
    private static DocumentObject Parse(string text)
        => ObjectParser.Parse(Encoding.Latin1.GetBytes(text), 0);

    [Fact]
    public void Integer()
    {
        var number = Assert.IsType<NumberObject>(Parse("42"));
        Assert.True(number.IsInteger);
        Assert.Equal(42, number.IntValue);
    }

    [Fact]
    public void Real()
    {
        var number = Assert.IsType<NumberObject>(Parse("2.5"));
        Assert.False(number.IsInteger);
        Assert.Equal(2.5, number.DoubleValue, 10);
    }

    [Fact]
    public void BooleanTrue()
    {
        Assert.True(Assert.IsType<BooleanObject>(Parse("true")).Value);
    }

    [Fact]
    public void BooleanFalse()
    {
        Assert.False(Assert.IsType<BooleanObject>(Parse("false")).Value);
    }

    [Fact]
    public void Null()
    {
        Assert.IsType<NullObject>(Parse("null"));
    }

    [Fact]
    public void Name()
    {
        Assert.Equal("Type", Assert.IsType<NameObject>(Parse("/Type")).Value);
    }

    [Fact]
    public void StringLiteral()
    {
        Assert.Equal("hi", Assert.IsType<StringObject>(Parse("(hi)")).Value);
    }

    [Fact]
    public void EmptyString()
    {
        Assert.Equal("", Assert.IsType<StringObject>(Parse("()")).Value);
    }

    [Fact]
    public void HexString()
    {
        Assert.Equal("Hello", Assert.IsType<StringObject>(Parse("<48656C6C6F>")).Value);
    }

    [Fact]
    public void EmptyArray()
    {
        Assert.Equal(0, Assert.IsType<ArrayObject>(Parse("[]")).Count);
    }

    [Fact]
    public void EmptyDictionary()
    {
        Assert.Equal(0, Assert.IsType<DictionaryObject>(Parse("<< >>")).Count);
    }

    [Fact]
    public void Array_MixedElements()
    {
        var array = Assert.IsType<ArrayObject>(Parse("[ 1 2.5 (x) /N true null ]"));
        Assert.Equal(6, array.Count);
        Assert.Equal(1, Assert.IsType<NumberObject>(array[0]).IntValue);
        Assert.Equal(2.5, Assert.IsType<NumberObject>(array[1]).DoubleValue, 10);
        Assert.Equal("x", Assert.IsType<StringObject>(array[2]).Value);
        Assert.Equal("N", Assert.IsType<NameObject>(array[3]).Value);
        Assert.True(Assert.IsType<BooleanObject>(array[4]).Value);
        Assert.IsType<NullObject>(array[5]);
    }

    [Fact]
    public void Dictionary_KeysAndValues()
    {
        var dict = Assert.IsType<DictionaryObject>(Parse("<< /Type /Page /Count 3 >>"));
        Assert.Equal(2, dict.Count);
        Assert.Equal("Page", Assert.IsType<NameObject>(dict["Type"]).Value);
        Assert.Equal(3, Assert.IsType<NumberObject>(dict["Count"]).IntValue);
    }

    [Fact]
    public void Dictionary_PreservesInsertionOrder()
    {
        var dict = Assert.IsType<DictionaryObject>(Parse("<< /B 1 /A 2 /C 3 >>"));
        Assert.Equal(new[] { "B", "A", "C" }, dict.Keys);
    }

    [Fact]
    public void Dictionary_ReferenceValue()
    {
        var dict = Assert.IsType<DictionaryObject>(Parse("<< /Kids 3 0 R >>"));
        var reference = Assert.IsType<ReferenceObject>(dict["Kids"]);
        Assert.Equal(3, reference.ObjectNumber);
        Assert.Equal(0, reference.Generation);
    }

    [Fact]
    public void Reference_WithGeneration_RoundTripsGeneration()
    {
        var reference = Assert.IsType<ReferenceObject>(Parse("7 3 R"));
        Assert.Equal(7, reference.ObjectNumber);
        Assert.Equal(3, reference.Generation);
    }

    [Fact]
    public void Array_TrailingIntegerIsNumberNotReference()
    {
        var array = Assert.IsType<ArrayObject>(Parse("[ 1 2 ]"));
        Assert.Equal(2, array.Count);
        Assert.Equal(1, Assert.IsType<NumberObject>(array[0]).IntValue);
        Assert.Equal(2, Assert.IsType<NumberObject>(array[1]).IntValue);
    }

    [Fact]
    public void NestedGraph()
    {
        var root = Assert.IsType<DictionaryObject>(
            Parse("<< /A [ 1 [ 2 3 ] << /K (v) >> ] /B << /C 9 0 R >> >>"));
        var a = Assert.IsType<ArrayObject>(root["A"]);
        Assert.Equal(3, a.Count);
        var inner = Assert.IsType<ArrayObject>(a[1]);
        Assert.Equal(2, Assert.IsType<NumberObject>(inner[0]).IntValue);
        Assert.Equal(3, Assert.IsType<NumberObject>(inner[1]).IntValue);
        var innerDict = Assert.IsType<DictionaryObject>(a[2]);
        Assert.Equal("v", Assert.IsType<StringObject>(innerDict["K"]).Value);
        var b = Assert.IsType<DictionaryObject>(root["B"]);
        Assert.Equal(9, Assert.IsType<ReferenceObject>(b["C"]).ObjectNumber);
    }

    [Fact]
    public void Reference_ObjectNumberBeyondInt32_DoesNotWrapToReference()
    {
        Assert.IsType<NullObject>(Parse("4294967297 0 R"));
    }

    [Fact]
    public void Reference_GenerationBeyondInt32_DoesNotWrapToReference()
    {
        Assert.IsType<NullObject>(Parse("7 4294967297 R"));
    }

    [Fact]
    public void Malformed_UnbalancedDictionary_Throws()
    {
        Assert.Throws<DocumentParseException>(() => Parse("<< /A 1"));
    }

    [Fact]
    public void Malformed_LoneGreaterThan_Throws()
    {
        Assert.Throws<DocumentParseException>(() => Parse(">"));
    }

    [Fact]
    public void Malformed_NumberWithTwoDots_Throws()
    {
        Assert.Throws<DocumentParseException>(() => Parse("1.2.3"));
    }

    [Fact]
    public void Malformed_UnterminatedString_Throws()
    {
        Assert.Throws<DocumentParseException>(() => Parse("(abc"));
    }

    [Fact]
    public void Malformed_UnterminatedHexString_Throws()
    {
        Assert.Throws<DocumentParseException>(() => Parse("<4869"));
    }

    [Fact]
    public void Malformed_UnbalancedArray_Throws()
    {
        Assert.Throws<DocumentParseException>(() => Parse("[ 1 2"));
    }

    [Fact]
    public void ParseException_CarriesByteOffset()
    {
        var exception = Assert.Throws<DocumentParseException>(() => Parse("<< /A 1"));
        Assert.True(exception.Offset >= 0);
    }
}
