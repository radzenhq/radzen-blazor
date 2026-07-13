#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Content;
namespace Radzen.Blazor.Pdf.Tests;

// Container overlay (z-stack) layout and content rotation.
//
// Overlay: a first-class box (like a Stack container) whose decoration paints through
// BoxRenderer; its children share the box origin, each laid out from the box top-left
// (inset by the padding) and merged in declaration order (later children on top within
// each draw kind). The box height is the tallest child's.
//
// Rotation: a rotated container places as a first-class box carrying a page-space
// rotation matrix (about the box center); BoxEmitter bakes it into the box's draws via
// PagePlan.ApplyTransform, which rotates edge endpoints, converts fills to equivalent
// solid strokes, and carries the matrix on text/image draws which ContentEmitter wraps
// in a balanced q cm .. Q. Rotated content is NOT clipped to the original box.
public class ContainerOverlayRotationTests
{
    // ---------------------------------------------------------------- overlay

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

        var pages = Paginator.Paginate(section, fonts);
        var page = Assert.Single(pages);

        // The overlay is a single first-class box - no lowered tables.
        Assert.Empty(page.Tables);
        var box = Assert.Single(page.Boxes);
        Assert.Same(container, box.Source);
        Assert.Null(box.Transform);
        Assert.Equal(0, box.Y, 6);
        Assert.Equal(0, box.Bounds.X, 6);
        Assert.Equal(400, box.Bounds.Width, 6);

        // The decoration carries the container background/borders through BoxStyle.
        Assert.Equal(container.Background, box.Style.Background);

        // Both children's lines are inset by the padding (X = 10) and share the box top
        // (first line of each child at Y = padding), in declaration order.
        var firstShort = box.Content.Lines.First(l => ReferenceEquals(l.Source, shortText));
        var firstTall = box.Content.Lines.First(l => ReferenceEquals(l.Source, tallText));
        Assert.Equal(10, firstShort.X, 6);
        Assert.Equal(10, firstTall.X, 6);
        Assert.Equal(10, firstShort.Y, 6);
        Assert.Equal(firstShort.Y, firstTall.Y, 6);

        // The short child precedes the tall child in the merged line list (later on top).
        Assert.True(
            box.Content.Lines.ToList().FindIndex(l => ReferenceEquals(l.Source, shortText))
            < box.Content.Lines.ToList().FindIndex(l => ReferenceEquals(l.Source, tallText)));

        // The box spans the tallest child: its inner height is the multi-line child's.
        var tallHeight = box.Content.Lines.Where(l => ReferenceEquals(l.Source, tallText)).Sum(l => l.Line.Height);
        var shortHeight = box.Content.Lines.Where(l => ReferenceEquals(l.Source, shortText)).Sum(l => l.Line.Height);
        Assert.True(tallHeight > shortHeight);
        Assert.Equal(tallHeight + 20, box.Bounds.Height, 6);
    }

    [Fact]
    public void Overlay_BuildsContentStream_BothChildrenAtSameOrigin_InDeclarationOrder()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Padding = Unit.FromPoint(6),
            Background = Color.FromRgb(200, 200, 200),
        });
        container.Blocks.AddParagraph().Inlines.Add("UNDERLAY");
        container.Blocks.AddParagraph().Inlines.Add("OVERLAY");

        var document = builder.Build();
        var page = Assert.Single(document.Pages);
        var content = Encoding.ASCII.GetString(page.GetContent()!);

        // The background fill is present and both children paint after it, in order.
        Assert.Contains("re f", content);
        var under = content.IndexOf("(UNDERLAY) Tj", StringComparison.Ordinal);
        var over = content.IndexOf("(OVERLAY) Tj", StringComparison.Ordinal);
        Assert.True(under >= 0, "first child present");
        Assert.True(over >= 0, "second child present");
        Assert.True(under < over, "children paint in declaration order (later on top)");

        // Both children start at the same box origin: identical Td coordinates.
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

        Assert.Throws<NotSupportedException>(() => Paginator.Paginate(section, fonts));
    }

    // --------------------------------------------------------------- rotation

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

        var pages = Paginator.Paginate(section, fonts);
        var page = Assert.Single(pages);

        // A rotated Stack container is a first-class box, not a lowered table.
        Assert.Empty(page.Tables);
        var box = Assert.Single(page.Boxes);
        Assert.True(box.Transform.HasValue, "box carries the rotation transform");
        var transform = box.Transform!.Value;

        var (cos, sin) = (Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6));
        Assert.Equal(cos, transform.A, 9);
        Assert.Equal(sin, transform.B, 9);
        Assert.Equal(-sin, transform.C, 9);
        Assert.Equal(cos, transform.D, 9);

        // Pivot = box center (page space, y up): the center maps to itself and a point
        // one unit to its right rotates by 30 degrees around it.
        var centerX = 100.0;
        var centerY = 600 - box.Bounds.Height / 2;
        var (px, py) = transform.Transform(centerX, centerY);
        Assert.Equal(centerX, px, 9);
        Assert.Equal(centerY, py, 9);
        var (qx, qy) = transform.Transform(centerX + 1, centerY);
        Assert.Equal(centerX + cos, qx, 9);
        Assert.Equal(centerY + sin, qy, 9);
    }

    [Fact]
    public void ApplyTransform_RotatesEdges_ConvertsFillsToSolidStrokes_TagsTexts()
    {
        var plan = new PagePlan { Size = PageSizes.A4 };
        plan.Fills.Add(Fill(0, 0, 10, 10)); // pre-mark draw stays untouched

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

        // The pre-mark fill is untouched; the post-mark fill became a solid stroke along
        // its rotated centerline (width = rect height), inserted before the border edge.
        var fill = Assert.Single(plan.Fills);
        Assert.Equal(10, fill.Width, 6);

        Assert.Equal(2, plan.Edges.Count);
        var converted = plan.Edges[0];
        Assert.Equal(BorderStyle.Solid, converted.Style);
        Assert.Equal(40, converted.LineWidth, 6);
        Assert.Equal(-40, converted.X1, 6); // (10, 40) rotated 90deg -> (-40, 10)
        Assert.Equal(10, converted.Y1, 6);
        Assert.Equal(-40, converted.X2, 6); // (110, 40) rotated 90deg -> (-40, 110)
        Assert.Equal(110, converted.Y2, 6);

        var border = plan.Edges[1];
        Assert.Equal(-20, border.X1, 6); // (10, 20) -> (-20, 10)
        Assert.Equal(10, border.Y1, 6);
        Assert.Equal(-20, border.X2, 6); // (110, 20) -> (-20, 110)
        Assert.Equal(110, border.Y2, 6);
        Assert.Equal(2, border.LineWidth, 6);

        var text = Assert.Single(plan.Texts);
        Assert.Equal(rotate90, text.Transform);
    }

    [Fact]
    public void WriteTextDraw_WithTransform_EmitsBalancedQ_Cm_Q_WithRotationOperands()
    {
        // Rotation of 30 degrees about (100, 200):
        // e = cx - cx*cos + cy*sin, f = cy - cx*sin - cy*cos.
        var transform = Matrix.Translate(-100, -200) * Matrix.Rotate(30) * Matrix.Translate(100, 200);
        var text = Text(72, 700) with { Transform = transform };

        using var writer = new ContentWriter();
        ContentEmitter.WriteTextDraw(writer, text);
        var content = Encoding.ASCII.GetString(writer.ToArray());

        var lines = content.Split('\n');
        Assert.Equal(1, lines.Count(line => line == "q"));
        Assert.Equal(1, lines.Count(line => line == "Q"));
        Assert.Equal("q", lines[0]);
        Assert.Equal("Q", lines[^2]); // last line is empty after the trailing \n

        var cm = Assert.Single(lines, line => line.EndsWith(" cm", StringComparison.Ordinal));
        var operands = cm[..^3].Split(' ').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        // The writer quantizes operands to 3 decimals, so compare with that tolerance.
        var (cos, sin) = (Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6));
        double[] expected = [cos, sin, -sin, cos, 100 - (100 * cos) + (200 * sin), 200 - (100 * sin) - (200 * cos)];
        Assert.Equal(6, operands.Length);
        for (var i = 0; i < 6; i++)
        {
            Assert.True(Math.Abs(expected[i] - operands[i]) < 1e-3, $"cm operand {i}: expected {expected[i]}, got {operands[i]}");
        }

        // The cm precedes the text object so the show is transformed.
        Assert.True(content.IndexOf(" cm\n", StringComparison.Ordinal) < content.IndexOf("BT\n", StringComparison.Ordinal));
    }

    [Fact]
    public void RotatedContainer_WithShadow_Throws()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
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

        Assert.Throws<NotSupportedException>(() => builder.Build());
    }

    [Fact]
    public void RotatedContainer_WithoutShadow_Builds()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(200),
            Rotation = 30,
            Background = Color.FromRgb(255, 255, 255),
        });
        container.Blocks.AddParagraph().Inlines.Add("Tilted");

        var document = builder.Build();
        var page = Assert.Single(document.Pages);
        Assert.NotNull(page.GetContent());
    }

    [Fact]
    public void UnrotatedContainer_WithShadow_EmitsShadow()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
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

        var document = builder.Build();
        var page = Assert.Single(document.Pages);
        var content = Encoding.ASCII.GetString(page.GetContent()!);
        // The blurred drop shadow installs a luminosity soft mask through an ExtGState (gs).
        Assert.Contains(" gs", content);
    }

    // ------------------------------------------------------------- regression

    // Built with the pre-change generator (commit ce34920e): a paragraph, a plain
    // vertical container (padding 8, gray background, 1pt borders, two paragraphs)
    // and a closing paragraph, all default base-14 text on a default A4 section.
    // The generator emits no /ID and no dates, so the comparison is whole-file.
    private const string PlainContainerBaseline =
        "JVBERi0xLjcKJeLjz9MKMSAwIG9iago8PCAvVHlwZSAvQ2F0YWxvZyAvUGFnZXMgMiAwIFIgL01hcmtJbmZvIDw8IC9NYXJrZWQg" +
        "dHJ1ZSA+PiAvU3RydWN0VHJlZVJvb3QgNSAwIFIgPj4KZW5kb2JqCjIgMCBvYmoKPDwgL1R5cGUgL1BhZ2VzIC9LaWRzIFszIDAg" +
        "Ul0gL0NvdW50IDEgPj4KZW5kb2JqCjMgMCBvYmoKPDwgL1R5cGUgL1BhZ2UgL1BhcmVudCAyIDAgUiAvTWVkaWFCb3ggWzAgMCA1" +
        "OTUuMjc1NTkwNTUxMTgxMiA4NDEuODg5NzYzNzc5NTI3N10gL0NvbnRlbnRzIDQgMCBSIC9SZXNvdXJjZXMgPDwgL0ZvbnQgPDwg" +
        "L0YwIDw8IC9UeXBlIC9Gb250IC9TdWJ0eXBlIC9UeXBlMSAvQmFzZUZvbnQgL0hlbHZldGljYSAvRW5jb2RpbmcgL1dpbkFuc2lF" +
        "bmNvZGluZyA+PiA+PiA+PiAvU3RydWN0UGFyZW50cyAwID4+CmVuZG9iago0IDAgb2JqCjw8IC9MZW5ndGggMjE2IC9GaWx0ZXIg" +
        "L0ZsYXRlRGVjb2RlID4+CnN0cmVhbQp4nJWOwW6CQBRF9+8r7rLdDG9gAEmMSUFrXJC0db7AMlAaCnE0qZ9vBIuEtlEzyVtM7jk5" +
        "LCJ2wYNrCwpZTIIAoYwEuwrK94SvPCiGNchpS4zTe1uSxHe/9rv1F/muEoqj/qeiNb2OqX4k78JGaQNK3kONmjso1mfKFuQ8MyRD" +
        "5xROuqly26nO6GFV78rMYP9hsGkOj9CftNBXcI97fG3emzpDVdYGZav6UTgvmE6dNFnNwbMZ4nnyn/XcH1yiYpM39lfUIk2GVnmT" +
        "VV5an/K9sX9JjyT5e+YKZW5kc3RyZWFtCmVuZG9iago1IDAgb2JqCjw8IC9UeXBlIC9TdHJ1Y3RUcmVlUm9vdCAvSyA2IDAgUiAv" +
        "UGFyZW50VHJlZSAxMSAwIFIgL1BhcmVudFRyZWVOZXh0S2V5IDEgPj4KZW5kb2JqCjYgMCBvYmoKPDwgL1R5cGUgL1N0cnVjdEVs" +
        "ZW0gL1MgL0RvY3VtZW50IC9QIDUgMCBSIC9LIFs3IDAgUl0gPj4KZW5kb2JqCjcgMCBvYmoKPDwgL1R5cGUgL1N0cnVjdEVsZW0g" +
        "L1MgL1NlY3QgL1AgNiAwIFIgL0sgWzggMCBSIDkgMCBSXSA+PgplbmRvYmoKOCAwIG9iago8PCAvVHlwZSAvU3RydWN0RWxlbSAv" +
        "UyAvUCAvUCA3IDAgUiAvUGcgMyAwIFIgL0sgWzBdID4+CmVuZG9iago5IDAgb2JqCjw8IC9UeXBlIC9TdHJ1Y3RFbGVtIC9TIC9Q" +
        "IC9QIDcgMCBSIC9QZyAzIDAgUiAvSyBbMV0gPj4KZW5kb2JqCjEwIDAgb2JqCls4IDAgUiA5IDAgUl0KZW5kb2JqCjExIDAgb2Jq" +
        "Cjw8IC9OdW1zIFswIDEwIDAgUl0gPj4KZW5kb2JqCnhyZWYKMCAxMgowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMTUgMDAw" +
        "MDAgbiAKMDAwMDAwMDExNSAwMDAwMCBuIAowMDAwMDAwMTcyIDAwMDAwIG4gCjAwMDAwMDA0MTkgMDAwMDAgbiAKMDAwMDAwMDcw" +
        "NyAwMDAwMCBuIAowMDAwMDAwNzk5IDAwMDAwIG4gCjAwMDAwMDA4NzEgMDAwMDAgbiAKMDAwMDAwMDk0NSAwMDAwMCBuIAowMDAw" +
        "MDAxMDE2IDAwMDAwIG4gCjAwMDAwMDEwODcgMDAwMDAgbiAKMDAwMDAwMTExNyAwMDAwMCBuIAp0cmFpbGVyCjw8IC9Sb290IDEg" +
        "MCBSIC9TaXplIDEyID4+CnN0YXJ0eHJlZgoxMTU2CiUlRU9GCg==";

    [Fact]
    public void PlainVerticalContainer_BuildBytes_IdenticalToPreChangeBaseline()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
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

        var bytes = builder.Build().ToArray();

        Assert.Equal(Convert.FromBase64String(PlainContainerBaseline), bytes);
    }

    // Built with the pre-change generator (commit 7174dca7, overlay still lowered to a
    // synthetic single-cell decoration table + one child table each): a paragraph, an
    // OVERLAY container (padding 8, gray background, 1pt borders, two paragraphs sharing
    // the box origin) and a closing paragraph on a default A4 section. The first-class
    // overlay box must reproduce these bytes exactly for the decoration + children.
    private const string PlainOverlayBaseline =
        "JVBERi0xLjcKJeLjz9MKMSAwIG9iago8PCAvVHlwZSAvQ2F0YWxvZyAvUGFnZXMgMiAwIFIgL01hcmtJbmZvIDw8IC9NYXJrZWQg" +
        "dHJ1ZSA+PiAvU3RydWN0VHJlZVJvb3QgNSAwIFIgPj4KZW5kb2JqCjIgMCBvYmoKPDwgL1R5cGUgL1BhZ2VzIC9LaWRzIFszIDAg" +
        "Ul0gL0NvdW50IDEgPj4KZW5kb2JqCjMgMCBvYmoKPDwgL1R5cGUgL1BhZ2UgL1BhcmVudCAyIDAgUiAvTWVkaWFCb3ggWzAgMCA1" +
        "OTUuMjc1NTkwNTUxMTgxMiA4NDEuODg5NzYzNzc5NTI3N10gL0NvbnRlbnRzIDQgMCBSIC9SZXNvdXJjZXMgPDwgL0ZvbnQgPDwg" +
        "L0YwIDw8IC9UeXBlIC9Gb250IC9TdWJ0eXBlIC9UeXBlMSAvQmFzZUZvbnQgL0hlbHZldGljYSAvRW5jb2RpbmcgL1dpbkFuc2lF" +
        "bmNvZGluZyA+PiA+PiA+PiAvU3RydWN0UGFyZW50cyAwID4+CmVuZG9iago0IDAgb2JqCjw8IC9MZW5ndGggMjAzIC9GaWx0ZXIg" +
        "L0ZsYXRlRGVjb2RlID4+CnN0cmVhbQp4nI2OywrCMBBF9/MVs9RNOk2TPkAKtlZxIb7iD4hpRXxgKOrnixrbUhTKwCyGe85cYhFx" +
        "pMY2BQTEQt/HwHMZcYFCekwKD3mIRmMOVyB8zWoCLt6rtIze6RNILpigqLocYQ3LNlWF7JOOWKtag7KXblSr8wdKlKVMAc6Y0CVU" +
        "OQThJyr4O6p20Nucd9r0UR0gU92p+a2GnAUOBs4snY6Q4hiTUfrPY4v6tSfR+cVoLPcat5fH15jN0qbV7WTltXWYl9r8kj4B7+xz" +
        "iAplbmRzdHJlYW0KZW5kb2JqCjUgMCBvYmoKPDwgL1R5cGUgL1N0cnVjdFRyZWVSb290IC9LIDYgMCBSIC9QYXJlbnRUcmVlIDEx" +
        "IDAgUiAvUGFyZW50VHJlZU5leHRLZXkgMSA+PgplbmRvYmoKNiAwIG9iago8PCAvVHlwZSAvU3RydWN0RWxlbSAvUyAvRG9jdW1l" +
        "bnQgL1AgNSAwIFIgL0sgWzcgMCBSXSA+PgplbmRvYmoKNyAwIG9iago8PCAvVHlwZSAvU3RydWN0RWxlbSAvUyAvU2VjdCAvUCA2" +
        "IDAgUiAvSyBbOCAwIFIgOSAwIFJdID4+CmVuZG9iago4IDAgb2JqCjw8IC9UeXBlIC9TdHJ1Y3RFbGVtIC9TIC9QIC9QIDcgMCBS" +
        "IC9QZyAzIDAgUiAvSyBbMF0gPj4KZW5kb2JqCjkgMCBvYmoKPDwgL1R5cGUgL1N0cnVjdEVsZW0gL1MgL1AgL1AgNyAwIFIgL1Bn" +
        "IDMgMCBSIC9LIFsxXSA+PgplbmRvYmoKMTAgMCBvYmoKWzggMCBSIDkgMCBSXQplbmRvYmoKMTEgMCBvYmoKPDwgL051bXMgWzAg" +
        "MTAgMCBSXSA+PgplbmRvYmoKeHJlZgowIDEyCjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDAxNSAwMDAwMCBuIAowMDAwMDAw" +
        "MTE1IDAwMDAwIG4gCjAwMDAwMDAxNzIgMDAwMDAgbiAKMDAwMDAwMDQxOSAwMDAwMCBuIAowMDAwMDAwNjk0IDAwMDAwIG4gCjAw" +
        "MDAwMDA3ODYgMDAwMDAgbiAKMDAwMDAwMDg1OCAwMDAwMCBuIAowMDAwMDAwOTMyIDAwMDAwIG4gCjAwMDAwMDEwMDMgMDAwMDAg" +
        "biAKMDAwMDAwMTA3NCAwMDAwMCBuIAowMDAwMDAxMTA0IDAwMDAwIG4gCnRyYWlsZXIKPDwgL1Jvb3QgMSAwIFIgL1NpemUgMTIg" +
        "Pj4Kc3RhcnR4cmVmCjExNDMKJSVFT0YK";

    [Fact]
    public void PlainOverlayContainer_BuildBytes_IdenticalToPreChangeBaseline()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
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

        var bytes = builder.Build().ToArray();

        Assert.Equal(Convert.FromBase64String(PlainOverlayBaseline), bytes);
    }

    [Fact]
    public void RoundedOverlayContainer_WithBackgroundAndBorder_RendersRoundedDecoration()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(230, 230, 230),
            CornerRadius = Unit.FromPoint(6),
        });
        container.Borders.Width = 1;
        container.Blocks.AddParagraph().Inlines.Add("Rounded overlay");

        var document = builder.Build();
        var page = Assert.Single(document.Pages);
        var content = Encoding.ASCII.GetString(page.GetContent()!);

        // The rounded background fills a Bezier path (h f) and the uniform border strokes
        // the same rounded path (h S) - both through BoxRenderer, no square "re f" fill.
        Assert.Contains("h\nf\n", content);
        Assert.Contains("h\nS\n", content);
        Assert.DoesNotContain("re f", content);
        var over = content.IndexOf("(Rounded overlay) Tj", StringComparison.Ordinal);
        Assert.True(over >= 0, "child text present over the rounded decoration");
    }

    // ----------------------------------------------------------------- helpers

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
