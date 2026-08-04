#nullable enable
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class OutlineStylingTests
{
    private static Document ThreePages()
    {
        var document = new Document();
        for (var i = 0; i < 3; i++)
        {
            var section = document.Sections.Add();
            section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
            section.Margins.SetAll(Unit.FromPoint(40));
            section.Blocks.Add(new Paragraph()).Inlines.Add("Page " + i);
        }

        return document;
    }

    [Fact]
    public void OutlineItem_EmitsColorAndStyleFlags()
    {
        var rendered = new DocumentRenderer().Render(ThreePages());
        rendered.Outline.Add(new OutlineItem("Styled", OutlineTarget.ToPage(0))
        {
            Color = Color.Red,
            Bold = true,
            Italic = true,
        });

        var item = Line(Emit(rendered), "/Title (Styled)");

        Carries("outline item", "/C [1 0 0]", item);
        Shaped("outline item /F", @"/F 3\b", item);
    }

    [Fact]
    public void CollapsedItem_EmitsNegativeCount_AndHidesDescendantsFromAncestorCount()
    {
        var parent = new OutlineItem("Parent", OutlineTarget.ToPage(0)) { Collapsed = true };
        parent.Children.Add(new OutlineItem("Child A", OutlineTarget.ToPage(1)));
        parent.Children.Add(new OutlineItem("Child B", OutlineTarget.ToPage(2)));
        var rendered = new DocumentRenderer().Render(ThreePages());
        rendered.Outline.Add(parent);

        var emission = Emit(rendered);

        Shaped("outline root /Count", @"/Count 1\b", Line(emission, "/Type /Outlines"));
        Shaped("collapsed parent /Count", @"/Count -2\b", Line(emission, "/Title (Parent)"));
    }

    [Fact]
    public void OpenParent_KeepsPositiveCounts()
    {
        var rendered = new DocumentRenderer().Render(ThreePages());
        var parent = new OutlineItem("Parent", OutlineTarget.ToPage(0));
        parent.Children.Add(new OutlineItem("Child A", OutlineTarget.ToPage(1)));
        parent.Children.Add(new OutlineItem("Child B", OutlineTarget.ToPage(2)));
        rendered.Outline.Add(parent);

        var emission = Emit(rendered);

        Shaped("outline root /Count", @"/Count 3\b", Line(emission, "/Type /Outlines"));
        Shaped("open parent /Count", @"/Count 2\b", Line(emission, "/Title (Parent)"));
    }

    private static string ItemTargeting(OutlineTarget target)
    {
        var rendered = new DocumentRenderer().Render(ThreePages());
        rendered.Outline.Add(new OutlineItem("D", target));
        return Line(Emit(rendered), "/Title (D)");
    }

    [Fact]
    public void FitModes_EmitCorrectDestinationArrays()
    {
        Shaped(
            "Fit outline item /Dest",
            @"/Dest \[\d+ 0 R /Fit\]",
            ItemTargeting(OutlineTarget.ToPageFit(1)));

        Shaped(
            "FitH outline item /Dest",
            @"/Dest \[\d+ 0 R /FitH 250\]",
            ItemTargeting(OutlineTarget.ToPageFitHorizontal(1, 250)));

        Shaped(
            "FitR outline item /Dest",
            @"/Dest \[\d+ 0 R /FitR 10 20 100 200\]",
            ItemTargeting(OutlineTarget.ToPageRectangle(1, 10, 20, 100, 200)));

        Shaped(
            "XYZ outline item /Dest",
            @"/Dest \[\d+ 0 R /XYZ 5 295 2\]",
            ItemTargeting(OutlineTarget.ToPageXYZ(1, 5, 295, 2.0)));
    }

    [Fact]
    public void PlainOutlineItem_EmitsNoStyleKeys()
    {
        var rendered = new DocumentRenderer().Render(ThreePages());
        rendered.Outline.Add(new OutlineItem("Plain", OutlineTarget.ToPage(0)));

        var item = Line(Emit(rendered), "/Title (Plain)");

        Lacks("outline item", "/C ", item);
        Lacks("outline item", "/F ", item);
    }
}
