#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedLinkStructureTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private static Document Authored(string? uri, string? anchor)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Link";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        paragraph.Inlines.Add("See ").Font.Family = BuildTestSupport.Latin;
        var link = paragraph.Inlines.Add("Radzen");
        link.Font.Family = BuildTestSupport.Latin;
        link.Link = uri;
        link.LinkToAnchor = anchor;
        paragraph.Inlines.Add(" now.").Font.Family = BuildTestSupport.Latin;

        if (anchor is not null)
        {
            var target = document.Sections.Add().Blocks.AddParagraph().Inlines.Add("Target");
            target.Font.Family = BuildTestSupport.Latin;
            target.Anchor = anchor;
        }

        return document;
    }

    private static List<DictionaryObject> Annotations(DocumentReader reader, int pageIndex)
    {
        var page = BuildTestSupport.PageLeaves(reader)[pageIndex].Page;
        var annotations = new List<DictionaryObject>();
        if (page.TryGetValue("Annots", out var annots) && reader.Resolve(annots!) is ArrayObject array)
        {
            foreach (var item in array)
            {
                annotations.Add(Assert.IsType<DictionaryObject>(reader.Resolve(item)));
            }
        }

        return annotations;
    }

    [Fact]
    public void UriLink_IsALinkElementInsideItsParagraph()
    {
        var reader = BuildTestSupport.Read(Authored("https://www.radzen.com", null), Accessible());
        var root = TaggedStructureProbe.Root(reader);

        var paragraph = TaggedStructureProbe.Single(root, "P");
        var link = Assert.Single(paragraph.Children);
        Assert.Equal("Link", link.Type);
    }

    [Fact]
    public void UriLink_OwnsTheMarkedLinkTextAndKeepsTheSurroundingRunsInTheParagraph()
    {
        var reader = BuildTestSupport.Read(Authored("https://www.radzen.com", null), Accessible());
        var structRoot = TaggedStructureProbe.StructRoot(reader);
        var root = TaggedStructureProbe.Root(reader);
        var paragraph = TaggedStructureProbe.Single(root, "P");
        var link = TaggedStructureProbe.Single(root, "Link");

        Assert.NotEmpty(link.Mcids);
        Assert.NotEmpty(paragraph.Mcids);

        foreach (var mcid in link.Mcids)
        {
            Assert.Same(link.Dict, TaggedStructureProbe.OwnerOfMcid(reader, structRoot, 0, mcid));
        }

        foreach (var mcid in paragraph.Mcids)
        {
            Assert.Same(paragraph.Dict, TaggedStructureProbe.OwnerOfMcid(reader, structRoot, 0, mcid));
        }

        var marked = TaggedStructureProbe.MarkedContentInOrder(reader, 0);
        Assert.Equal(
            new[] { "P", "Link", "P" },
            marked.Select(entry => entry.Tag).ToArray());
    }

    [Fact]
    public void UriLink_AnnotationIsReachedByObjrAndPointsBackThroughStructParent()
    {
        var reader = BuildTestSupport.Read(Authored("https://www.radzen.com", null), Accessible());
        var structRoot = TaggedStructureProbe.StructRoot(reader);
        var link = TaggedStructureProbe.Single(TaggedStructureProbe.Root(reader), "Link");

        var objr = Assert.Single(link.ObjectReferences);
        var annotation = Assert.IsType<DictionaryObject>(reader.Resolve(objr["Obj"]!));
        Assert.Equal("Link", BuildTestSupport.Name(reader, annotation, "Subtype"));

        var referenced = Assert.Single(Annotations(reader, 0));
        Assert.Same(referenced, annotation);

        Assert.True(annotation.TryGetValue("StructParent", out var key), "the annotation carries /StructParent");
        var structParent = Assert.IsType<NumberObject>(reader.Resolve(key!)).IntValue;
        var owner = Assert.IsType<DictionaryObject>(
            TaggedStructureProbe.ParentTreeEntry(reader, structRoot, structParent));
        Assert.Same(link.Dict, owner);
    }

    [Fact]
    public void AnchorLink_IsAlsoWiredIntoTheStructureTree()
    {
        var reader = BuildTestSupport.Read(Authored(null, "target"), Accessible());
        var link = TaggedStructureProbe.Single(TaggedStructureProbe.Root(reader), "Link");

        Assert.Single(link.ObjectReferences);
        Assert.NotEmpty(link.Mcids);
    }

    [Fact]
    public void UntaggedOutput_KeepsLinkTextInTheParagraph()
    {
        var reader = BuildTestSupport.Read(Authored("https://www.radzen.com", null));
        var types = new List<string>();
        StructureTestHelpers.CollectTypes(reader, StructureTestHelpers.RootKids(reader), types);

        Assert.DoesNotContain("Link", types);
        Assert.All(
            TaggedStructureProbe.MarkedContentInOrder(reader, 0),
            entry => Assert.Equal("P", entry.Tag));
    }

    [Fact]
    public void TaggedTableOfContents_RendersEntriesAsTocTociReferenceAndLink()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Navigation";
        BuildTestSupport.RegisterLatin(document);

        var front = document.Sections.Add();
        var toc = front.Blocks.AddTableOfContents();
        toc.Font.Family = BuildTestSupport.Latin;
        toc.AddEntry("Chapter One", "ch1");
        toc.AddEntry("Chapter Two", "ch2", level: 1);

        foreach (var (anchor, text) in new[] { ("ch1", "Chapter one body"), ("ch2", "Chapter two body") })
        {
            var section = document.Sections.Add();
            var run = section.Blocks.AddParagraph().Inlines.Add(text);
            run.Font.Family = BuildTestSupport.Latin;
            run.Anchor = anchor;
        }

        var reader = BuildTestSupport.Read(document, Accessible());
        var structRoot = TaggedStructureProbe.StructRoot(reader);
        var root = TaggedStructureProbe.Root(reader);

        var navigation = TaggedStructureProbe.Single(root, "TOC");
        Assert.Equal(2, navigation.Children.Count);
        foreach (var entry in navigation.Children)
        {
            Assert.Equal("TOCI", entry.Type);
            var reference = Assert.Single(entry.Children);
            Assert.Equal("Reference", reference.Type);
            var link = Assert.Single(reference.Children);
            Assert.Equal("Link", link.Type);
            Assert.NotEmpty(link.Mcids);

            foreach (var objr in link.ObjectReferences)
            {
                var annotation = Assert.IsType<DictionaryObject>(reader.Resolve(objr["Obj"]!));
                Assert.True(annotation.TryGetValue("StructParent", out var key), "the annotation carries /StructParent");
                var structParent = Assert.IsType<NumberObject>(reader.Resolve(key!)).IntValue;
                Assert.Same(
                    link.Dict,
                    Assert.IsType<DictionaryObject>(TaggedStructureProbe.ParentTreeEntry(reader, structRoot, structParent)));
            }

            Assert.Equal(2, link.ObjectReferences.Count);
        }
    }
}
