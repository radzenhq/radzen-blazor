#nullable enable

using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TextContentTests
{
    private static ContentOperation Find(byte[] content, string op)
    {
        foreach (var operation in ContentStreamTokenizer.Parse(content))
        {
            if (operation.Operator == op)
            {
                return operation;
            }
        }

        throw new Xunit.Sdk.XunitException($"operator '{op}' not found");
    }

    private static byte[] WinAnsi(string text)
    {
        var bytes = new List<byte>();
        foreach (var c in text)
        {
            Assert.True(WinAnsiEncoding.TryGetCode(c, out var code), $"cannot encode U+{(int)c:X4}");
            bytes.Add(code);
        }

        return [.. bytes];
    }

    [Fact]
    public void Text_EmitsBtTfTjEtInOrder()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hello", 72, 700));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var operators = ContentStreamTokenizer.Operators(content);

        var bt = operators.IndexOf("BT");
        var tf = operators.IndexOf("Tf");
        var tj = operators.IndexOf("Tj");
        var et = operators.IndexOf("ET");

        Assert.True(bt >= 0);
        Assert.True(bt < tf);
        Assert.True(tf < tj);
        Assert.True(tj < et);
    }

    [Fact]
    public void Tf_CarriesResourceNameAndSize()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Name = "Helvetica", Size = 18 } });

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var tf = Find(content, "Tf");

        Assert.Equal(2, tf.Operands.Count);
        Assert.Equal(ContentTokenKind.Name, tf.Operands[0].Kind);
        Assert.Equal(18, tf.Operands[1].Number, 3);
    }

    [Fact]
    public void FontResource_IsBase14Type1Helvetica()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Name = "Helvetica" } });

        var reader = ContentTestHelpers.Reload(document);
        var content = ContentTestHelpers.PageContent(reader, 0);
        var name = Find(content, "Tf").Operands[0].Text;

        var font = ContentTestHelpers.FontResource(reader, 0, name);
        Assert.Equal("Font", Assert.IsType<NameObject>(font["Type"]).Value);
        Assert.Equal("Type1", Assert.IsType<NameObject>(font["Subtype"]).Value);
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(font["BaseFont"]).Value);

        var encoding = Assert.IsType<NameObject>(reader.Resolve(font["Encoding"]));
        Assert.Equal("WinAnsiEncoding", encoding.Value);
    }

    [Fact]
    public void Bold_MapsToHelveticaBoldBaseFont()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Name = "Helvetica", Bold = true } });

        var reader = ContentTestHelpers.Reload(document);
        var content = ContentTestHelpers.PageContent(reader, 0);
        var name = Find(content, "Tf").Operands[0].Text;

        var font = ContentTestHelpers.FontResource(reader, 0, name);
        Assert.Equal("Helvetica-Bold", Assert.IsType<NameObject>(font["BaseFont"]).Value);
    }

    [Fact]
    public void Times_MapsToTimesRomanBaseFont()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Name = "Times" } });

        var reader = ContentTestHelpers.Reload(document);
        var content = ContentTestHelpers.PageContent(reader, 0);
        var name = Find(content, "Tf").Operands[0].Text;

        var font = ContentTestHelpers.FontResource(reader, 0, name);
        Assert.Equal("Times-Roman", Assert.IsType<NameObject>(font["BaseFont"]).Value);
    }

    [Fact]
    public void Courier_MapsToCourierBaseFont()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Name = "Courier" } });

        var reader = ContentTestHelpers.Reload(document);
        var content = ContentTestHelpers.PageContent(reader, 0);
        var name = Find(content, "Tf").Operands[0].Text;

        var font = ContentTestHelpers.FontResource(reader, 0, name);
        Assert.Equal("Courier", Assert.IsType<NameObject>(font["BaseFont"]).Value);
    }

    [Fact]
    public void TwoDistinctFonts_ProduceTwoResources()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("a", 0, 0) { Font = new Font { Name = "Helvetica" } });
        page.Content.Add(new TextContent("b", 0, 0) { Font = new Font { Name = "Times" } });

        var reader = ContentTestHelpers.Reload(document);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(ContentTestHelpers.Kid(reader, 0)["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));

        var baseFonts = new List<string>();
        foreach (var key in fonts.Keys)
        {
            var font = Assert.IsType<DictionaryObject>(reader.Resolve(fonts[key]));
            baseFonts.Add(Assert.IsType<NameObject>(font["BaseFont"]).Value);
        }

        Assert.Contains("Helvetica", baseFonts);
        Assert.Contains("Times-Roman", baseFonts);
    }

    [Fact]
    public void Text_IsWinAnsiEncoded()
    {
        var document = new Document();
        var page = document.Pages.Add();
        var text = "A\u20AC\u2013z"; // A, euro, en-dash, z - distinct in WinAnsi vs UTF
        page.Content.Add(new TextContent(text, 0, 0));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var tj = Find(content, "Tj");

        Assert.Single(tj.Operands);
        Assert.Equal(ContentTokenKind.String, tj.Operands[0].Kind);
        Assert.Equal(WinAnsi(text), tj.Operands[0].Bytes);
        Assert.Equal([0x41, 0x80, 0x96, 0x7A], tj.Operands[0].Bytes);
    }

    [Fact]
    public void AsciiText_RoundTripsAsWinAnsiBytes()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hello", 0, 0));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var tj = Find(content, "Tj");

        Assert.Equal(WinAnsi("Hello"), tj.Operands[0].Bytes);
    }

    [Fact]
    public void TextPosition_XAndYEmitted()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("P", 72, 700));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        double x, y;
        var operators = ContentStreamTokenizer.Operators(content);
        if (operators.Contains("Tm"))
        {
            var tm = Find(content, "Tm");
            x = tm.Num(4);
            y = tm.Num(5);
        }
        else
        {
            var td = Find(content, "Td");
            x = td.Num(0);
            y = td.Num(1);
        }

        Assert.Equal(72, x, 3);
        Assert.Equal(700, y, 3);
    }

    [Fact]
    public void TextColor_EmittedAsNonStrokingRg()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("C", 0, 0) { Color = Color.Red });

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var rg = Find(content, "rg");

        Assert.Equal(3, rg.Operands.Count);
        Assert.Equal(1.0, rg.Num(0), 3);
        Assert.Equal(0.0, rg.Num(1), 3);
        Assert.Equal(0.0, rg.Num(2), 3);
    }
}
