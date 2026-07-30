#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;
using ModelDocument = Radzen.Documents.Document;

namespace Radzen.Blazor.Documents.Tests;

public class StyleSystemPublicContractTests
{
    private static TextContent RenderedText(ModelDocument document, string text)
    {
        using var stream = new MemoryStream(new DocumentRenderer().ToArray(document));
        return Radzen.Documents.Pdf.Document
            .LoadFromStream(stream)
            .Pages
            .SelectMany(page => page.Content)
            .OfType<TextContent>()
            .Single(content => content.Text == text);
    }

    private static string TaggedPdf(ModelDocument document)
    {
        document.Language = "en-US";
        document.Info.Title = "Style contracts";
        using var font = Radzen.Blazor.Pdf.Tests.PdfTestResources.Open("Fonts/LiberationSans-Regular.ttf");
        document.Fonts.Register("Contract Sans", font);
        document.Styles.Normal.Font.Family = "Contract Sans";

        return Encoding.Latin1.GetString(new DocumentRenderer
        {
            Accessibility = PdfUaConformance.PdfUa1,
        }.ToArray(document));
    }

    [Fact]
    public void RenderedFontUsesLocalThenNearestStyleThenDefaultPrecedence()
    {
        var document = new ModelDocument();
        var root = document.Styles.Add("Root");
        root.Font.Size = 16;
        root.Font.Color = Color.Red;
        var middle = document.Styles.Add("Middle", "Root");
        middle.Font.Size = 20;
        document.Styles.Add("Leaf", "Middle");
        var section = document.Sections.Add();
        section.Blocks.AddParagraph("Default");
        section.Blocks.AddParagraph("Styled").StyleName = "Leaf";
        var local = section.Blocks.AddParagraph("Local");
        local.StyleName = "Leaf";
        local.Font.Size = 8;
        local.Font.Color = Color.Blue;

        var defaultText = RenderedText(document, "Default");
        var styledText = RenderedText(document, "Styled");
        var localText = RenderedText(document, "Local");

        Assert.Equal(10, defaultText.Font.Size!.Value.Point, 9);
        Assert.Equal(Color.Black, defaultText.Color);
        Assert.Equal(20, styledText.Font.Size!.Value.Point, 9);
        Assert.Equal(Color.Red, styledText.Color);
        Assert.Equal(8, localText.Font.Size!.Value.Point, 9);
        Assert.Equal(Color.Blue, localText.Color);
    }

    [Fact]
    public void HeadingLevelUsesTheNearestStyleInTheBaseChain()
    {
        var document = new ModelDocument();
        var root = document.Styles.Add("Root");
        root.HeadingLevel = 2;
        var middle = document.Styles.Add("Middle", "Root");
        middle.HeadingLevel = 4;
        document.Styles.Add("Leaf", "Middle");
        document.Sections.Add().Blocks.AddParagraph("Heading").StyleName = "Leaf";

        var pdf = TaggedPdf(document);

        Assert.Contains("/S /H4", pdf, StringComparison.Ordinal);
        Assert.DoesNotContain("/S /H2", pdf, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltInHeadingStyleSuppliesItsHeadingLevel()
    {
        var document = new ModelDocument();
        document.Sections.Add().Blocks.AddParagraph("Heading").StyleName = "Heading3";

        Assert.Contains("/S /H3", TaggedPdf(document), StringComparison.Ordinal);
    }

    [Fact]
    public void EveryStyleValueStartsUnsetAndAssignmentsStick()
    {
        var style = new StyleCollection().Add("Contract");

        Assert.Null(style.Alignment);
        Assert.Null(style.SpacingBefore);
        Assert.Null(style.SpacingAfter);
        Assert.Null(style.LeftIndent);
        Assert.Null(style.KeepTogether);
        Assert.Null(style.KeepWithNext);
        Assert.Null(style.HeadingLevel);

        style.Alignment = HorizontalAlignment.Right;
        style.SpacingBefore = "1pt";
        style.SpacingAfter = "2pt";
        style.LeftIndent = "3pt";
        style.KeepTogether = true;
        style.KeepWithNext = true;
        style.HeadingLevel = 5;

        Assert.Equal(HorizontalAlignment.Right, style.Alignment);
        Assert.Equal(Unit.Parse("1pt"), style.SpacingBefore);
        Assert.Equal(Unit.Parse("2pt"), style.SpacingAfter);
        Assert.Equal(Unit.Parse("3pt"), style.LeftIndent);
        Assert.True(style.KeepTogether);
        Assert.True(style.KeepWithNext);
        Assert.Equal(5, style.HeadingLevel);
    }

    [Fact]
    public void AssigningNullResetsEveryStyleValue()
    {
        var style = new StyleCollection().Add("Contract");
        style.Alignment = HorizontalAlignment.Right;
        style.SpacingBefore = "1pt";
        style.SpacingAfter = "2pt";
        style.LeftIndent = "3pt";
        style.KeepTogether = true;
        style.KeepWithNext = true;
        style.HeadingLevel = 5;

        style.Alignment = null;
        style.SpacingBefore = null;
        style.SpacingAfter = null;
        style.LeftIndent = null;
        style.KeepTogether = null;
        style.KeepWithNext = null;
        style.HeadingLevel = null;

        Assert.Null(style.Alignment);
        Assert.Null(style.SpacingBefore);
        Assert.Null(style.SpacingAfter);
        Assert.Null(style.LeftIndent);
        Assert.Null(style.KeepTogether);
        Assert.Null(style.KeepWithNext);
        Assert.Null(style.HeadingLevel);
    }

    [Fact]
    public void UnsetParagraphValuesResolveToTheBuiltInDefaults()
    {
        var document = new ModelDocument();
        var paragraph = document.Sections.Add().Blocks.AddParagraph("Default");

        var format = document.Resolve(paragraph);

        Assert.Equal(HorizontalAlignment.Left, format.Alignment);
        Assert.Equal(default, format.SpacingBefore);
        Assert.Equal(default, format.SpacingAfter);
        Assert.Equal(default, format.LeftIndent);
        Assert.False(format.KeepTogether);
        Assert.False(format.KeepWithNext);
        Assert.Equal(0, format.HeadingLevel);
    }

    [Fact]
    public void ParagraphValuesResolveThroughTheNamedStyleChain()
    {
        var document = new ModelDocument();
        var root = document.Styles.Add("Root");
        root.LeftIndent = "30pt";
        root.KeepWithNext = true;
        root.HeadingLevel = 3;
        var leaf = document.Styles.Add("Leaf", "Root");
        leaf.Alignment = HorizontalAlignment.Center;
        var paragraph = document.Sections.Add().Blocks.AddParagraph("Styled");
        paragraph.StyleName = "Leaf";
        paragraph.SpacingBefore = "4pt";

        var format = document.Resolve(paragraph);

        Assert.Equal(HorizontalAlignment.Center, format.Alignment);
        Assert.Equal(Unit.Parse("4pt"), format.SpacingBefore);
        Assert.Equal(Unit.Parse("30pt"), format.LeftIndent);
        Assert.True(format.KeepWithNext);
        Assert.Equal(3, format.HeadingLevel);
    }

    [Fact]
    public void ResolveRejectsAParagraphOutsideTheDocument()
        => Assert.Throws<ArgumentException>(() => new ModelDocument().Resolve(new Paragraph { Text = "Detached" }));

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void HeadingLevelRejectsValuesOutsideOneThroughSix(int value)
    {
        var style = new StyleCollection().Add("Contract");

        Assert.Throws<ArgumentOutOfRangeException>(() => style.HeadingLevel = value);
    }

    [Fact]
    public void StyleCollectionContainsNormalAndSixHeadingBuiltIns()
    {
        var styles = new StyleCollection();

        Assert.Equal(7, styles.Count);
        Assert.Same(styles.Normal, styles["Normal"]);
        Assert.Null(styles.Normal.BaseStyle);

        for (var level = 1; level <= 6; level++)
        {
            var style = styles[$"Heading{level}"];
            Assert.Equal($"Heading{level}", style.Name);
            Assert.Equal("Normal", style.BaseStyle);
            Assert.Equal(level, style.HeadingLevel);
        }
    }

    [Fact]
    public void StyleCollectionRejectsExactDuplicateNames()
    {
        var styles = new StyleCollection();
        styles.Add("Contract");

        Assert.Throws<ArgumentException>(() => styles.Add("Contract"));
        Assert.Throws<ArgumentException>(() => styles.Add("Normal"));
    }

    [Fact]
    public void StyleCollectionUsesOrdinalCaseSensitiveNames()
    {
        var styles = new StyleCollection();
        var lower = styles.Add("normal");

        Assert.Same(lower, styles["normal"]);
        Assert.NotSame(styles.Normal, lower);
    }

    [Fact]
    public void StyleCollectionLookupMissAndMissingBaseFailLoudly()
    {
        var styles = new StyleCollection();

        Assert.Throws<KeyNotFoundException>(() => styles["Absent"]);
        Assert.Throws<ArgumentException>(() => styles.Add("Leaf", "Absent"));
    }

    [Fact]
    public void MissingBaseStyleReferenceFailsWhenRenderedWithTheFullChain()
    {
        var document = new ModelDocument();
        document.Styles.Add("Leaf").BaseStyle = "Absent";
        document.Sections.Add().Blocks.AddParagraph("Text").StyleName = "Leaf";

        var error = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().Render(document));

        Assert.Contains("Leaf -> Absent", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BaseStyleCycleIsRejectedWhenRenderedWithTheFullChain()
    {
        var document = new ModelDocument();
        var first = document.Styles.Add("First");
        document.Styles.Add("Second", "First");
        first.BaseStyle = "Second";
        document.Sections.Add().Blocks.AddParagraph("Text").StyleName = "First";

        var error = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().Render(document));

        Assert.Contains("cyclic", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("First -> Second -> First", error.Message, StringComparison.Ordinal);
    }
}
