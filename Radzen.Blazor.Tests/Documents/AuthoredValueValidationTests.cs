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
        var list = new List();
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
            { "Borders.Width", value => borders.Width = value },
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
            { "List.LeftIndent", value => list.LeftIndent = value },
            { "List.HangingIndent", value => list.HangingIndent = value },
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
}
