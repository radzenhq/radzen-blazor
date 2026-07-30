#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentLoadTests
{
    private static PortableDocument Load(byte[] bytes, LoadOptions? options = null)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream, options);
    }

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    [Fact]
    public void RoundTrip_ThreePages_PreservesCountSizesInfoAndContent()
    {
        var source = new PortableDocument();
        source.Info.Title = "Round Trip";
        source.Info.Author = "Vasil";
        source.Info.Subject = "Subject Line";
        source.Info.Keywords = "alpha beta gamma";
        source.Info.Creator = "Radzen";

        var c0 = Ascii("BT /F1 12 Tf 72 700 Td (Page one) Tj ET");
        var c1 = Ascii("BT /F1 10 Tf 40 500 Td (Second page here) Tj ET");
        var c2 = Ascii("0 0 1 rg 10 10 200 200 re f");
        source.Pages.Add(PageSizes.A4).SetContent(c0);
        source.Pages.Add(PageSizes.Letter).SetContent(c1);
        source.Pages.Add(PageSizes.A4, PageOrientation.Landscape).SetContent(c2);

        var loaded = Load(source.ToArray());

        Assert.Equal(3, loaded.Pages.Count);

        Assert.Equal(PageSizes.A4.Width.Point, loaded.Pages[0].Width.Point, 0.01);
        Assert.Equal(PageSizes.A4.Height.Point, loaded.Pages[0].Height.Point, 0.01);
        Assert.Equal(PageSizes.Letter.Width.Point, loaded.Pages[1].Width.Point, 0.01);
        Assert.Equal(PageSizes.Letter.Height.Point, loaded.Pages[1].Height.Point, 0.01);
        Assert.Equal(PageSizes.A4.Height.Point, loaded.Pages[2].Width.Point, 0.01);
        Assert.Equal(PageSizes.A4.Width.Point, loaded.Pages[2].Height.Point, 0.01);

        Assert.Equal("Round Trip", loaded.Info.Title);
        Assert.Equal("Vasil", loaded.Info.Author);
        Assert.Equal("Subject Line", loaded.Info.Subject);
        Assert.Equal("alpha beta gamma", loaded.Info.Keywords);
        Assert.Equal("Radzen", loaded.Info.Creator);

        Assert.Equal(c0, loaded.Pages[0].GetContent());
        Assert.Equal(c1, loaded.Pages[1].GetContent());
        Assert.Equal(c2, loaded.Pages[2].GetContent());
    }

    [Fact]
    public void RoundTrip_ReSaveReparsed_KidContentMatchesOriginals()
    {
        var source = new PortableDocument();
        var a = Ascii("first");
        var b = Ascii("second");
        source.Pages.Add().SetContent(a);
        source.Pages.Add().SetContent(b);

        var loaded = Load(source.ToArray());
        var reader = DocumentReader.Parse(loaded.ToArray());

        Assert.Equal(2, PageCount(reader));
        Assert.Equal(a, KidContent(reader, 0));
        Assert.Equal(b, KidContent(reader, 1));
    }

    [Fact]
    public void Load_Encrypted_EmptyUserPassword_PagesReadable()
    {
        var bytes = PdfTestResources.ReadAllBytes("Documents/encrypted-aes-256.pdf");

        var document = Load(bytes, new LoadOptions { Password = "" });

        Assert.Equal(1, document.Pages.Count);
        var content = document.Pages[0].GetContent();
        Assert.NotNull(content);
        Assert.Contains("Hello encrypted world!", Readable(content!));
    }

    [Fact]
    public void Load_Encrypted_WrongPassword_Throws()
    {
        var bytes = PdfTestResources.ReadAllBytes("Documents/encrypted-aes-256.pdf");

        Assert.Throws<InvalidPasswordException>(
            () => Load(bytes, new LoadOptions { Password = "definitely-wrong" }));
    }

    [Fact]
    public void Split_RemoveFirstPage_DropsItAndKeepsOrder()
    {
        var source = new PortableDocument();
        source.Pages.Add().SetContent(Ascii("one"));
        source.Pages.Add().SetContent(Ascii("two"));
        source.Pages.Add().SetContent(Ascii("three"));

        var loaded = Load(source.ToArray());
        loaded.Pages.RemoveAt(0);

        var reader = DocumentReader.Parse(loaded.ToArray());
        Assert.Equal(2, PageCount(reader));
        Assert.Equal(Ascii("two"), KidContent(reader, 0));
        Assert.Equal(Ascii("three"), KidContent(reader, 1));
    }

    [Fact]
    public void Reorder_InsertMovesPage()
    {
        var source = new PortableDocument();
        source.Pages.Add().SetContent(Ascii("A"));
        source.Pages.Add().SetContent(Ascii("B"));
        source.Pages.Add().SetContent(Ascii("C"));

        var loaded = Load(source.ToArray());
        var moved = loaded.Pages[0];
        loaded.Pages.RemoveAt(0);
        loaded.Pages.Insert(2, moved);

        var reader = DocumentReader.Parse(loaded.ToArray());
        Assert.Equal(3, PageCount(reader));
        Assert.Equal(Ascii("B"), KidContent(reader, 0));
        Assert.Equal(Ascii("C"), KidContent(reader, 1));
        Assert.Equal(Ascii("A"), KidContent(reader, 2));
    }

    private static string Readable(byte[] content)
    {
        var raw = Encoding.Latin1.GetString(content);
        if (raw.Contains("Hello encrypted world!", StringComparison.Ordinal))
        {
            return raw;
        }

        return Encoding.Latin1.GetString(FlateFilter.Decode(content));
    }

    internal static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    internal static DictionaryObject PagesNode(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Pages"]));

    internal static ArrayObject Kids(DocumentReader reader)
        => Assert.IsType<ArrayObject>(reader.Resolve(PagesNode(reader)["Kids"]));

    internal static int PageCount(DocumentReader reader)
        => Assert.IsType<NumberObject>(PagesNode(reader)["Count"]).IntValue;

    internal static DictionaryObject Kid(DocumentReader reader, int index)
        => Assert.IsType<DictionaryObject>(reader.Resolve(Kids(reader)[index]));

    internal static byte[] KidContent(DocumentReader reader, int index)
        => Assert.IsType<StreamObject>(reader.Resolve(Kid(reader, index)["Contents"])).Data.ToArray();
}
