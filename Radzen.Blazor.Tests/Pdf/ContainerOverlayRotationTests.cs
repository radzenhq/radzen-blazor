#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Blazor.Tests.Isolated;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Pdf.Tests;

public class ContainerOverlayRotationTests
{

    [Fact]
    public void Overlay_ChildrenShareBoxOrigin_DecorationCoversTallestChild()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var container = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(230, 230, 230),
        });
        var shortText = container.Blocks.Add(PaginationSupport.Text("Short"));
        var tallText = container.Blocks.Add(PaginationSupport.Text("Tall child that wraps onto multiple lines because the box is only four hundred points wide and this text is long"));

        var capture = new LayoutCaptureContext(ImageProbes.None);
        var pages = IsolatedPaginator.PaginateIsolated(section, fonts, capture: capture);
        var page = Assert.Single(pages);

        Assert.Empty(page.Body.Tables);
        var box = Assert.Single(page.Body.Boxes);
        Assert.Equal(capture.Source(container), box.Source);
        Assert.Null(box.Transform);
        Assert.Equal(0, box.Bounds.Y, 6);
        Assert.Equal(0, box.Bounds.X, 6);
        Assert.Equal(400, box.Bounds.Width, 6);

        Assert.Equal(container.Background, box.Style.Background);

        var shortSource = capture.Source(shortText);
        var tallSource = capture.Source(tallText);
        var firstShort = box.Content.Lines.First(l => l.Source == shortSource);
        var firstTall = box.Content.Lines.First(l => l.Source == tallSource);
        Assert.Equal(10, firstShort.X, 6);
        Assert.Equal(10, firstTall.X, 6);
        Assert.Equal(10, firstShort.Y, 6);
        Assert.Equal(firstShort.Y, firstTall.Y, 6);

        Assert.True(
            box.Content.Lines.ToList().FindIndex(l => l.Source == shortSource)
            < box.Content.Lines.ToList().FindIndex(l => l.Source == tallSource));

        var tallHeight = box.Content.Lines.Where(l => l.Source == tallSource).Sum(l => l.Line.Height);
        var shortHeight = box.Content.Lines.Where(l => l.Source == shortSource).Sum(l => l.Line.Height);
        Assert.True(tallHeight > shortHeight);
        Assert.Equal(tallHeight + 20, box.Bounds.Height, 6);
    }

    [Fact]
    public void Overlay_BuildsContentStream_BothChildrenAtSameOrigin_InDeclarationOrder()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Padding = Unit.FromPoint(6),
            Background = Color.FromRgb(200, 200, 200),
        });
        container.Blocks.AddParagraph().Inlines.Add("UNDERLAY");
        container.Blocks.AddParagraph().Inlines.Add("OVERLAY");

        var pdf = new DocumentRenderer().Render(document);
        var page = Assert.Single(pdf.Pages);
        var content = Encoding.ASCII.GetString(page.GetContent()!);

        Assert.True(ContentOperationTestHelpers.HasSequence(page.GetContent()!, "re", "f"));
        var under = content.IndexOf("(UNDERLAY) Tj", StringComparison.Ordinal);
        var over = content.IndexOf("(OVERLAY) Tj", StringComparison.Ordinal);
        Assert.True(under >= 0, "first child present");
        Assert.True(over >= 0, "second child present");
        Assert.True(under < over, "children paint in declaration order (later on top)");

        var underTd = TdBefore(content, "UNDERLAY");
        var overTd = TdBefore(content, "OVERLAY");
        Assert.Equal(underTd.X, overTd.X, 4);
        Assert.Equal(underTd.Y, overTd.Y, 4);
    }

    [Fact]
    public void OverlayContainer_InsideAnotherContainer_Throws()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var outer = section.Blocks.Add(new Container());
        var inner = outer.Blocks.Add(new Container { Layout = ContainerLayout.Overlay });
        inner.Blocks.Add(PaginationSupport.Text("Nested"));

        Assert.Throws<NotSupportedException>(() => IsolatedPaginator.PaginateIsolated(section, fonts));
    }


    [Fact]
    public void RotatedContainer_BoxCarriesPivotCenteredRotationMatrix()
    {
        var fonts = PaginationSupport.Fonts();
        var section = PaginationSupport.Section(400, 600);
        var container = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(200),
            Rotation = 30,
        });
        container.Blocks.Add(PaginationSupport.Text("Tilted"));

        var pages = IsolatedPaginator.PaginateIsolated(section, fonts);
        var page = Assert.Single(pages);

        Assert.Empty(page.Body.Tables);
        var box = Assert.Single(page.Body.Boxes);
        Assert.True(box.Transform.HasValue, "box carries the rotation transform");
        var transform = box.Transform!.Value;

        var (cos, sin) = (Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6));
        Assert.Equal(cos, transform.A, 9);
        Assert.Equal(-sin, transform.B, 9);
        Assert.Equal(sin, transform.C, 9);
        Assert.Equal(cos, transform.D, 9);

        var centerX = 100.0;
        var centerY = box.Bounds.Height / 2;
        var (px, py) = transform.Transform(centerX, centerY);
        Assert.Equal(centerX, px, 9);
        Assert.Equal(centerY, py, 9);
        var (qx, qy) = transform.Transform(centerX + 1, centerY);
        Assert.Equal(centerX + cos, qx, 9);
        Assert.Equal(centerY - sin, qy, 9);
    }

    [Fact]
    public void ApplyTransform_RotatesEdges_ConvertsFillsToSolidStrokes_TagsTexts()
    {
        var plan = new PagePlan { Size = PageSizes.A4 };
        plan.Fills.Add(Fill(0, 0, 10, 10));

        var mark = plan.Mark();
        plan.Fills.Add(Fill(10, 20, 100, 40));
        plan.Edges.Add(new EdgeDraw
        {
            X1 = 10,
            Y1 = 20,
            X2 = 110,
            Y2 = 20,
            LineWidth = 2,
            Color = Color.Black,
            Style = BorderStyle.Solid,
        });
        plan.Texts.Add(Text(15, 30));

        var rotate90 = Matrix.Rotate(90);
        PageDrawTransformer.ApplyTransform(plan, rotate90, mark);

        var fill = Assert.Single(plan.Fills);
        Assert.Equal(10, fill.Width, 6);

        Assert.Equal(2, plan.Edges.Count);
        var converted = plan.Edges[0];
        Assert.Equal(BorderStyle.Solid, converted.Style);
        Assert.Equal(40, converted.LineWidth, 6);
        Assert.Equal(-40, converted.X1, 6);
        Assert.Equal(10, converted.Y1, 6);
        Assert.Equal(-40, converted.X2, 6);
        Assert.Equal(110, converted.Y2, 6);

        var border = plan.Edges[1];
        Assert.Equal(-20, border.X1, 6);
        Assert.Equal(10, border.Y1, 6);
        Assert.Equal(-20, border.X2, 6);
        Assert.Equal(110, border.Y2, 6);
        Assert.Equal(2, border.LineWidth, 6);

        var text = Assert.Single(plan.Texts);
        Assert.Equal(rotate90, text.Transform);
    }

    [Fact]
    public void WriteTextDraw_WithTransform_EmitsBalancedQ_Cm_Q_WithRotationOperands()
    {
        var transform = Matrix.Translate(-100, -200) * Matrix.Rotate(30) * Matrix.Translate(100, 200);
        var text = Text(72, 700) with { Transform = transform };

        using var writer = new ContentWriter();
        DrawWriter.WriteTextDraw(
            writer,
            text,
            new PlannedFont(new OutputFont(text.Font.Key, text.Font.Base14, null), null));
        var drawn = writer.ToArray();

        var operators = ContentOperationTestHelpers.Operators(drawn);
        Assert.Equal(1, operators.Count(op => op == "q"));
        Assert.Equal(1, operators.Count(op => op == "Q"));
        Assert.Equal("q", operators[0]);
        Assert.Equal("Q", operators[^1]);

        var cm = Assert.Single(ContentStreamTokenizer.Parse(drawn), op => op.Operator == "cm");
        var (cos, sin) = (Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6));
        double[] expected = [cos, sin, -sin, cos, 100 - (100 * cos) + (200 * sin), 200 - (100 * sin) - (200 * cos)];
        Assert.Equal(6, cm.Operands.Count);
        for (var i = 0; i < 6; i++)
        {
            Assert.True(Math.Abs(expected[i] - cm.Num(i)) < 1e-3, $"cm operand {i}: expected {expected[i]}, got {cm.Num(i)}");
        }

        Assert.True(operators.IndexOf("cm") < operators.IndexOf("BT"));
    }

    [Fact]
    public void RotatedContainer_WithShadow_Throws()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(200),
            Rotation = 30,
            Background = Color.FromRgb(255, 255, 255),
            Shadow = new BoxShadow
            {
                Color = Color.FromArgb(160, 0, 0, 0),
                BlurRadius = Unit.FromPoint(8),
                OffsetX = Unit.FromPoint(2),
                OffsetY = Unit.FromPoint(3),
            },
        });
        container.Blocks.AddParagraph().Inlines.Add("Tilted");

        Assert.Throws<NotSupportedException>(() => new DocumentRenderer().Render(document));
    }

    [Fact]
    public void RotatedContainer_WithoutShadow_Builds()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(200),
            Rotation = 30,
            Background = Color.FromRgb(255, 255, 255),
        });
        container.Blocks.AddParagraph().Inlines.Add("Tilted");

        var pdf = new DocumentRenderer().Render(document);
        var page = Assert.Single(pdf.Pages);
        Assert.NotNull(page.GetContent());
    }

    [Fact]
    public void UnrotatedContainer_WithShadow_EmitsShadow()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(200),
            Background = Color.FromRgb(255, 255, 255),
            Shadow = new BoxShadow
            {
                Color = Color.FromArgb(160, 0, 0, 0),
                BlurRadius = Unit.FromPoint(8),
                OffsetX = Unit.FromPoint(2),
                OffsetY = Unit.FromPoint(3),
            },
        });
        container.Blocks.AddParagraph().Inlines.Add("Panel");

        var pdf = new DocumentRenderer().Render(document);
        var page = Assert.Single(pdf.Pages);
        Assert.Contains("gs", ContentOperationTestHelpers.Operators(page.GetContent()!));
    }


    private const string PlainContainerDeclarationOrderSha256 =
        "1d16fd7e53d4f4ddaa79930eedd6448f5f518ca5e3dfe40c7c6e4317e4c5e18c";

    [Fact]
    public void PlainVerticalContainer_BuildBytes_MatchDeclarationOrderBaseline()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.AddParagraph().Inlines.Add("Before the box");
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(230, 230, 230),
        });
        container.Borders.SetAll(width: 1);
        container.Blocks.AddParagraph().Inlines.Add("Inside the box");
        container.Blocks.AddParagraph().Inlines.Add("Second line inside");
        section.Blocks.AddParagraph().Inlines.Add("After the box");

        var bytes = new DocumentRenderer().Render(document).ToArray();

        Assert.Equal(PlainContainerDeclarationOrderSha256, Sha256Hex(bytes));
    }

    private const string PlainOverlayDeclarationOrderSha256 =
        "a561b4c68628f6268d912c82f8f369999036929a50eac17b75ae1350d7abe4a1";

    [Fact]
    public void PlainOverlayContainer_BuildBytes_MatchDeclarationOrderBaseline()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.AddParagraph().Inlines.Add("Before the box");
        var container = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(230, 230, 230),
        });
        container.Borders.SetAll(width: 1);
        container.Blocks.AddParagraph().Inlines.Add("Under");
        container.Blocks.AddParagraph().Inlines.Add("Over");
        section.Blocks.AddParagraph().Inlines.Add("After the box");

        var bytes = new DocumentRenderer().Render(document).ToArray();

        Assert.Equal(PlainOverlayDeclarationOrderSha256, Sha256Hex(bytes));
    }

    private static string Sha256Hex(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    [Fact]
    public void RoundedOverlayContainer_WithBackgroundAndBorder_RendersRoundedDecoration()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(230, 230, 230),
            CornerRadius = Unit.FromPoint(6),
        });
        container.Borders.SetAll(width: 1);
        container.Blocks.AddParagraph().Inlines.Add("Rounded overlay");

        var pdf = new DocumentRenderer().Render(document);
        var page = Assert.Single(pdf.Pages);
        var content = Encoding.ASCII.GetString(page.GetContent()!);

        Assert.True(ContentOperationTestHelpers.HasSequence(page.GetContent()!, "h", "f"));
        Assert.True(ContentOperationTestHelpers.HasSequence(page.GetContent()!, "h", "S"));
        Assert.False(ContentOperationTestHelpers.HasSequence(page.GetContent()!, "re", "f"));
        var over = content.IndexOf("(Rounded overlay) Tj", StringComparison.Ordinal);
        Assert.True(over >= 0, "child text present over the rounded decoration");
    }


    private static (double X, double Y) TdBefore(string content, string text)
    {
        var match = Regex.Match(content, @"([0-9.\-]+) ([0-9.\-]+) Td\n\(" + text + @"\) Tj");
        Assert.True(match.Success, $"Td before ({text}) found");
        return (
            double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    private static FillDraw Fill(double x, double y, double width, double height) => new()
    {
        X = x,
        Y = y,
        Width = width,
        Height = height,
        Color = Color.FromRgb(230, 230, 230),
    };

    private static TextDraw Text(double x, double baseline) => new()
    {
        X = x,
        Baseline = baseline,
        Size = 12,
        Color = Color.Black,
        Font = new EmittedFont { Key = "F0", Base14 = "Helvetica" },
        Bytes = Encoding.ASCII.GetBytes("Hi"),
    };
}
