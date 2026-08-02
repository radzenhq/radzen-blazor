#nullable enable
using System;
using System.Globalization;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

public class AuthoredValueValidationTests
{
    public static TheoryData<double> NonFinite() => new()
    {
        double.NaN,
        double.PositiveInfinity,
        double.NegativeInfinity,
    };

    [Theory]
    [MemberData(nameof(NonFinite))]
    public void ContainerRotationRejectsNonFiniteValues(double value)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Container().Rotation = value);

    [Theory]
    [MemberData(nameof(NonFinite))]
    public void ParagraphLineSpacingRejectsNonFiniteValues(double value)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Paragraph().LineSpacing = value);

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void ParagraphLineSpacingRejectsNonPositiveValues(double value)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Paragraph().LineSpacing = value);

    [Fact]
    public void ParagraphWidowsAndOrphansRejectNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Paragraph().Widows = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Paragraph().Orphans = -1);
    }

    [Fact]
    public void ParagraphWidowsAndOrphansAcceptZero()
    {
        var paragraph = new Paragraph { Widows = 0, Orphans = 0 };

        Assert.Equal(0, paragraph.Widows);
        Assert.Equal(0, paragraph.Orphans);
    }

    [Fact]
    public void QrCodeQuietZoneModulesRejectsNegativeValues()
    {
        var code = new Document().Sections.Add().Blocks.AddQrCode("payload", Unit.FromPoint(50));

        Assert.Throws<ArgumentOutOfRangeException>(() => code.QuietZoneModules = -1);
        code.QuietZoneModules = 0;
        Assert.Equal(0, code.QuietZoneModules);
    }

    [Theory]
    [MemberData(nameof(NonFinite))]
    public void TextInlineScalesRejectNonFiniteValues(double value)
    {
        var run = new Paragraph().Inlines.Add("text");

        Assert.Throws<ArgumentOutOfRangeException>(() => run.HorizontalScale = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => run.VerticalAlignmentScale = value);
    }

    [Fact]
    public void ColumnRelativeWidthRejectsInfinity()
    {
        var column = new Document().Sections.Add().Blocks.AddTable().Columns.Add();

        Assert.Throws<ArgumentOutOfRangeException>(() => column.RelativeWidth = double.PositiveInfinity);
    }

    [Theory]
    [MemberData(nameof(NonFinite))]
    public void MatrixFactoriesRejectNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix.Scale(value, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix.Scale(1, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix.Translate(value, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix.Translate(0, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix.Rotate(value));
    }

    [Theory]
    [InlineData("12pt")]
    [InlineData("-3.5pt")]
    [InlineData("0pt")]
    [InlineData("50%")]
    [InlineData("-12.25%")]
    public void UnitToStringRoundTripsThroughParse(string text)
    {
        var unit = Unit.Parse(text);

        Assert.Equal(text, unit.ToString());
        Assert.Equal(unit, Unit.Parse(unit.ToString()));
    }

    [Theory]
    [InlineData("9cm")]
    [InlineData("5mm")]
    [InlineData("1in")]
    public void UnitToStringNormalizesAbsoluteMeasurementsToPoints(string text)
    {
        var unit = Unit.Parse(text);

        Assert.EndsWith("pt", unit.ToString(), StringComparison.Ordinal);
        Assert.Equal(unit, Unit.Parse(unit.ToString()));
    }

    [Fact]
    public void UnitToStringIsCultureInvariant()
    {
        var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1.5pt", Unit.FromPoint(1.5).ToString());
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        }
    }

    [Fact]
    public void ColorToStringRoundTripsThroughFromHex()
    {
        Assert.Equal("#FF8000", Color.FromRgb(255, 128, 0).ToString());
        Assert.Equal("#FF800080", Color.FromArgb(128, 255, 128, 0).ToString());
        Assert.Equal(Color.FromRgb(255, 128, 0), Color.FromHex(Color.FromRgb(255, 128, 0).ToString()));
        Assert.Equal(
            Color.FromArgb(128, 255, 128, 0),
            Color.FromHex(Color.FromArgb(128, 255, 128, 0).ToString()));
    }

    [Fact]
    public void ColorToStringOmitsAnOpaqueAlphaChannel()
        => Assert.Equal("#000000", Color.Black.ToString());

    public static TheoryData<string, Action<Unit>> AbsoluteOnlyMeasurements()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var table = new Table();
        var container = new Container();
        var column = table.Columns.Add();
        var cell = table.Rows.Add().Cells[0];
        var paragraph = new Paragraph();
        var run = new Run("x");
        var list = new ListBlock();
        var style = document.Styles.Add("guarded");
        var toc = new TableOfContents();
        var shadow = new BoxShadow();
        var borders = new Borders();

        return new()
        {
            { "Margins.Top", value => section.Margins.Top = value },
            { "Margins.Right", value => section.Margins.Right = value },
            { "Margins.Bottom", value => section.Margins.Bottom = value },
            { "Margins.Left", value => section.Margins.Left = value },
            { "Margins.SetAll", section.Margins.SetAll },
            { "Section.HeaderDistance", value => section.HeaderDistance = value },
            { "Section.FooterDistance", value => section.FooterDistance = value },
            { "PageSize.Width", value => _ = new PageSize(value, Unit.FromPoint(10)) },
            { "PageSize.Height", value => _ = new PageSize(Unit.FromPoint(10), value) },
            { "Border.Width", value => borders.Top.Width = value },
            { "BoxShadow.BlurRadius", value => shadow.BlurRadius = value },
            { "BoxShadow.OffsetX", value => shadow.OffsetX = value },
            { "BoxShadow.OffsetY", value => shadow.OffsetY = value },
            { "BoxShadow.Spread", value => shadow.Spread = value },
            { "Cell.Padding", value => cell.Padding = value },
            { "Cell.PaddingLeft", value => cell.PaddingLeft = value },
            { "Cell.PaddingRight", value => cell.PaddingRight = value },
            { "Cell.PaddingTop", value => cell.PaddingTop = value },
            { "Cell.PaddingBottom", value => cell.PaddingBottom = value },
            { "Column.Width", value => column.Width = value },
            { "Container.Padding", value => container.Padding = value },
            { "Container.PaddingLeft", value => container.PaddingLeft = value },
            { "Container.PaddingRight", value => container.PaddingRight = value },
            { "Container.PaddingTop", value => container.PaddingTop = value },
            { "Container.PaddingBottom", value => container.PaddingBottom = value },
            { "Container.CornerRadius", value => container.CornerRadius = value },
            { "Container.Width", value => container.Width = value },
            { "TextInline.LetterSpacing", value => run.LetterSpacing = value },
            { "TextInline.WordSpacing", value => run.WordSpacing = value },
            { "ListBlock.LeftIndent", value => list.LeftIndent = value },
            { "ListBlock.HangingIndent", value => list.HangingIndent = value },
            { "Paragraph.LeftIndent", value => paragraph.LeftIndent = value },
            { "Paragraph.SpacingBefore", value => paragraph.SpacingBefore = value },
            { "Paragraph.SpacingAfter", value => paragraph.SpacingAfter = value },
            { "Style.SpacingBefore", value => style.SpacingBefore = value },
            { "Style.SpacingAfter", value => style.SpacingAfter = value },
            { "Style.LeftIndent", value => style.LeftIndent = value },
            { "Table.Width", value => table.Width = value },
            { "Table.LeftIndent", value => table.LeftIndent = value },
            { "Table.CornerRadius", value => table.CornerRadius = value },
            { "TableOfContents.LevelIndent", value => toc.LevelIndent = value },
            { "TabStop.Position", value => _ = new TabStop(value) },
            { "Font.Size", value => run.Font.Size = value },
        };
    }

    [Theory]
    [MemberData(nameof(AbsoluteOnlyMeasurements))]
    public void AbsoluteOnlyMeasurementsRejectPercentages(string subject, Action<Unit> assign)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => assign(Unit.FromPercent(50)));

        Assert.Contains(subject.Replace(".SetAll", ".Top", StringComparison.Ordinal), error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSizeRejectsNonPositiveDimensions(double value)
    {
        var width = Assert.Throws<ArgumentOutOfRangeException>(() => new PageSize(Unit.FromPoint(value), Unit.FromPoint(100)));
        var height = Assert.Throws<ArgumentOutOfRangeException>(() => new PageSize(Unit.FromPoint(100), Unit.FromPoint(value)));

        Assert.Contains("PageSize.Width", width.Message, StringComparison.Ordinal);
        Assert.Contains("PageSize.Height", height.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void ImageFitBoxRejectsNonPositiveBounds(double value)
    {
        var image = new Document().Sections.Add().Blocks.AddImage(new System.IO.MemoryStream());

        var width = Assert.Throws<ArgumentOutOfRangeException>(() => image.FitBox = (Unit.FromPoint(value), Unit.FromPoint(10)));
        var height = Assert.Throws<ArgumentOutOfRangeException>(() => image.FitBox = (Unit.FromPoint(10), Unit.FromPoint(value)));

        Assert.Contains("Image.FitBox.MaxWidth", width.Message, StringComparison.Ordinal);
        Assert.Contains("Image.FitBox.MaxHeight", height.Message, StringComparison.Ordinal);
        Assert.Null(image.FitBox);
    }

    [Fact]
    public void ImageFitBoxRejectsRelativeBounds()
    {
        var image = new Document().Sections.Add().Blocks.AddImage(new System.IO.MemoryStream());

        Assert.Throws<ArgumentOutOfRangeException>(() => image.FitBox = (Unit.FromPercent(50), Unit.FromPoint(10)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ImageDimensionsRejectNonPositiveValues(double value)
    {
        var image = new Document().Sections.Add().Blocks.AddImage(new System.IO.MemoryStream());

        Assert.Throws<ArgumentOutOfRangeException>(() => image.Width = Unit.FromPoint(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Height = Unit.FromPoint(value));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("de-CH-1901")]
    [InlineData("english")]
    [InlineData("sr-Latn-RS")]
    [InlineData("zh-cmn-Hans-CN")]
    [InlineData("es-419")]
    [InlineData("en-US-u-va-posix")]
    [InlineData("x-private")]
    [InlineData("EN-us")]
    public void DocumentLanguageAcceptsWellFormedTags(string tag)
        => Assert.Equal(tag, new Document { Language = tag }.Language);

    [Theory]
    [InlineData("")]
    [InlineData("en_US")]
    [InlineData("en-")]
    [InlineData("-en")]
    [InlineData("en--US")]
    [InlineData("en-toolongsubtag")]
    [InlineData("i-klingon")]
    [InlineData("e")]
    [InlineData("en US")]
    [InlineData("en-US-")]
    [InlineData("en-a")]
    public void DocumentLanguageRejectsMalformedTags(string tag)
    {
        var error = Assert.Throws<ArgumentException>(() => new Document { Language = tag });

        Assert.Contains("Document.Language", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentLanguageAcceptsNull()
        => Assert.Null(new Document { Language = null }.Language);

    [Fact]
    public void InlineLanguageRejectsMalformedTags()
    {
        var paragraph = new Document().Sections.Add().Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("text");

        Assert.Throws<ArgumentException>(() => run.Language = "not a tag");
        run.Language = "la";
        Assert.Equal("la", run.Language);
    }
}
