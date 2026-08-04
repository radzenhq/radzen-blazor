#nullable enable
using System;
using System.Globalization;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

    private static string Emitted(Document document) => Emit(new DocumentRenderer().Render(document));

    private static string Tagged(Document document)
        => Emit(new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.Render(document));

    private static string LinkAnnotation(string emission)
        => IndirectObject(emission, References("page", "Annots", 1, Line(emission, "/Type /Page "))[0]);

    [Fact]
    public void LinkedInlineImage_ProducesOneClickableRegionCoveringTheImage()
    {
        var (document, _) = Authored(image => image.Link = "https://www.radzen.com/");

        var emission = Emitted(document);
        var annotation = LinkAnnotation(emission);

        Carries("link annotation", "/Subtype /Link", annotation);
        Carries("link annotation", "/A << /S /URI /URI (https://www.radzen.com/) >>", annotation);

        var rect = Shaped(
            "link annotation",
            @"/Rect \[(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+)\]",
            annotation);
        double Corner(int index) => double.Parse(rect.Groups[index].Value, CultureInfo.InvariantCulture);

        Assert.Equal(Margin, Corner(1), 1.0);
        Assert.Equal(Margin + ImageWidth, Corner(3), 1.0);
        Assert.Equal(ImageHeight, Corner(4) - Corner(2), 1.0);
    }

    [Fact]
    public void UnlinkedInlineImage_ProducesNoClickableRegion()
    {
        var (document, _) = Authored(static _ => { });

        Lacks("document emission", "/Subtype /Link", Emitted(document));
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

        var annotation = LinkAnnotation(Emitted(document));

        Carries("link annotation", "/Subtype /Link", annotation);
        Carries("link annotation", "/A << /S /GoTo /D ", annotation);
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

        var emission = Tagged(document);
        var paragraph = Line(emission, "/S /P ");
        var link = IndirectObject(emission, References("paragraph element", "K", 1, paragraph)[0]);

        Carries("paragraph kid", "/S /Link", link);

        var figure = IndirectObject(
            emission,
            Shaped("link element", @"/K \[(\d+) 0 R", link).Groups[1].Value);

        Carries("link kid", "/S /Figure", figure);
        Carries("link kid", $"/Alt ({image.AlternateText})", figure);
    }

    [Fact]
    public void LinkedInlineImage_JoinsTheAnnotationToTheLinkElementNotTheFigure()
    {
        var (document, _) = Authored(picture =>
        {
            picture.Link = "https://www.radzen.com/";
            picture.AlternateText = "The Radzen logo";
        });
        document.Language = "en";
        document.Info.Title = "Doc";

        var emission = Tagged(document);
        var annotationNumber = References("page", "Annots", 1, Line(emission, "/Type /Page "))[0];
        var paragraph = Line(emission, "/S /P ");
        var link = IndirectObject(emission, References("paragraph element", "K", 1, paragraph)[0]);

        var kids = Shaped(
            "link element",
            $@"/K \[(\d+) 0 R << /Type /OBJR /Pg \d+ 0 R /Obj {annotationNumber} 0 R >>\]",
            link);

        Lacks("figure element", "/OBJR", IndirectObject(emission, kids.Groups[1].Value));
    }

    [Fact]
    public void UnlinkedInlineImage_TagsTheFigureDirectlyUnderTheParagraph()
    {
        var (document, _) = Authored(picture => picture.AlternateText = "The Radzen logo");
        document.Language = "en";
        document.Info.Title = "Doc";

        var emission = Tagged(document);
        var paragraph = Line(emission, "/S /P ");
        var figure = IndirectObject(emission, References("paragraph element", "K", 1, paragraph)[0]);

        Lacks("document emission", "/S /Link", emission);
        Carries("paragraph kid", "/S /Figure", figure);
    }

    [Fact]
    public void InlineImageReplacementText_IsWrittenAsActualTextOnTheTaggedFigure()
    {
        var (document, _) = Authored(picture => picture.ReplacementText = "Radzen");
        document.Language = "en";
        document.Info.Title = "Doc";

        var figure = Line(Tagged(document), "/S /Figure");

        Lacks("figure element", "/Alt ", figure);
        Carries("figure element", "/ActualText (Radzen)", figure);
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
    public void InlineImageRelativeWidth_IsRejectedAtTheSetter()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => Authored(picture => picture.Width = Unit.FromPercent(50)));

        Assert.Contains("InlineImage.Width", error.Message, StringComparison.Ordinal);
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
