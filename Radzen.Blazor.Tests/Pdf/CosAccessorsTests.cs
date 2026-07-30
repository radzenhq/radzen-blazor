#nullable enable
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CosAccessorsTests
{
    private static DocumentReader Reader()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Name /Helvetica /Str (hello) /Int 42 /Real 3.5 /Flag true "
                + "/Dict << /K 1 >> /Arr [1 2] /Ref 2 0 R /Dangling 9 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n/Indirect\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Catalog >>\nendobj\n");
        var count = 4;
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 3 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return DocumentReader.Parse(pdf.ToArray());
    }

    private static DictionaryObject Dict(DocumentReader reader) => (DictionaryObject)reader.GetObject(1);

    [Fact]
    public void GetName_ReturnsValue()
    {
        var reader = Reader();
        Assert.Equal("Helvetica", reader.GetName(Dict(reader), "Name"));
    }

    [Fact]
    public void GetString_ReturnsValue()
    {
        var reader = Reader();
        Assert.Equal("hello", reader.GetString(Dict(reader), "Str"));
    }

    [Fact]
    public void GetInt_And_GetNumber_ReturnValue()
    {
        var reader = Reader();
        Assert.Equal(42, reader.GetInt(Dict(reader), "Int"));
        Assert.Equal(3.5, reader.GetNumber(Dict(reader), "Real"));
    }

    [Fact]
    public void GetBool_ReturnsValue()
    {
        var reader = Reader();
        Assert.True(reader.GetBool(Dict(reader), "Flag"));
    }

    [Fact]
    public void GetDictionary_And_GetArray_ReturnObjects()
    {
        var reader = Reader();
        Assert.NotNull(reader.GetDictionary(Dict(reader), "Dict"));
        Assert.Equal(2, reader.GetArray(Dict(reader), "Arr")!.Count);
    }

    [Fact]
    public void Get_ResolvesIndirectReference()
    {
        var reader = Reader();
        Assert.Equal("Indirect", reader.GetName(Dict(reader), "Ref"));
    }

    [Fact]
    public void Get_MissingKey_ReturnsNull()
    {
        var reader = Reader();
        Assert.Null(reader.GetName(Dict(reader), "Absent"));
        Assert.Null(reader.GetInt(Dict(reader), "Absent"));
        Assert.Null(reader.GetDictionary(Dict(reader), "Absent"));
    }

    [Fact]
    public void Get_WrongType_ReturnsNull()
    {
        var reader = Reader();
        Assert.Null(reader.GetInt(Dict(reader), "Name"));
        Assert.Null(reader.GetString(Dict(reader), "Int"));
        Assert.Null(reader.GetArray(Dict(reader), "Dict"));
    }

    [Fact]
    public void Get_DanglingReference_ResolvesToNull()
    {
        var reader = Reader();
        Assert.Null(reader.GetName(Dict(reader), "Dangling"));
        Assert.Null(reader.GetDictionary(Dict(reader), "Dangling"));
    }

    [Fact]
    public void TryGet_PresentAndTyped_BindsValue()
    {
        var reader = Reader();
        Assert.True(reader.TryGet<NameObject>(Dict(reader), "Name", out var name));
        Assert.Equal("Helvetica", name!.Value);
    }

    [Fact]
    public void TryGet_MissingOrWrongType_ReturnsFalse()
    {
        var reader = Reader();
        Assert.False(reader.TryGet<ArrayObject>(Dict(reader), "Name", out var wrong));
        Assert.Null(wrong);
        Assert.False(reader.TryGet<NameObject>(Dict(reader), "Absent", out _));
    }

    [Fact]
    public void As_ResolvesAndTypeChecksArbitraryValue()
    {
        var reader = Reader();
        var dict = Dict(reader);
        Assert.Equal("Indirect", reader.AsName(dict["Ref"]));
        Assert.Null(reader.AsName(dict["Int"]));
        Assert.NotNull(reader.AsArray(dict["Arr"]));
    }
}
