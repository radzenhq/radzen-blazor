#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class NavigationOutlineTests
{
    private static Document TwoSectionDocument()
    {
        var document = new Document();
        foreach (var (anchor, text) in new[] { ("intro", "Introduction body"), ("details", "Details body") })
        {
            var section = document.Sections.Add();
            section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
            section.Margins.SetAll(Unit.FromPoint(40));
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(text).Anchor = anchor;
            section.Blocks.Add(paragraph);
        }

        return document;
    }

    private static Document PlainDocument()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(40));
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Plain content");
        section.Blocks.Add(paragraph);
        return document;
    }

    private static DictionaryObject Resolve(DocumentReader reader, DocumentObject value)
        => Assert.IsType<DictionaryObject>(reader.Resolve(value));

    private static Dictionary<string, ArrayObject> NamedDestinations(DocumentReader reader)
    {
        var catalog = ContentTestHelpers.Catalog(reader);
        Assert.True(catalog.TryGetValue("Names", out var namesObject), "catalog must carry /Names");
        var names = Resolve(reader, namesObject!);
        Assert.True(names.TryGetValue("Dests", out var destsObject), "/Names must carry /Dests");
        var dests = Resolve(reader, destsObject!);
        var entries = Assert.IsType<ArrayObject>(reader.Resolve(dests["Names"]));
        var result = new Dictionary<string, ArrayObject>(StringComparer.Ordinal);
        for (var i = 0; i + 1 < entries.Count; i += 2)
        {
            var name = Assert.IsType<StringObject>(reader.Resolve(entries[i]));
            result[name.Value] = Assert.IsType<ArrayObject>(reader.Resolve(entries[i + 1]));
        }

        return result;
    }

    [Fact]
    public void Outline_EmitsCatalogTree_WithTitlesAndNesting()
    {
        var document = TwoSectionDocument();
        var child = new OutlineItem("Details", OutlineTarget.ToAnchor("details"));
        var root = new OutlineItem("Introduction", OutlineTarget.ToAnchor("intro"));
        root.Children.Add(child);
        var renderer = new DocumentRenderer();
        renderer.Outline.Add(root);

        var reader = BuildTestSupport.Read(document, renderer);
        var catalog = ContentTestHelpers.Catalog(reader);
        Assert.True(catalog.TryGetValue("Outlines", out var outlinesObject), "catalog must carry /Outlines");
        var outlines = Resolve(reader, outlinesObject!);
        Assert.Equal("Outlines", Assert.IsType<NameObject>(reader.Resolve(outlines["Type"])).Value);
        Assert.Equal(2, Assert.IsType<NumberObject>(reader.Resolve(outlines["Count"])).IntValue);

        var first = Resolve(reader, outlines["First"]);
        var last = Resolve(reader, outlines["Last"]);
        Assert.Equal("Introduction", Assert.IsType<StringObject>(reader.Resolve(first["Title"])).Value);
        Assert.Equal("Introduction", Assert.IsType<StringObject>(reader.Resolve(last["Title"])).Value);
        Assert.Same(outlines, Resolve(reader, first["Parent"]));
        Assert.Equal("intro", Assert.IsType<StringObject>(reader.Resolve(first["Dest"])).Value);
        Assert.Equal(1, Assert.IsType<NumberObject>(reader.Resolve(first["Count"])).IntValue);

        var nested = Resolve(reader, first["First"]);
        Assert.Equal("Details", Assert.IsType<StringObject>(reader.Resolve(nested["Title"])).Value);
        Assert.Same(first, Resolve(reader, nested["Parent"]));
        Assert.Equal("details", Assert.IsType<StringObject>(reader.Resolve(nested["Dest"])).Value);
        Assert.Same(nested, Resolve(reader, first["Last"]));
        Assert.False(nested.ContainsKey("Next"));
        Assert.False(nested.ContainsKey("Prev"));
    }

    [Fact]
    public void OutlineSiblings_LinkPrevAndNext()
    {
        var document = TwoSectionDocument();
        var renderer = new DocumentRenderer();
        renderer.Outline.Add(new OutlineItem("Introduction", OutlineTarget.ToAnchor("intro")));
        renderer.Outline.Add(new OutlineItem("Details", OutlineTarget.ToAnchor("details")));

        var reader = BuildTestSupport.Read(document, renderer);
        var outlines = Resolve(reader, ContentTestHelpers.Catalog(reader)["Outlines"]);
        Assert.Equal(2, Assert.IsType<NumberObject>(reader.Resolve(outlines["Count"])).IntValue);
        var first = Resolve(reader, outlines["First"]);
        var last = Resolve(reader, outlines["Last"]);
        Assert.Equal("Introduction", Assert.IsType<StringObject>(reader.Resolve(first["Title"])).Value);
        Assert.Equal("Details", Assert.IsType<StringObject>(reader.Resolve(last["Title"])).Value);
        Assert.Same(last, Resolve(reader, first["Next"]));
        Assert.Same(first, Resolve(reader, last["Prev"]));
        Assert.False(first.ContainsKey("Prev"));
        Assert.False(last.ContainsKey("Next"));
    }

    [Fact]
    public void OutlinePageTarget_EmitsExplicitDestination()
    {
        var document = TwoSectionDocument();
        var renderer = new DocumentRenderer();
        renderer.Outline.Add(new OutlineItem("Second page", OutlineTarget.ToPage(1)));

        var reader = BuildTestSupport.Read(document, renderer);
        var outlines = Resolve(reader, ContentTestHelpers.Catalog(reader)["Outlines"]);
        var item = Resolve(reader, outlines["First"]);
        var dest = Assert.IsType<ArrayObject>(reader.Resolve(item["Dest"]));
        Assert.Equal(5, dest.Count);
        Assert.Same(ContentTestHelpers.Kid(reader, 1), Resolve(reader, dest[0]));
        Assert.Equal("XYZ", Assert.IsType<NameObject>(reader.Resolve(dest[1])).Value);
        Assert.Equal(300, Assert.IsType<NumberObject>(reader.Resolve(dest[3])).DoubleValue, 0.5);
    }

    [Fact]
    public void Anchors_EmitNamedDestinations_OnTheRightPages()
    {
        var reader = BuildTestSupport.Read(TwoSectionDocument());
        var dests = NamedDestinations(reader);
        Assert.Equal(2, dests.Count);

        var intro = dests["intro"];
        var details = dests["details"];
        Assert.Same(ContentTestHelpers.Kid(reader, 0), Resolve(reader, intro[0]));
        Assert.Same(ContentTestHelpers.Kid(reader, 1), Resolve(reader, details[0]));
        foreach (var dest in new[] { intro, details })
        {
            Assert.Equal(5, dest.Count);
            Assert.Equal("XYZ", Assert.IsType<NameObject>(reader.Resolve(dest[1])).Value);
            var top = Assert.IsType<NumberObject>(reader.Resolve(dest[3])).DoubleValue;
            Assert.InRange(top, 1, 300);
        }
    }

    [Fact]
    public void RunLinkToAnchor_EmitsGoToLinkAnnotation()
    {
        var document = TwoSectionDocument();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("jump to details").LinkToAnchor = "details";
        document.Sections[0].Blocks.Add(paragraph);

        var reader = BuildTestSupport.Read(document);
        var page = ContentTestHelpers.Kid(reader, 0);
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(page["Annots"]));
        var annot = Resolve(reader, Assert.Single(annots));
        Assert.Equal("Link", Assert.IsType<NameObject>(reader.Resolve(annot["Subtype"])).Value);
        var action = Resolve(reader, annot["A"]);
        Assert.Equal("GoTo", Assert.IsType<NameObject>(reader.Resolve(action["S"])).Value);
        Assert.Equal("details", Assert.IsType<StringObject>(reader.Resolve(action["D"])).Value);

        var dests = NamedDestinations(reader);
        Assert.Same(ContentTestHelpers.Kid(reader, 1), Resolve(reader, dests["details"][0]));
    }

    [Fact]
    public void GeneratedPageRotate_EmitsRotate90()
    {
        var document = new DocumentRenderer().Render(PlainDocument());
        document.Pages[0].Rotate = 90;

        var reader = ContentTestHelpers.Reload(document);
        var page = ContentTestHelpers.Kid(reader, 0);
        Assert.Equal(90, Assert.IsType<NumberObject>(reader.Resolve(page["Rotate"])).IntValue);
    }

    [Fact]
    public void PageRotate_RejectsInvalidAngle()
    {
        var document = new DocumentRenderer().Render(PlainDocument());
        Assert.Throws<ArgumentOutOfRangeException>(() => document.Pages[0].Rotate = 45);
    }

    [Fact]
    public void PlainDocument_EmitsNoNavigationKeys_AndStaysDeterministic()
    {
        var bytes = new DocumentRenderer().ToArray(PlainDocument());
        Assert.Equal(bytes, new DocumentRenderer().ToArray(PlainDocument()));

        var text = Encoding.Latin1.GetString(bytes);
        Assert.DoesNotContain("/Outlines", text);
        Assert.DoesNotContain("/Dests", text);
        Assert.DoesNotContain("/GoTo", text);
        Assert.DoesNotContain("/Rotate", text);

        var reader = DocumentReader.Parse(bytes);
        var page = ContentTestHelpers.Kid(reader, 0);
        Assert.False(page.ContainsKey("Rotate"));
    }
}
