#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedLinkStructureTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private static string Rendered(Document document, DocumentRenderer? renderer = null)
        => Encoding.Latin1.GetString((renderer ?? new DocumentRenderer()).ToArray(document));

    private static string Element(string type) => $"/Type /StructElem /S /{type} /P ";

    private static string ElementNumber(string emission, string type)
        => Shaped(
            $"{type} element",
            $@"(\d+) 0 obj\n<< {Regex.Escape(Element(type))}",
            emission).Groups[1].Value;

    private static string StructureRoot(string emission)
        => IndirectObject(
            emission,
            Shaped("catalog", @"/StructTreeRoot (\d+) 0 R", Line(emission, "/Type /Catalog")).Groups[1].Value);

    private static string Kids(string subject, string element)
        => Shaped(subject, @"/K \[([^\]]*)\]", element).Groups[1].Value;

    private static string[] ChildElements(string kids)
        => [.. Regex.Matches(Regex.Replace(kids, "<< [^>]*>>", " "), @"(\d+) 0 R")
            .Select(match => match.Groups[1].Value)];

    private static string[] ObjectReferences(string kids)
        => [.. Regex.Matches(kids, @"<< /Type /OBJR /Pg \d+ 0 R /Obj (\d+) 0 R >>")
            .Select(match => match.Groups[1].Value)];

    private static int[] Mcids(string kids)
        => [.. Regex.Matches(Regex.Replace(kids, @"<< [^>]*>>|\d+ 0 R", " "), @"\d+")
            .Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture))];

    private static string ParentTreeOwner(string emission, string structureRoot, int key)
    {
        var tree = IndirectObject(
            emission,
            Shaped("structure root", @"/ParentTree (\d+) 0 R", structureRoot).Groups[1].Value);
        var nums = Shaped("parent tree", @"/Nums \[([^\]]*)\]", tree).Groups[1].Value;

        foreach (Match pair in Regex.Matches(nums, @"(\d+) (\d+) 0 R"))
        {
            if (int.Parse(pair.Groups[1].Value, CultureInfo.InvariantCulture) == key)
            {
                return pair.Groups[2].Value;
            }
        }

        Assert.Fail($"The parent tree has no entry {key}.\nparent tree:\n{Excerpt(nums)}");
        return string.Empty;
    }

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

    [Fact]
    public void UriLink_CarriesItsVisibleTextAsContents()
    {
        var emission = Rendered(Authored("https://www.radzen.com", null), Accessible());
        var annotation = References("page", "Annots", 1, Line(emission, "/Type /Page "))[0];

        Carries($"link annotation {annotation} 0 R", "/Contents (Radzen)", IndirectObject(emission, annotation));
    }

    [Fact]
    public void AnchorLink_CarriesItsVisibleTextAsContents()
    {
        var emission = Rendered(Authored(null, "target"), Accessible());
        var annotation = References("page", "Annots", 1, Line(emission, "/Type /Page "))[0];

        Carries($"link annotation {annotation} 0 R", "/Contents (Radzen)", IndirectObject(emission, annotation));
    }

    [Fact]
    public void UntaggedUriLink_CarriesNoContents()
    {
        var emission = Rendered(Authored("https://www.radzen.com", null));
        var annotation = References("page", "Annots", 1, Line(emission, "/Type /Page "))[0];

        Lacks($"link annotation {annotation} 0 R", "/Contents", IndirectObject(emission, annotation));
    }

    [Fact]
    public void TaggedAnnotatedPage_DeclaresStructureTabOrder()
    {
        var emission = Rendered(Authored("https://www.radzen.com", null), Accessible());

        Carries("page", "/Tabs /S", Line(emission, "/Type /Page "));
    }

    [Fact]
    public void UntaggedAnnotatedPage_DeclaresNoTabOrder()
    {
        var emission = Rendered(Authored("https://www.radzen.com", null));

        Lacks("page", "/Tabs", Line(emission, "/Type /Page "));
    }

    [Fact]
    public void TaggedPageWithoutAnnotations_DeclaresNoTabOrder()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "No links";
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.AddText(document.Sections.Add(), "Plain body", BuildTestSupport.Latin);

        var emission = Rendered(document, Accessible());

        Lacks("page", "/Tabs", Line(emission, "/Type /Page "));
    }

    [Fact]
    public void UriLink_IsALinkElementInsideItsParagraph()
    {
        var emission = Rendered(Authored("https://www.radzen.com", null), Accessible());
        var paragraph = Line(emission, Element("P"));

        var child = Assert.Single(ChildElements(Kids("paragraph element", paragraph)));

        Carries($"paragraph child {child} 0 R", Element("Link"), IndirectObject(emission, child));
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
        var emission = Rendered(Authored("https://www.radzen.com", null), Accessible());
        var linkNumber = ElementNumber(emission, "Link");
        var objectReferences = ObjectReferences(Kids("link element", IndirectObject(emission, linkNumber)));

        var referenced = Assert.Single(objectReferences);
        var annotation = IndirectObject(emission, referenced);
        Carries($"annotation {referenced} 0 R", "/Subtype /Link", annotation);

        var annots = References("page", "Annots", 1, Line(emission, "/Type /Page "));
        Assert.True(
            annots[0] == referenced,
            $"The page carries annotation {annots[0]} 0 R but the OBJR points at {referenced} 0 R.");

        var owner = ParentTreeOwner(emission, StructureRoot(emission), NumberIn(annotation, "StructParent"));
        Assert.True(
            owner == linkNumber,
            $"The parent tree maps /StructParent to {owner} 0 R, not the link element {linkNumber} 0 R.");
    }

    [Fact]
    public void AnchorLink_IsAlsoWiredIntoTheStructureTree()
    {
        var emission = Rendered(Authored(null, "target"), Accessible());
        var kids = Kids("link element", IndirectObject(emission, ElementNumber(emission, "Link")));

        Assert.Single(ObjectReferences(kids));
        Assert.NotEmpty(Mcids(kids));
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

        var emission = Rendered(document, Accessible());
        var structureRoot = StructureRoot(emission);
        var navigation = IndirectObject(emission, ElementNumber(emission, "TOC"));
        var entries = ChildElements(Kids("TOC element", navigation));

        Assert.True(entries.Length == 2, $"Expected 2 TOCI entries, found {entries.Length}.");

        foreach (var entryNumber in entries)
        {
            var entry = IndirectObject(emission, entryNumber);
            Carries($"TOC entry {entryNumber} 0 R", Element("TOCI"), entry);

            var referenceNumber = Assert.Single(ChildElements(Kids($"TOC entry {entryNumber} 0 R", entry)));
            var reference = IndirectObject(emission, referenceNumber);
            Carries($"reference {referenceNumber} 0 R", Element("Reference"), reference);

            var linkNumber = Assert.Single(ChildElements(Kids($"reference {referenceNumber} 0 R", reference)));
            var link = IndirectObject(emission, linkNumber);
            Carries($"link {linkNumber} 0 R", Element("Link"), link);

            var kids = Kids($"link {linkNumber} 0 R", link);
            Assert.NotEmpty(Mcids(kids));

            var objectReferences = ObjectReferences(kids);
            Assert.True(
                objectReferences.Length == 2,
                $"Expected the link element {linkNumber} 0 R to carry 2 OBJRs, found {objectReferences.Length}.");

            foreach (var annotationNumber in objectReferences)
            {
                var annotation = IndirectObject(emission, annotationNumber);
                var owner = ParentTreeOwner(emission, structureRoot, NumberIn(annotation, "StructParent"));
                Assert.True(
                    owner == linkNumber,
                    $"The parent tree maps annotation {annotationNumber} 0 R to {owner} 0 R,"
                    + $" not the link element {linkNumber} 0 R.");
            }
        }
    }
}
