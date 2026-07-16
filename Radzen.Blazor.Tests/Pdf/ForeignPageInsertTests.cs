#nullable enable

using System;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Inserting a page that belongs to another Document carries that page's loaded
// source state onto the receiving document, so the emitted page keeps the
// /Resources its retained content stream refers to. Text extraction cannot see a
// regression here: it reads content operators and never resolves the font, so
// these tests assert on the emitted page dictionary.
public class ForeignPageInsertTests
{
    private static Document LoadedWithText(string text)
    {
        var document = new Document();
        document.Pages.Add().Content.Add(new TextContent(text, 72, 700) { Font = new Font { Size = 12 } });
        return InterpreterTestSupport.Load(document.ToArray());
    }

    private static DictionaryObject PageResources(DocumentReader reader, int index)
        => Assert.IsType<DictionaryObject>(reader.Resolve(DocumentLoadTests.Kid(reader, index)["Resources"]));

    private static void AssertFontsResolve(DocumentReader reader, int index)
    {
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(PageResources(reader, index)["Font"]));
        Assert.NotEmpty(fonts.Keys);
        foreach (var name in fonts.Keys)
        {
            Assert.IsType<DictionaryObject>(reader.Resolve(fonts[name]));
        }
    }

    [Fact]
    public void Insert_ForeignLoadedPage_EmitsResourcesForRetainedContent()
    {
        var source = LoadedWithText("Hello foreign page");
        var target = new Document();

        target.Pages.Insert(0, source.Pages[0]);

        var reader = DocumentReader.Parse(target.ToArray());
        Assert.Equal(1, DocumentLoadTests.PageCount(reader));
        AssertFontsResolve(reader, 0);
    }

    [Fact]
    public void Insert_ForeignLoadedPage_ResourcesNameMatchesContentStream()
    {
        var source = LoadedWithText("Hello foreign page");
        var target = new Document();

        target.Pages.Insert(0, source.Pages[0]);

        var bytes = target.ToArray();
        var reader = DocumentReader.Parse(bytes);
        var content = System.Text.Encoding.Latin1.GetString(DocumentLoadTests.KidContent(reader, 0));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(PageResources(reader, 0)["Font"]));
        foreach (var name in ContentStreamTokenizer.Parse(DocumentLoadTests.KidContent(reader, 0))
            .Where(op => op.Operator == "Tf")
            .Select(op => op.Operands.First(o => o.Kind == ContentTokenKind.Name).Text))
        {
            Assert.True(fonts.ContainsKey(name), $"Content stream references /{name} but /Resources has [{string.Join(", ", fonts.Keys)}]. Content: {content}");
        }
    }

    [Fact]
    public void Insert_ForeignLoadedPage_IntoLoadedTarget_EmitsResources()
    {
        var source = LoadedWithText("Hello foreign page");
        var target = LoadedWithText("Target own text");

        target.Pages.Insert(0, source.Pages[0]);

        var reader = DocumentReader.Parse(target.ToArray());
        Assert.Equal(2, DocumentLoadTests.PageCount(reader));
        AssertFontsResolve(reader, 0);
        AssertFontsResolve(reader, 1);
    }

    [Fact]
    public void Insert_ForeignLoadedPage_LeavesDonorSaveIntact()
    {
        var source = LoadedWithText("Hello foreign page");
        var target = new Document();

        target.Pages.Insert(0, source.Pages[0]);

        var reader = DocumentReader.Parse(source.ToArray());
        Assert.Equal(1, DocumentLoadTests.PageCount(reader));
        AssertFontsResolve(reader, 0);
    }

    [Fact]
    public void Insert_ForeignBuiltPage_EmitsResources()
    {
        var source = new Document();
        source.Pages.Add().Content.Add(new TextContent("Built page", 72, 700) { Font = new Font { Size = 12 } });
        var target = new Document();

        target.Pages.Insert(0, source.Pages[0]);

        var reader = DocumentReader.Parse(target.ToArray());
        AssertFontsResolve(reader, 0);
    }

    [Fact]
    public void Insert_InvalidIndex_DoesNotAdoptPage()
    {
        var source = LoadedWithText("Hello foreign page");
        var target = new Document();

        Assert.Throws<ArgumentOutOfRangeException>(() => target.Pages.Insert(3, source.Pages[0]));
        Assert.Empty(target.Pages);

        var reader = DocumentReader.Parse(source.ToArray());
        AssertFontsResolve(reader, 0);
    }

    [Fact]
    public void Insert_ForeignLoadedPage_MatchesImportPageResources()
    {
        var inserted = new Document();
        inserted.Pages.Insert(0, LoadedWithText("Hello foreign page").Pages[0]);

        var imported = new Document();
        imported.ImportPage(LoadedWithText("Hello foreign page"), 0);

        var insertedReader = DocumentReader.Parse(inserted.ToArray());
        var importedReader = DocumentReader.Parse(imported.ToArray());

        var insertedFonts = Assert.IsType<DictionaryObject>(insertedReader.Resolve(PageResources(insertedReader, 0)["Font"]));
        var importedFonts = Assert.IsType<DictionaryObject>(importedReader.Resolve(PageResources(importedReader, 0)["Font"]));
        Assert.Equal(importedFonts.Keys.OrderBy(k => k), insertedFonts.Keys.OrderBy(k => k));
    }
}
