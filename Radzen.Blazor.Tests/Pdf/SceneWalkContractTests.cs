#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Scene;
using Radzen.Documents;
using Radzen.Documents.Core;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

internal sealed class SceneTrace : ISceneVisitor
{
    private readonly StringBuilder text = new();
    private int depth;

    public List<SceneClip> BoxClips { get; } = [];

    public List<LaidOutBox> Boxes { get; } = [];

    public List<LaidOutRow> NestedRows { get; } = [];

    public List<string?> ImageMediaTypes { get; } = [];

    private readonly List<bool> tables = [];

    public static SceneTrace Of(LaidOutDocument document)
    {
        var trace = new SceneTrace();
        SceneWalk.Document(document, trace);
        return trace;
    }

    public override string ToString() => text.ToString().TrimEnd();

    private void Write(string line)
    {
        text.Append(' ', depth * 2);
        text.AppendLine(line);
    }

    private void Enter(string line)
    {
        Write(line);
        depth++;
    }

    private void Leave(string line)
    {
        depth--;
        Write(line);
    }

    void ISceneVisitor.BeginDocument(LaidOutDocument document)
        => Enter($"document pages={document.Pages.Length}");

    void ISceneVisitor.EndDocument(LaidOutDocument document) => Leave("/document");

    void ISceneVisitor.BeginPage(LaidOutPage page, int index)
        => Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"page {index} size={N(page.Size.Width.Point)}x{N(page.Size.Height.Point)} content={N(page.ContentBox.X)},{N(page.ContentBox.Y)}"));

    void ISceneVisitor.EndPage(LaidOutPage page, int index) => Leave("/page");

    void ISceneVisitor.EnterLayer(SceneLayerKind kind, double top)
        => Enter(string.Create(CultureInfo.InvariantCulture, $"layer {kind} top={N(top)}"));

    void ISceneVisitor.LeaveLayer(SceneLayerKind kind) => Leave($"/layer {kind}");

    void ISceneVisitor.Line(in LaidOutLine line, in SceneFrame frame) => Write($"line \"{Text(line)}\"");

    void ISceneVisitor.Image(in LaidOutImage image, in SceneFrame frame)
    {
        ImageMediaTypes.Add(image.Paint.MediaType);
        Write($"image {image.Paint.MediaType}");
    }

    void ISceneVisitor.CodeSymbol(in LaidOutCodeSymbol codeSymbol, in SceneFrame frame) => Write("code");

    void ISceneVisitor.EnterBox(LaidOutBox box, in SceneFrame frame, in SceneClip clip)
    {
        Boxes.Add(box);
        BoxClips.Add(clip);
        Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"box clip={N(clip.Bounds.X)},{N(clip.Bounds.Y)},{N(clip.Bounds.Width)},{N(clip.Bounds.Height)} lines={clip.ClipsLines} inline={clip.ClipsInline}"));
    }

    void ISceneVisitor.LeaveBox(LaidOutBox box, in SceneFrame frame) => Leave("/box");

    void ISceneVisitor.EnterFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
    {
        tables.Add(false);
        Enter("fragment");
    }

    void ISceneVisitor.LeaveFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
    {
        tables.RemoveAt(tables.Count - 1);
        Leave("/fragment");
    }

    void ISceneVisitor.EnterTable(in LaidOutTablePlacement table, in SceneFrame frame)
    {
        tables.Add(true);
        Enter("table");
    }

    void ISceneVisitor.LeaveTable(in LaidOutTablePlacement table, in SceneFrame frame)
    {
        tables.RemoveAt(tables.Count - 1);
        Leave("/table");
    }

    void ISceneVisitor.EnterRow(in LaidOutRow row, in SceneFrame frame)
    {
        if (tables[^1])
        {
            NestedRows.Add(row);
        }

        Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"row {row.SourceRow} y={N(row.Y)} h={N(row.Height)} cells={row.Cells.Length}"));
    }

    void ISceneVisitor.LeaveRow(in LaidOutRow row, in SceneFrame frame) => Leave("/row");

    void ISceneVisitor.EnterCell(LaidOutCell cell, in SceneFrame frame, in SceneClip clip)
        => Enter($"cell {cell.Row},{cell.Column}");

    void ISceneVisitor.LeaveCell(LaidOutCell cell, in SceneFrame frame) => Leave("/cell");

    void ISceneVisitor.Link(in LaidOutLink link) => Write($"link {link.Uri ?? link.Anchor}");

    void ISceneVisitor.Anchor(in LaidOutAnchor anchor) => Write($"anchor {anchor.Name}");

    void ISceneVisitor.Watermark(LaidOutWatermark watermark) => Write($"watermark {watermark.Text?.Text}");

    private static string Text(in LaidOutLine line)
    {
        var value = new StringBuilder();
        foreach (var fragment in line.Line.Fragments)
        {
            value.Append(fragment.Text);
        }

        return value.ToString();
    }

    private static string N(double value)
        => value.ToString("0.##", CultureInfo.InvariantCulture);
}

public class SceneWalkContractTests
{
    private static Paragraph Text(string value, double size = 10)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(value);
        run.Font.Family = BuildTestSupport.Latin;
        run.Font.Size = size;
        return paragraph;
    }

    private static Document Scripted()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(220));
        section.Margins.SetAll(Unit.FromPoint(20));
        section.Watermark = new Watermark { Text = "DRAFT", Font = { Family = BuildTestSupport.Latin } };
        section.Header.Blocks.Add(Text("HEAD"));
        section.Footer.Blocks.Add(Text("FOOT"));

        var anchored = section.Blocks.Add(Text("Top"));
        anchored.Inlines[0].Anchor = "top";

        var linked = section.Blocks.AddParagraph();
        var link = linked.Inlines.Add("Radzen");
        link.Font.Family = BuildTestSupport.Latin;
        link.Font.Size = 10;
        link.Link = "https://www.radzen.com";

        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(4) });
        container.Blocks.Add(Text("Inside"));

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(80));
        table.Columns.Add(Unit.FromPoint(80));
        for (var row = 0; row < 2; row++)
        {
            var added = table.Rows.Add();
            added.Cells[0].Blocks.Add(Text($"r{row}c0"));
            added.Cells[1].Blocks.Add(Text($"r{row}c1"));
        }

        return document;
    }

    [Fact]
    public void ScriptedDocument_TraversalOrder_IsStable()
    {
        var trace = SceneTrace.Of(DocumentLayouter.Layout(Scripted())).ToString();

        Assert.Equal(
            """
            document pages=1
              page 0 size=300x220 content=20,46.93
                layer Body top=46.93
                  line "Top"
                  line "Radzen"
                  box clip=20,23,260,19.5 lines=False inline=False
                    line "Inside"
                  /box
                  fragment
                    row 0 y=42.5 h=11.5 cells=2
                      cell 0,0
                        line "r0c0"
                      /cell
                      cell 0,1
                        line "r0c1"
                      /cell
                    /row
                    row 1 y=54 h=11.5 cells=2
                      cell 1,0
                        line "r1c0"
                      /cell
                      cell 1,1
                        line "r1c1"
                      /cell
                    /row
                  /fragment
                /layer Body
                layer Header top=35.43
                  line "HEAD"
                /layer Header
                layer Footer top=173.07
                  line "FOOT"
                /layer Footer
                link https://www.radzen.com
                anchor top
                watermark DRAFT
              /page
            /document
            """,
            trace,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void NestedTable_ReportsRowsWithBackgroundsAndCells()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));

        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(5) });
        var table = container.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(60));
        table.Columns.Add(Unit.FromPoint(60));
        for (var row = 0; row < 3; row++)
        {
            var added = table.Rows.Add();
            added.Background = Color.FromRgb((byte)(row * 10), 0, 0);
            added.Cells[0].Blocks.Add(Text($"r{row}c0"));
            added.Cells[1].Blocks.Add(Text($"r{row}c1"));
        }

        var trace = SceneTrace.Of(DocumentLayouter.Layout(document));

        Assert.Equal(3, trace.NestedRows.Count);
        for (var row = 0; row < 3; row++)
        {
            Assert.Equal(row, trace.NestedRows[row].SourceRow);
            Assert.Equal(2, trace.NestedRows[row].Cells.Length);
            Assert.All(
                trace.NestedRows[row].Cells,
                placed => Assert.Equal(Color.FromRgb((byte)(placed.Cell.Row * 10), 0, 0), placed.Cell.Decoration.Background));
            Assert.True(trace.NestedRows[row].Height > 0);
        }

        Assert.Equal(0, trace.NestedRows[0].Y);
        Assert.Equal(trace.NestedRows[0].Height, trace.NestedRows[1].Y, 6);
        Assert.Equal(trace.NestedRows[1].Y + trace.NestedRows[1].Height, trace.NestedRows[2].Y, 6);
    }

    [Fact]
    public void OverflowingBox_AnnouncesClipMatchingTheBoxBounds()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));

        var narrow = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(6),
            Padding = Unit.FromPoint(0),
        });

        narrow.Blocks.Add(Text("Wide", 24));

        var laidOut = DocumentLayouter.Layout(document);
        var trace = SceneTrace.Of(laidOut);

        var box = Assert.Single(trace.Boxes);
        var clip = Assert.Single(trace.BoxClips);

        Assert.True(clip.ClipsLines);
        Assert.False(clip.ClipsInline);
        Assert.Equal(laidOut.Pages[0].ContentBox.X + box.Bounds.X, clip.Bounds.X, 6);
        Assert.Equal(box.Bounds.Y, clip.Bounds.Y, 6);
        Assert.Equal(box.Bounds.Width, clip.Bounds.Width, 6);
        Assert.Equal(box.Bounds.Height, clip.Bounds.Height, 6);
    }

    [Fact]
    public void NonOverflowingBox_AnnouncesNoClipping()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));

        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(4) });
        container.Blocks.Add(Text("Short"));

        var clip = Assert.Single(SceneTrace.Of(DocumentLayouter.Layout(document)).BoxClips);

        Assert.False(clip.ClipsLines);
        Assert.False(clip.ClipsInline);
    }

    [Theory]
    [InlineData("Images/rgb.png", "image/png")]
    [InlineData("Images/rgb.jpg", "image/jpeg")]
    public void CapturedImage_CarriesSniffedMediaType(string resource, string mediaType)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(20));
        section.Blocks.AddImage(PdfTestResources.Open(resource)).Width = Unit.FromPoint(40);

        var trace = SceneTrace.Of(DocumentLayouter.Layout(document));

        Assert.Equal(mediaType, Assert.Single(trace.ImageMediaTypes));
    }
}
