using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// ISO 32000-1 7.3 COS object serialization.
public class ObjectSerializationTests
{
    private static string Serialize(DocumentObject obj)
    {
        using var ms = new MemoryStream();
        obj.Write(ms);
        return Encoding.Latin1.GetString(ms.ToArray());
    }

    private static byte[] SerializeBytes(DocumentObject obj)
    {
        using var ms = new MemoryStream();
        obj.Write(ms);
        return ms.ToArray();
    }

    [Fact]
    public void Boolean_True_WritesTrue()
    {
        Assert.Equal("true", Serialize(new BooleanObject(true)));
    }

    [Fact]
    public void Boolean_False_WritesFalse()
    {
        Assert.Equal("false", Serialize(new BooleanObject(false)));
    }

    [Fact]
    public void Null_WritesNull()
    {
        Assert.Equal("null", Serialize(new NullObject()));
    }

    [Theory]
    [InlineData(42, "42")]
    [InlineData(-7, "-7")]
    [InlineData(0, "0")]
    [InlineData(1234567, "1234567")]
    public void Number_Integer_HasNoDecimalPoint(int value, string expected)
    {
        var text = Serialize(new NumberObject(value));
        Assert.Equal(expected, text);
        Assert.DoesNotContain(".", text);
    }

    [Theory]
    [InlineData(3.14, "3.14")]
    [InlineData(0.5, "0.5")]
    [InlineData(-0.002, "-0.002")]
    public void Number_Real_UsesInvariantDot(double value, string expected)
    {
        Assert.Equal(expected, Serialize(new NumberObject(value)));
    }

    [Fact]
    public void Number_Real_TrimsTrailingZeros()
    {
        Assert.Equal("1.5", Serialize(new NumberObject(1.50)));
    }

    [Fact]
    public void Number_WholeValuedReal_HasNoDecimalPoint()
    {
        Assert.Equal("2", Serialize(new NumberObject(2.0)));
    }

    [Fact]
    public void Number_Small_NeverUsesExponentNotation()
    {
        var text = Serialize(new NumberObject(0.0000001));
        Assert.DoesNotContain("e", text);
        Assert.DoesNotContain("E", text);
    }

    [Fact]
    public void Reference_WritesObjectGenerationR()
    {
        Assert.Equal("3 0 R", Serialize(new ReferenceObject(3, 0)));
    }

    [Fact]
    public void Reference_NonZeroGeneration()
    {
        Assert.Equal("12 5 R", Serialize(new ReferenceObject(12, 5)));
    }

    [Fact]
    public void Array_Integers_SingleSpaceSeparated()
    {
        var array = new ArrayObject();
        array.Add(new NumberObject(1));
        array.Add(new NumberObject(2));
        array.Add(new NumberObject(3));
        Assert.Equal("[1 2 3]", Serialize(array));
    }

    [Fact]
    public void Array_Empty_WritesBrackets()
    {
        Assert.Equal("[]", Serialize(new ArrayObject()));
    }

    [Fact]
    public void Array_MixedTypes()
    {
        var array = new ArrayObject();
        array.Add(new NameObject("N"));
        array.Add(new StringObject("s"));
        array.Add(new NumberObject(4));
        array.Add(new BooleanObject(true));
        array.Add(new NullObject());
        Assert.Equal("[/N (s) 4 true null]", Serialize(array));
    }

    [Fact]
    public void Array_Nested()
    {
        var inner = new ArrayObject();
        inner.Add(new NumberObject(1));
        inner.Add(new NumberObject(2));
        var outer = new ArrayObject();
        outer.Add(inner);
        outer.Add(new NumberObject(3));
        Assert.Equal("[[1 2] 3]", Serialize(outer));
    }

    [Fact]
    public void Dictionary_SingleEntry()
    {
        var dict = new DictionaryObject();
        dict["Type"] = new NameObject("Catalog");
        Assert.Equal("<< /Type /Catalog >>", Serialize(dict));
    }

    [Fact]
    public void Dictionary_PreservesInsertionOrder()
    {
        var dict = new DictionaryObject();
        dict["Type"] = new NameObject("Pages");
        dict["Count"] = new NumberObject(1);
        Assert.Equal("<< /Type /Pages /Count 1 >>", Serialize(dict));
    }

    [Fact]
    public void Dictionary_ReferenceValue_WritesAsIndirectReference()
    {
        var dict = new DictionaryObject();
        dict["Pages"] = new ReferenceObject(3, 0);
        Assert.Equal("<< /Pages 3 0 R >>", Serialize(dict));
    }

    [Fact]
    public void Dictionary_Nested()
    {
        var resources = new DictionaryObject();
        resources["Font"] = new NameObject("Helvetica");
        var dict = new DictionaryObject();
        dict["Resources"] = resources;
        Assert.Equal("<< /Resources << /Font /Helvetica >> >>", Serialize(dict));
    }

    [Fact]
    public void Stream_ExactBytes_WithAutomaticLength()
    {
        var data = Encoding.Latin1.GetBytes("Hello");
        var expected = Encoding.Latin1.GetBytes("<< /Length 5 >>\nstream\nHello\nendstream");
        Assert.Equal(expected, SerializeBytes(new StreamObject(data)));
    }

    [Fact]
    public void Stream_LengthEqualsDataByteCount()
    {
        var data = Encoding.Latin1.GetBytes("some longer payload");
        var text = Serialize(new StreamObject(data));
        Assert.Contains($"/Length {data.Length}", text);
    }

    [Fact]
    public void Stream_ExtraDictionaryEntries_ArePresent()
    {
        var stream = new StreamObject(Encoding.Latin1.GetBytes("data!"));
        stream.Dictionary["Filter"] = new NameObject("FlateDecode");
        var text = Serialize(stream);
        Assert.Contains("/Length 5", text);
        Assert.Contains("/Filter /FlateDecode", text);
    }
}
