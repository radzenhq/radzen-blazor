#nullable enable

using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using Radzen.Documents.Fonts;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class TextContentTests
{
    private static string PageLine(string emission) => Line(emission, "/Type /Page ");

    private static string PageContent(string emission)
        => IndirectObject(emission, Shaped("page", @"/Contents (\d+) 0 R", PageLine(emission)).Groups[1].Value);

    private static string FontKey(string content)
        => Shaped("text object", @"/(\S+) [\d.]+ Tf\n", content).Groups[1].Value;

    private static string FontResource(string emission, string key)
        => Shaped(
            $"font resource /{key}",
            $@"/{Regex.Escape(key)} << ([^>]*)>>",
            PageLine(emission)).Groups[1].Value;

    [Fact]
    public void Text_EmitsBtTfTjEtInOrder()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hello", 72, 700));

        Shaped("text object", @"BT\n[\s\S]*? Tf\n[\s\S]*? Tj\n[\s\S]*?ET\n", PageContent(Emit(document)));
    }

    [Fact]
    public void Tf_CarriesResourceNameAndSize()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Family = "Helvetica", Size = 18 } });

        Shaped("text object", @"\n/\S+ 18 Tf\n", PageContent(Emit(document)));
    }

    [Fact]
    public void FontResource_IsBase14Type1Helvetica()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Family = "Helvetica" } });

        var emission = Emit(document);
        var key = FontKey(PageContent(emission));
        var font = FontResource(emission, key);

        Carries($"font resource /{key}", "/Type /Font ", font);
        Carries($"font resource /{key}", "/Subtype /Type1 ", font);
        Carries($"font resource /{key}", "/BaseFont /Helvetica ", font);
        Carries($"font resource /{key}", "/Encoding /WinAnsiEncoding ", font);
    }

    [Fact]
    public void Bold_MapsToHelveticaBoldBaseFont()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Family = "Helvetica", Bold = true } });

        var emission = Emit(document);
        var key = FontKey(PageContent(emission));

        Carries($"font resource /{key}", "/BaseFont /Helvetica-Bold ", FontResource(emission, key));
    }

    [Fact]
    public void Times_MapsToTimesRomanBaseFont()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Family = "Times" } });

        var emission = Emit(document);
        var key = FontKey(PageContent(emission));

        Carries($"font resource /{key}", "/BaseFont /Times-Roman ", FontResource(emission, key));
    }

    [Fact]
    public void Courier_MapsToCourierBaseFont()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 0, 0) { Font = new Font { Family = "Courier" } });

        var emission = Emit(document);
        var key = FontKey(PageContent(emission));

        Carries($"font resource /{key}", "/BaseFont /Courier ", FontResource(emission, key));
    }

    [Fact]
    public void TwoDistinctFonts_ProduceTwoResources()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("a", 0, 0) { Font = new Font { Family = "Helvetica" } });
        page.Content.Add(new TextContent("b", 0, 0) { Font = new Font { Family = "Times" } });

        var resources = PageLine(Emit(document));

        Carries("page resources", "/BaseFont /Helvetica ", resources);
        Carries("page resources", "/BaseFont /Times-Roman ", resources);
    }

    [Fact]
    public void Text_IsWinAnsiEncoded()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("A\u20AC\u2013z", 0, 0));

        Carries("text object", "(A\u0080\u0096z) Tj\n", PageContent(Emit(document)));
    }

    [Fact]
    public void AsciiText_RoundTripsAsWinAnsiBytes()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hello", 0, 0));

        Carries("text object", "(Hello) Tj\n", PageContent(Emit(document)));
    }

    [Fact]
    public void NonWinAnsiChar_EmitsQuestionMarkPlaceholder_NotDropped()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("A\u03A9z", 0, 0));

        Carries("text object", "(A?z) Tj\n", PageContent(Emit(document)));
    }

    [Fact]
    public void TextPosition_XAndYEmitted()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("P", 72, 700));

        Shaped(
            "text object",
            @"\n(?:72 700 Td|1 0 [-\d.]+ 1 72 700 Tm)\n",
            PageContent(Emit(document)));
    }

    [Fact]
    public void TextColor_EmittedAsNonStrokingRg()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("C", 0, 0) { Color = Color.Red });

        Carries("text object", "\n1 0 0 rg\n", PageContent(Emit(document)));
    }
}
