#nullable enable
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentWriteTests
{
    private static string Catalog(string emission) => Line(emission, "/Type /Catalog");

    private static string PagesNode(string emission) => Line(emission, "/Type /Pages ");

    private static string PagesNumber(string emission)
        => Shaped("catalog", @"/Pages (\d+) 0 R", Catalog(emission)).Groups[1].Value;

    private static string[] Kids(string emission, int count)
        => References("pages node", "Kids", count, PagesNode(emission));

    private static string PageContent(string emission, string pageObject)
        => IndirectObject(emission, Shaped("page", @"/Contents (\d+) 0 R", pageObject).Groups[1].Value);

    private static void AssertMediaBox(string subject, string pageObject, double width, double height)
    {
        var box = Shaped(
            $"{subject} /MediaBox",
            @"/MediaBox \[([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+)\]",
            pageObject);

        Assert.Equal(0.0, double.Parse(box.Groups[1].Value, CultureInfo.InvariantCulture), 0.01);
        Assert.Equal(0.0, double.Parse(box.Groups[2].Value, CultureInfo.InvariantCulture), 0.01);
        Assert.Equal(width, double.Parse(box.Groups[3].Value, CultureInfo.InvariantCulture), 0.01);
        Assert.Equal(height, double.Parse(box.Groups[4].Value, CultureInfo.InvariantCulture), 0.01);
    }

    [Fact]
    public void Output_StartsWithPdf17Header()
    {
        var document = new PortableDocument();
        document.Pages.Add();

        var bytes = document.ToArray();
        var header = Encoding.ASCII.GetString(bytes, 0, 8);
        Assert.Equal("%PDF-1.7", header);
    }

    [Fact]
    public void EmptyDocument_SavesValidCatalogAndEmptyPagesTree()
    {
        var emission = Emit(new PortableDocument());

        Carries("catalog", "/Type /Catalog", Catalog(emission));

        var pages = PagesNode(emission);
        Carries("pages node", "/Type /Pages ", pages);
        Carries("pages node", "/Count 0", pages);
        References("pages node", "Kids", 0, pages);
    }

    [Fact]
    public void OnePageA4_BuildsSinglePageKidWithMediaBoxAndParent()
    {
        var document = new PortableDocument();
        document.Pages.Add();

        var emission = Emit(document);
        Carries("pages node", "/Count 1", PagesNode(emission));

        var kid = Kids(emission, 1)[0];
        var page = IndirectObject(emission, kid);
        Carries($"page {kid} 0 R", "/Type /Page ", page);
        AssertMediaBox($"page {kid} 0 R", page, PageSizes.A4.Width.Point, PageSizes.A4.Height.Point);
        Carries($"page {kid} 0 R", $"/Parent {PagesNumber(emission)} 0 R", page);
    }

    [Fact]
    public void ThreePages_PreserveOrderAndMediaBoxes()
    {
        var document = new PortableDocument();
        document.Pages.Add(PageSizes.A4);
        document.Pages.Add(PageSizes.Letter);
        document.Pages.Add(PageSizes.A4, PageOrientation.Landscape);

        var emission = Emit(document);
        Carries("pages node", "/Count 3", PagesNode(emission));

        var kids = Kids(emission, 3);
        AssertMediaBox("first page", IndirectObject(emission, kids[0]), PageSizes.A4.Width.Point, PageSizes.A4.Height.Point);
        AssertMediaBox("second page", IndirectObject(emission, kids[1]), PageSizes.Letter.Width.Point, PageSizes.Letter.Height.Point);
        AssertMediaBox("third page", IndirectObject(emission, kids[2]), PageSizes.A4.Height.Point, PageSizes.A4.Width.Point);
        AssertMediaBox("third page", IndirectObject(emission, kids[2]), 841.88, 595.27);
    }

    [Fact]
    public void EveryKid_ParentPointsBackToPagesNode()
    {
        var document = new PortableDocument();
        document.Pages.Add(PageSizes.A4);
        document.Pages.Add(PageSizes.Letter);
        document.Pages.Add(PageSizes.A5, PageOrientation.Landscape);

        var emission = Emit(document);
        var parent = $"/Parent {PagesNumber(emission)} 0 R";

        foreach (var kid in Kids(emission, 3))
        {
            Carries($"page {kid} 0 R", parent, IndirectObject(emission, kid));
        }
    }

    [Fact]
    public void Info_AllFieldsSet_WrittenAsStrings()
    {
        var document = new PortableDocument();
        document.Info.Title = "The Title";
        document.Info.Author = "The Author";
        document.Info.Subject = "The Subject";
        document.Info.Keywords = "one two three";
        document.Info.Creator = "Radzen";
        document.Pages.Add();

        var emission = Emit(document);
        var info = IndirectObject(emission, Shaped("trailer", @"/Info (\d+) 0 R", Line(emission, "/Root ")).Groups[1].Value);

        Carries("info dictionary", "/Title (The Title)", info);
        Carries("info dictionary", "/Author (The Author)", info);
        Carries("info dictionary", "/Subject (The Subject)", info);
        Carries("info dictionary", "/Keywords (one two three)", info);
        Carries("info dictionary", "/Creator (Radzen)", info);
    }

    [Fact]
    public void Info_UnsetFields_AbsentFromDictionary()
    {
        var document = new PortableDocument();
        document.Info.Title = "Only Title";

        var emission = Emit(document);
        var info = IndirectObject(emission, Shaped("trailer", @"/Info (\d+) 0 R", Line(emission, "/Root ")).Groups[1].Value);

        Carries("info dictionary", "/Title (Only Title)", info);
        Lacks("info dictionary", "/Author", info);
        Lacks("info dictionary", "/Subject", info);
        Lacks("info dictionary", "/Keywords", info);
        Lacks("info dictionary", "/Creator", info);
    }

    [Fact]
    public void Info_NoFieldsSet_NoInfoInTrailer()
    {
        var emission = Emit(new PortableDocument());

        Lacks("trailer", "/Info", Line(emission, "/Root "));
    }

    [Fact]
    public void Page_WithContent_ContentsStreamRoundTripsByteIdentical()
    {
        const string content = "BT /F1 12 Tf 72 700 Td (Hi) Tj ET";
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes(content));

        var emission = Emit(document);
        var stream = PageContent(emission, IndirectObject(emission, Kids(emission, 1)[0]));

        Carries("page content stream", $"<< /Length {content.Length} >>\nstream\n{content}\nendstream", stream);
        Lacks("page content stream", "/Filter", stream);
    }

    [Fact]
    public void Page_WithoutContent_HasNoContentsEntry()
    {
        var document = new PortableDocument();
        document.Pages.Add();

        var emission = Emit(document);

        Lacks("page", "/Contents", IndirectObject(emission, Kids(emission, 1)[0]));
    }

    [Fact]
    public void ObjectCount_MatchesCatalogPagesPagesContentsInfo()
    {
        var document = new PortableDocument();
        document.Info.Title = "Counted";
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("a"));
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("b"));

        var emission = Emit(document);
        var objects = Regex.Matches(emission, @"(?m)^\d+ 0 obj$").Count;

        Assert.True(
            objects == 7,
            $"Expected 7 indirect objects in the emission, found {objects}.\n{Excerpt(emission)}");
    }

    [Fact]
    public void AllReferences_InCatalogAndPageChain_Resolve()
    {
        var document = new PortableDocument();
        document.Info.Title = "Refs";
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("one"));
        document.Pages.Add(PageSizes.Letter);

        var emission = Emit(document);
        var referenced = Regex.Matches(emission, @"(\d+) 0 R")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(referenced);

        foreach (var number in referenced)
        {
            IndirectObject(emission, number);
        }
    }

    [Fact]
    public void ToArray_IsDeterministic()
    {
        var document = new PortableDocument();
        document.Info.Title = "Stable";
        document.Pages.Add();
        document.Pages.Add(PageSizes.Letter);

        Assert.Equal(document.ToArray(), document.ToArray());
    }

    [Fact]
    public void Save_ToStream_MatchesToArray()
    {
        var document = new PortableDocument();
        document.Pages.Add();

        using var stream = new MemoryStream();
        document.SaveToStream(stream);

        Assert.Equal(document.ToArray(), stream.ToArray());
    }

    [Fact]
    public void RemoveAt_DecrementsCountAndKeepsRemainingContent()
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("first"));
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("second"));

        document.Pages.RemoveAt(0);

        var emission = Emit(document);
        Carries("pages node", "/Count 1", PagesNode(emission));

        var page = IndirectObject(emission, Kids(emission, 1)[0]);
        Carries("page content stream", "<< /Length 6 >>\nstream\nsecond\nendstream", PageContent(emission, page));
    }

    [Fact]
    public void Insert_ReordersKidsInReparsedTree()
    {
        var document = new PortableDocument();
        var first = document.Pages.Add();
        first.SetContent(Encoding.ASCII.GetBytes("A"));
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("B"));

        document.Pages.RemoveAt(0);
        document.Pages.Insert(1, first);

        var emission = Emit(document);
        Carries("pages node", "/Count 2", PagesNode(emission));

        var kids = Kids(emission, 2);
        Carries(
            "first page content stream",
            "<< /Length 1 >>\nstream\nB\nendstream",
            PageContent(emission, IndirectObject(emission, kids[0])));
        Carries(
            "second page content stream",
            "<< /Length 1 >>\nstream\nA\nendstream",
            PageContent(emission, IndirectObject(emission, kids[1])));
    }
}
