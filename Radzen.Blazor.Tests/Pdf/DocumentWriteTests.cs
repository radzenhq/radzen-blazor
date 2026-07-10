using System.Collections.Generic;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// Write->reload tests for the physical Document/Page skeleton. Documents are
// produced by Document.Save/ToArray and read back through the Objects-layer
// DocumentReader, which is the oracle for the resulting object graph. Loading
// into a Document is out of scope for C1.
public class DocumentWriteTests
{
    private static DocumentReader Reload(Document document) => DocumentReader.Parse(document.ToArray());

    private static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    private static DictionaryObject PagesNode(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Pages"]));

    private static ArrayObject Kids(DocumentReader reader)
        => Assert.IsType<ArrayObject>(reader.Resolve(PagesNode(reader)["Kids"]));

    private static DictionaryObject Kid(DocumentReader reader, int index)
        => Assert.IsType<DictionaryObject>(reader.Resolve(Kids(reader)[index]));

    private static byte[] KidContent(DocumentReader reader, int index)
        => Assert.IsType<StreamObject>(reader.Resolve(Kid(reader, index)["Contents"])).Data;

    private static void AssertMediaBox(DocumentReader reader, DictionaryObject page, double width, double height)
    {
        var box = Assert.IsType<ArrayObject>(reader.Resolve(page["MediaBox"]));
        Assert.Equal(4, box.Count);
        Assert.True(System.Math.Abs(0.0 - Assert.IsType<NumberObject>(box[0]).DoubleValue) < 0.01);
        Assert.True(System.Math.Abs(0.0 - Assert.IsType<NumberObject>(box[1]).DoubleValue) < 0.01);
        Assert.True(System.Math.Abs(width - Assert.IsType<NumberObject>(box[2]).DoubleValue) < 0.01);
        Assert.True(System.Math.Abs(height - Assert.IsType<NumberObject>(box[3]).DoubleValue) < 0.01);
    }

    [Fact]
    public void Output_StartsWithPdf17Header()
    {
        var document = new Document();
        document.Pages.Add();

        var bytes = document.ToArray();
        var header = Encoding.ASCII.GetString(bytes, 0, 8);
        Assert.Equal("%PDF-1.7", header);
    }

    [Fact]
    public void EmptyDocument_SavesValidCatalogAndEmptyPagesTree()
    {
        var reader = Reload(new Document());

        var catalog = Catalog(reader);
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);

        var pages = PagesNode(reader);
        Assert.Equal("Pages", Assert.IsType<NameObject>(pages["Type"]).Value);
        Assert.Equal(0, Assert.IsType<NumberObject>(pages["Count"]).IntValue);
        Assert.Empty(Kids(reader));
    }

    [Fact]
    public void OnePageA4_BuildsSinglePageKidWithMediaBoxAndParent()
    {
        var document = new Document();
        document.Pages.Add();

        var reader = Reload(document);
        Assert.Equal(1, Assert.IsType<NumberObject>(PagesNode(reader)["Count"]).IntValue);
        Assert.Single(Kids(reader));

        var page = Kid(reader, 0);
        Assert.Equal("Page", Assert.IsType<NameObject>(page["Type"]).Value);
        AssertMediaBox(reader, page, PageSizes.A4.Width.Point, PageSizes.A4.Height.Point);

        var pagesRef = Assert.IsType<ReferenceObject>(Catalog(reader)["Pages"]);
        var parentRef = Assert.IsType<ReferenceObject>(page["Parent"]);
        Assert.Equal(pagesRef.ObjectNumber, parentRef.ObjectNumber);
    }

    [Fact]
    public void ThreePages_PreserveOrderAndMediaBoxes()
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4);
        document.Pages.Add(PageSizes.Letter);
        document.Pages.Add(PageSizes.A4, PageOrientation.Landscape);

        var reader = Reload(document);
        Assert.Equal(3, Assert.IsType<NumberObject>(PagesNode(reader)["Count"]).IntValue);
        Assert.Equal(3, Kids(reader).Count);

        AssertMediaBox(reader, Kid(reader, 0), PageSizes.A4.Width.Point, PageSizes.A4.Height.Point);
        AssertMediaBox(reader, Kid(reader, 1), PageSizes.Letter.Width.Point, PageSizes.Letter.Height.Point);
        // A4 landscape swaps width/height: [0 0 841.88 595.27].
        AssertMediaBox(reader, Kid(reader, 2), PageSizes.A4.Height.Point, PageSizes.A4.Width.Point);
        AssertMediaBox(reader, Kid(reader, 2), 841.88, 595.27);
    }

    [Fact]
    public void EveryKid_ParentPointsBackToPagesNode()
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4);
        document.Pages.Add(PageSizes.Letter);
        document.Pages.Add(PageSizes.A5, PageOrientation.Landscape);

        var reader = Reload(document);
        var pagesRef = Assert.IsType<ReferenceObject>(Catalog(reader)["Pages"]).ObjectNumber;

        var kids = Kids(reader);
        for (var i = 0; i < kids.Count; i++)
        {
            var parent = Assert.IsType<ReferenceObject>(Kid(reader, i)["Parent"]);
            Assert.Equal(pagesRef, parent.ObjectNumber);
        }
    }

    [Fact]
    public void Info_AllFieldsSet_WrittenAsStrings()
    {
        var document = new Document();
        document.Info.Title = "The Title";
        document.Info.Author = "The Author";
        document.Info.Subject = "The Subject";
        document.Info.Keywords = "one two three";
        document.Info.Creator = "Radzen";
        document.Pages.Add();

        var reader = Reload(document);
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]!));

        Assert.Equal("The Title", Assert.IsType<StringObject>(info["Title"]).Value);
        Assert.Equal("The Author", Assert.IsType<StringObject>(info["Author"]).Value);
        Assert.Equal("The Subject", Assert.IsType<StringObject>(info["Subject"]).Value);
        Assert.Equal("one two three", Assert.IsType<StringObject>(info["Keywords"]).Value);
        Assert.Equal("Radzen", Assert.IsType<StringObject>(info["Creator"]).Value);
    }

    [Fact]
    public void Info_UnsetFields_AbsentFromDictionary()
    {
        var document = new Document();
        document.Info.Title = "Only Title";

        var reader = Reload(document);
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]!));

        Assert.Equal("Only Title", Assert.IsType<StringObject>(info["Title"]).Value);
        Assert.False(info.ContainsKey("Author"));
        Assert.False(info.ContainsKey("Subject"));
        Assert.False(info.ContainsKey("Keywords"));
        Assert.False(info.ContainsKey("Creator"));
    }

    [Fact]
    public void Info_NoFieldsSet_NoInfoInTrailer()
    {
        var reader = Reload(new Document());

        Assert.False(reader.Trailer.ContainsKey("Info"));
    }

    [Fact]
    public void Page_WithContent_ContentsStreamRoundTripsByteIdentical()
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (Hi) Tj ET");
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(content);

        var reader = Reload(document);
        var stream = Assert.IsType<StreamObject>(reader.Resolve(Kid(reader, 0)["Contents"]));
        Assert.Equal(content, stream.Data);
        // Skeleton stores content verbatim - no compression filter.
        Assert.False(stream.Dictionary.ContainsKey("Filter"));
    }

    [Fact]
    public void Page_WithoutContent_HasNoContentsEntry()
    {
        var document = new Document();
        document.Pages.Add();

        var reader = Reload(document);
        Assert.False(Kid(reader, 0).ContainsKey("Contents"));
    }

    [Fact]
    public void ObjectCount_MatchesCatalogPagesPagesContentsInfo()
    {
        var document = new Document();
        document.Info.Title = "Counted";
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("a"));
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("b"));

        var reader = Reload(document);
        // catalog + pages + 2 page + 2 content + info == 7
        Assert.Equal(7, reader.ObjectCount);
    }

    [Fact]
    public void AllReferences_InCatalogAndPageChain_Resolve()
    {
        var document = new Document();
        document.Info.Title = "Refs";
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("one"));
        document.Pages.Add(PageSizes.Letter);

        var reader = Reload(document);
        var visited = new HashSet<int>();
        AssertResolves(reader, reader.Trailer, visited);
    }

    private static void AssertResolves(DocumentReader reader, DocumentObject value, HashSet<int> visited)
    {
        switch (value)
        {
            case ReferenceObject reference:
                if (!visited.Add(reference.ObjectNumber))
                {
                    return;
                }

                var resolved = reader.Resolve(reference);
                Assert.NotNull(resolved);
                AssertResolves(reader, resolved, visited);
                break;
            case DictionaryObject dictionary:
                foreach (var key in dictionary.Keys)
                {
                    AssertResolves(reader, dictionary[key], visited);
                }

                break;
            case StreamObject stream:
                foreach (var key in stream.Dictionary.Keys)
                {
                    AssertResolves(reader, stream.Dictionary[key], visited);
                }

                break;
            case ArrayObject array:
                for (var i = 0; i < array.Count; i++)
                {
                    AssertResolves(reader, array[i], visited);
                }

                break;
        }
    }

    [Fact]
    public void ToArray_IsDeterministic()
    {
        var document = new Document();
        document.Info.Title = "Stable";
        document.Pages.Add();
        document.Pages.Add(PageSizes.Letter);

        Assert.Equal(document.ToArray(), document.ToArray());
    }

    [Fact]
    public void Save_ToStream_MatchesToArray()
    {
        var document = new Document();
        document.Pages.Add();

        using var stream = new MemoryStream();
        document.SaveToStream(stream);

        Assert.Equal(document.ToArray(), stream.ToArray());
    }

    [Fact]
    public void RemoveAt_DecrementsCountAndKeepsRemainingContent()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("first"));
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("second"));

        document.Pages.RemoveAt(0);

        var reader = Reload(document);
        Assert.Equal(1, Assert.IsType<NumberObject>(PagesNode(reader)["Count"]).IntValue);
        Assert.Single(Kids(reader));
        Assert.Equal(Encoding.ASCII.GetBytes("second"), KidContent(reader, 0));
    }

    [Fact]
    public void Insert_ReordersKidsInReparsedTree()
    {
        var document = new Document();
        var first = document.Pages.Add();
        first.SetContent(Encoding.ASCII.GetBytes("A"));
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("B"));

        document.Pages.RemoveAt(0);
        document.Pages.Insert(1, first);

        var reader = Reload(document);
        Assert.Equal(2, Assert.IsType<NumberObject>(PagesNode(reader)["Count"]).IntValue);
        Assert.Equal(Encoding.ASCII.GetBytes("B"), KidContent(reader, 0));
        Assert.Equal(Encoding.ASCII.GetBytes("A"), KidContent(reader, 1));
    }
}
