#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents;
using Radzen.Documents.Layout;
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

        var capture = new LayoutCaptureContext();
        var pages = Paginator.PaginateIsolated(section, fonts, capture: capture);
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

        Assert.Contains("re f", content);
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

        Assert.Throws<NotSupportedException>(() => Paginator.PaginateIsolated(section, fonts));
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

        var pages = Paginator.PaginateIsolated(section, fonts);
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
        plan.ApplyTransform(rotate90, mark);

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
        ContentEmitter.WriteTextDraw(writer, text);
        var content = Encoding.ASCII.GetString(writer.ToArray());

        var lines = content.Split('\n');
        Assert.Equal(1, lines.Count(line => line == "q"));
        Assert.Equal(1, lines.Count(line => line == "Q"));
        Assert.Equal("q", lines[0]);
        Assert.Equal("Q", lines[^2]);

        var cm = Assert.Single(lines, line => line.EndsWith(" cm", StringComparison.Ordinal));
        var operands = cm[..^3].Split(' ').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        var (cos, sin) = (Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6));
        double[] expected = [cos, sin, -sin, cos, 100 - (100 * cos) + (200 * sin), 200 - (100 * sin) - (200 * cos)];
        Assert.Equal(6, operands.Length);
        for (var i = 0; i < 6; i++)
        {
            Assert.True(Math.Abs(expected[i] - operands[i]) < 1e-3, $"cm operand {i}: expected {expected[i]}, got {operands[i]}");
        }

        Assert.True(content.IndexOf(" cm\n", StringComparison.Ordinal) < content.IndexOf("BT\n", StringComparison.Ordinal));
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
        var content = Encoding.ASCII.GetString(page.GetContent()!);
        Assert.Contains(" gs", content);
    }


    private const string PlainContainerBaseline =
        "JVBERi0xLjcKJeLjz9MKMSAwIG9iago8PCAvVHlwZSAvQ2F0YWxvZyAvUGFnZXMgMiAwIFIgL01hcmtJbmZvIDw8IC" +
        "9NYXJrZWQgdHJ1ZSA+PiAvU3RydWN0VHJlZVJvb3QgNSAwIFIgPj4KZW5kb2JqCjIgMCBvYmoKPDwgL1R5cGUgL1Bh" +
        "Z2VzIC9LaWRzIFszIDAgUl0gL0NvdW50IDEgPj4KZW5kb2JqCjMgMCBvYmoKPDwgL1R5cGUgL1BhZ2UgL1BhcmVudC" +
        "AyIDAgUiAvTWVkaWFCb3ggWzAgMCA1OTUuMjc1NTkwNTUxMTgxMiA4NDEuODg5NzYzNzc5NTI3N10gL0NvbnRlbnRz" +
        "IDQgMCBSIC9SZXNvdXJjZXMgPDwgL0ZvbnQgPDwgL0YwIDw8IC9UeXBlIC9Gb250IC9TdWJ0eXBlIC9UeXBlMSAvQm" +
        "FzZUZvbnQgL0hlbHZldGljYSAvRW5jb2RpbmcgL1dpbkFuc2lFbmNvZGluZyA+PiA+PiA+PiAvU3RydWN0UGFyZW50" +
        "cyAwID4+CmVuZG9iago0IDAgb2JqCjw8IC9MZW5ndGggMjE2IC9GaWx0ZXIgL0ZsYXRlRGVjb2RlID4+CnN0cmVhbQ" +
        "p4nJWOwWrCQBRF9+8r7lI3kzfJJBoIQpPY4kJo63xBm4mNpAmdCvr5RWPHkFpRBt5iuOdwWMTsg3vXrmnCYhpFmMhY" +
        "sK+gwkCEKoBiWIOSvohxeK9PJLFz67Bbf1LoK6E4dj81rehlSLmRvAsbpPUoeQ81aO4g7xlJ4i2zRQ6ezZDmGaX6JL" +
        "Jr8h4ZkqFLR0f+kdYFjVJTttZg+2Hw1u7H0Buaa5ovs75V3mSV7KwP5dbYS9J/DNPOoM5di+a7Kv50XceDc8DKvLdN" +
        "gbpqDKqj6lfxAw1Re+YKZW5kc3RyZWFtCmVuZG9iago1IDAgb2JqCjw8IC9UeXBlIC9TdHJ1Y3RUcmVlUm9vdCAvSy" +
        "A2IDAgUiAvUGFyZW50VHJlZSAxMSAwIFIgL1BhcmVudFRyZWVOZXh0S2V5IDEgPj4KZW5kb2JqCjYgMCBvYmoKPDwg" +
        "L1R5cGUgL1N0cnVjdEVsZW0gL1MgL0RvY3VtZW50IC9QIDUgMCBSIC9LIFs3IDAgUl0gPj4KZW5kb2JqCjcgMCBvYm" +
        "oKPDwgL1R5cGUgL1N0cnVjdEVsZW0gL1MgL1NlY3QgL1AgNiAwIFIgL0sgWzggMCBSIDkgMCBSXSA+PgplbmRvYmoK" +
        "OCAwIG9iago8PCAvVHlwZSAvU3RydWN0RWxlbSAvUyAvUCAvUCA3IDAgUiAvUGcgMyAwIFIgL0sgWzBdID4+CmVuZG" +
        "9iago5IDAgb2JqCjw8IC9UeXBlIC9TdHJ1Y3RFbGVtIC9TIC9QIC9QIDcgMCBSIC9QZyAzIDAgUiAvSyBbMV0gPj4K" +
        "ZW5kb2JqCjEwIDAgb2JqCls4IDAgUiA5IDAgUl0KZW5kb2JqCjExIDAgb2JqCjw8IC9OdW1zIFswIDEwIDAgUl0gPj" +
        "4KZW5kb2JqCnhyZWYKMCAxMgowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMTUgMDAwMDAgbiAKMDAwMDAwMDEx" +
        "NSAwMDAwMCBuIAowMDAwMDAwMTcyIDAwMDAwIG4gCjAwMDAwMDA0MTkgMDAwMDAgbiAKMDAwMDAwMDcwNyAwMDAwMC" +
        "BuIAowMDAwMDAwNzk5IDAwMDAwIG4gCjAwMDAwMDA4NzEgMDAwMDAgbiAKMDAwMDAwMDk0NSAwMDAwMCBuIAowMDAw" +
        "MDAxMDE2IDAwMDAwIG4gCjAwMDAwMDEwODcgMDAwMDAgbiAKMDAwMDAwMTExNyAwMDAwMCBuIAp0cmFpbGVyCjw8IC" +
        "9Sb290IDEgMCBSIC9TaXplIDEyID4+CnN0YXJ0eHJlZgoxMTU2CiUlRU9GCg==";

    [Fact]
    public void PlainVerticalContainer_BuildBytes_IdenticalToPreChangeBaseline()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.AddParagraph().Inlines.Add("Before the box");
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(230, 230, 230),
        });
        container.Borders.Width = 1;
        container.Blocks.AddParagraph().Inlines.Add("Inside the box");
        container.Blocks.AddParagraph().Inlines.Add("Second line inside");
        section.Blocks.AddParagraph().Inlines.Add("After the box");

        var bytes = new DocumentRenderer().Render(document).ToArray();

        Assert.Equal(Convert.FromBase64String(PlainContainerBaseline), bytes);
    }

    private const string PlainOverlayBaseline =
        "JVBERi0xLjcKJeLjz9MKMSAwIG9iago8PCAvVHlwZSAvQ2F0YWxvZyAvUGFnZXMgMiAwIFIgL01hcmtJbmZvIDw8IC" +
        "9NYXJrZWQgdHJ1ZSA+PiAvU3RydWN0VHJlZVJvb3QgNSAwIFIgPj4KZW5kb2JqCjIgMCBvYmoKPDwgL1R5cGUgL1Bh" +
        "Z2VzIC9LaWRzIFszIDAgUl0gL0NvdW50IDEgPj4KZW5kb2JqCjMgMCBvYmoKPDwgL1R5cGUgL1BhZ2UgL1BhcmVudC" +
        "AyIDAgUiAvTWVkaWFCb3ggWzAgMCA1OTUuMjc1NTkwNTUxMTgxMiA4NDEuODg5NzYzNzc5NTI3N10gL0NvbnRlbnRz" +
        "IDQgMCBSIC9SZXNvdXJjZXMgPDwgL0ZvbnQgPDwgL0YwIDw8IC9UeXBlIC9Gb250IC9TdWJ0eXBlIC9UeXBlMSAvQm" +
        "FzZUZvbnQgL0hlbHZldGljYSAvRW5jb2RpbmcgL1dpbkFuc2lFbmNvZGluZyA+PiA+PiA+PiAvU3RydWN0UGFyZW50" +
        "cyAwID4+CmVuZG9iago0IDAgb2JqCjw8IC9MZW5ndGggMjAyIC9GaWx0ZXIgL0ZsYXRlRGVjb2RlID4+CnN0cmVhbQ" +
        "p4nI2O3QqCQBBG7+cp5rJu1nHdNQUJ0iy6kP62F4jWIippierxo7RVpEAG5mL4zpmPWEgcqbHNHgbEAt/Hgecy4gKF" +
        "9JgUHvIAjcYcrkD4ntUUXHzYtAw/6TNILpig0F5OsIZlm7Kh6klHrFWtQVWXblSrcwk5C4wiJ0tmY6ThEONxArGqRG" +
        "YPzoTQJVS5pX3+odUOerHOC6PxdtC4LZ59VEdIFaRZ0rS6nay8to7ymza/pH8MQWkQtWFz2WnzJTtT83sNvQDP4XOI" +
        "CmVuZHN0cmVhbQplbmRvYmoKNSAwIG9iago8PCAvVHlwZSAvU3RydWN0VHJlZVJvb3QgL0sgNiAwIFIgL1BhcmVudF" +
        "RyZWUgMTEgMCBSIC9QYXJlbnRUcmVlTmV4dEtleSAxID4+CmVuZG9iago2IDAgb2JqCjw8IC9UeXBlIC9TdHJ1Y3RF" +
        "bGVtIC9TIC9Eb2N1bWVudCAvUCA1IDAgUiAvSyBbNyAwIFJdID4+CmVuZG9iago3IDAgb2JqCjw8IC9UeXBlIC9TdH" +
        "J1Y3RFbGVtIC9TIC9TZWN0IC9QIDYgMCBSIC9LIFs4IDAgUiA5IDAgUl0gPj4KZW5kb2JqCjggMCBvYmoKPDwgL1R5" +
        "cGUgL1N0cnVjdEVsZW0gL1MgL1AgL1AgNyAwIFIgL1BnIDMgMCBSIC9LIFswXSA+PgplbmRvYmoKOSAwIG9iago8PC" +
        "AvVHlwZSAvU3RydWN0RWxlbSAvUyAvUCAvUCA3IDAgUiAvUGcgMyAwIFIgL0sgWzFdID4+CmVuZG9iagoxMCAwIG9i" +
        "agpbOCAwIFIgOSAwIFJdCmVuZG9iagoxMSAwIG9iago8PCAvTnVtcyBbMCAxMCAwIFJdID4+CmVuZG9iagp4cmVmCj" +
        "AgMTIKMDAwMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAwMDE1IDAwMDAwIG4gCjAwMDAwMDAxMTUgMDAwMDAgbiAKMDAw" +
        "MDAwMDE3MiAwMDAwMCBuIAowMDAwMDAwNDE5IDAwMDAwIG4gCjAwMDAwMDA2OTMgMDAwMDAgbiAKMDAwMDAwMDc4NS" +
        "AwMDAwMCBuIAowMDAwMDAwODU3IDAwMDAwIG4gCjAwMDAwMDA5MzEgMDAwMDAgbiAKMDAwMDAwMTAwMiAwMDAwMCBu" +
        "IAowMDAwMDAxMDczIDAwMDAwIG4gCjAwMDAwMDExMDMgMDAwMDAgbiAKdHJhaWxlcgo8PCAvUm9vdCAxIDAgUiAvU2" +
        "l6ZSAxMiA+PgpzdGFydHhyZWYKMTE0MgolJUVPRgo=";

    [Fact]
    public void PlainOverlayContainer_BuildBytes_IdenticalToPreChangeBaseline()
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
        container.Borders.Width = 1;
        container.Blocks.AddParagraph().Inlines.Add("Under");
        container.Blocks.AddParagraph().Inlines.Add("Over");
        section.Blocks.AddParagraph().Inlines.Add("After the box");

        var bytes = new DocumentRenderer().Render(document).ToArray();

        Assert.Equal(Convert.FromBase64String(PlainOverlayBaseline), bytes);
    }

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
        container.Borders.Width = 1;
        container.Blocks.AddParagraph().Inlines.Add("Rounded overlay");

        var pdf = new DocumentRenderer().Render(document);
        var page = Assert.Single(pdf.Pages);
        var content = Encoding.ASCII.GetString(page.GetContent()!);

        Assert.Contains("h\nf\n", content);
        Assert.Contains("h\nS\n", content);
        Assert.DoesNotContain("re f", content);
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
        Font = new GeneratedFont { Key = "F0", Base14 = "Helvetica" },
        Bytes = Encoding.ASCII.GetBytes("Hi"),
    };
}
