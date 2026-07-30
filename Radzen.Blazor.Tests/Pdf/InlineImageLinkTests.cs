#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class InlineImageLinkTests
{
    private const double PageWidth = 300;
    private const double PageHeight = 200;
    private const double Margin = 20;
    private const double ImageWidth = 40;
    private const double ImageHeight = 30;

    private static (Document Document, InlineImage Image) Authored(Action<InlineImage> configure)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margins.SetAll(Unit.FromPoint(Margin));
        var paragraph = section.Blocks.AddParagraph();
        var image = paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(ImageWidth);
        image.Height = Unit.FromPoint(ImageHeight);
        configure(image);
        return (document, image);
    }

    private static List<DictionaryObject> LinkAnnotations(DocumentReader reader)
    {
        var page = ContentTestHelpers.Kid(reader, 0);
        var links = new List<DictionaryObject>();
        if (!page.TryGetValue("Annots", out var annots) || reader.Resolve(annots!) is not ArrayObject array)
        {
            return links;
        }

        for (var i = 0; i < array.Count; i++)
        {
            if (reader.Resolve(array[i]) is DictionaryObject annotation
                && annotation.TryGetValue("Subtype", out var subtype)
                && reader.Resolve(subtype!) is NameObject { Value: "Link" })
            {
                links.Add(annotation);
            }
        }

        return links;
    }

    private static double[] Rect(DocumentReader reader, DictionaryObject annotation)
    {
        var rect = Assert.IsType<ArrayObject>(reader.Resolve(annotation["Rect"]));
        return [.. Enumerable.Range(0, 4).Select(i => ((NumberObject)reader.Resolve(rect[i])).DoubleValue)];
    }

    private static string Uri(DocumentReader reader, DictionaryObject annotation)
    {
        var action = Assert.IsType<DictionaryObject>(reader.Resolve(annotation["A"]));
        return Assert.IsType<StringObject>(reader.Resolve(action["URI"])).Value;
    }

    [Fact]
    public void LinkedInlineImage_ProducesOneClickableRegionCoveringTheImage()
    {
        var (document, _) = Authored(image => image.Link = "https://www.radzen.com/");

        var reader = BuildTestSupport.Read(document);
        var annotation = Assert.Single(LinkAnnotations(reader));

        Assert.Equal("https://www.radzen.com/", Uri(reader, annotation));

        var rect = Rect(reader, annotation);
        Assert.Equal(Margin, rect[0], 1.0);
        Assert.Equal(Margin + ImageWidth, rect[2], 1.0);
        Assert.Equal(ImageHeight, rect[3] - rect[1], 1.0);
    }

    [Fact]
    public void UnlinkedInlineImage_ProducesNoClickableRegion()
    {
        var (document, _) = Authored(static _ => { });

        Assert.Empty(LinkAnnotations(BuildTestSupport.Read(document)));
    }

    [Fact]
    public void InlineImageLinkToAnchor_NavigatesToTheAnchor()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margins.SetAll(Unit.FromPoint(Margin));
        var paragraph = section.Blocks.AddParagraph();
        var image = paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(ImageWidth);
        image.Height = Unit.FromPoint(ImageHeight);
        image.LinkToAnchor = "target";
        section.Blocks.AddParagraph().Inlines.Add("destination").Anchor = "target";

        var reader = BuildTestSupport.Read(document);
        var annotation = Assert.Single(LinkAnnotations(reader));

        var action = Assert.IsType<DictionaryObject>(reader.Resolve(annotation["A"]));

        Assert.Equal("GoTo", Assert.IsType<NameObject>(reader.Resolve(action["S"])).Value);
        Assert.True(action.ContainsKey("D"), "an internal link action carries a destination");
    }

    [Fact]
    public void InlineImageAnchor_IsANavigationTarget()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margins.SetAll(Unit.FromPoint(Margin));
        var paragraph = section.Blocks.AddParagraph();
        var image = paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Anchor = "picture";

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);

        Assert.Contains(page.Anchors, anchor => anchor.Name == "picture");
    }

    [Fact]
    public void LinkedInlineImage_TagsTheFigureInsideALinkElement()
    {
        var (document, image) = Authored(picture =>
        {
            picture.Link = "https://www.radzen.com/";
            picture.AlternateText = "The Radzen logo";
        });
        document.Language = "en";
        document.Info.Title = "Doc";

        var reader = BuildTestSupport.Read(
            document,
            new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 });

        var paragraph = Descendants(reader, TaggedStructureProbe.Root(reader))
            .Single(element => element.Type == "P");
        var link = Assert.Single(paragraph.Children.Where(child => child.Type == "Link"));
        var figure = Assert.Single(link.Children);

        Assert.Equal("Figure", figure.Type);
        Assert.Equal(
            image.AlternateText,
            Assert.IsType<StringObject>(reader.Resolve(figure.Dict["Alt"])).Value);
    }

    [Fact]
    public void UnlinkedInlineImage_TagsTheFigureDirectlyUnderTheParagraph()
    {
        var (document, _) = Authored(picture => picture.AlternateText = "The Radzen logo");
        document.Language = "en";
        document.Info.Title = "Doc";

        var reader = BuildTestSupport.Read(
            document,
            new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 });

        var paragraph = Descendants(reader, TaggedStructureProbe.Root(reader))
            .Single(element => element.Type == "P");

        Assert.DoesNotContain(paragraph.Children, child => child.Type == "Link");
        Assert.Contains(paragraph.Children, child => child.Type == "Figure");
    }

    [Fact]
    public void LinkedInlineImage_KeepsTheOutputByteIdenticalAcrossBuilds()
    {
        var first = Authored(image => image.Link = "https://www.radzen.com/");
        var second = Authored(image => image.Link = "https://www.radzen.com/");

        Assert.Equal(
            new DocumentRenderer().ToArray(first.Document),
            new DocumentRenderer().ToArray(second.Document));
    }

    private static IEnumerable<ProbeElement> Descendants(DocumentReader reader, ProbeElement element)
    {
        yield return element;
        foreach (var child in element.Children)
        {
            foreach (var nested in Descendants(reader, child))
            {
                yield return nested;
            }
        }
    }

    [Fact]
    public void InlineImageSizing_ReadsBackWhatWasSetAndClearsToNatural()
    {
        var (_, image) = Authored(static _ => { });

        Assert.Equal(Unit.FromPoint(ImageWidth), image.Width);
        Assert.Equal(Unit.FromPoint(ImageHeight), image.Height);

        image.Width = null;
        image.Height = null;

        Assert.Null(image.Width);
        Assert.Null(image.Height);
    }

    [Fact]
    public void InlineImageRelativeWidth_ReadsBackWithoutThrowing()
    {
        var (_, image) = Authored(picture => picture.Width = Unit.FromPercent(50));

        Assert.Equal(Unit.FromPercent(50), image.Width);
    }

    [Fact]
    public void InlineImageExposesOnlyMembersItHonors()
    {
        var members = typeof(InlineImage)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Width", "Height", "AlternateText", "Link", "LinkToAnchor", "Anchor", "Opacity",
            },
            members);
    }

    [Theory]
    [InlineData(typeof(PageNumberField))]
    [InlineData(typeof(PageCountField))]
    public void PageFieldsExposeNoAuthoredText(Type field)
    {
        Assert.Null(field.GetProperty("Text"));
        Assert.True(typeof(TextInline).IsAssignableFrom(field));
    }

    [Fact]
    public void PageFieldTextIsNotPartOfTheParagraphText()
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Page ");
        paragraph.Inlines.Add(new PageNumberField());

        Assert.Equal("Page ", paragraph.Text);
    }
}
