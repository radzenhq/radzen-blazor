#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System;
using Radzen.Blazor.Pdf.Tests;
using Radzen.Documents.Codes;
using Radzen.Documents.Core;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class SceneDumpRendererTests
{
    private const string Latin = BuildTestSupport.Latin;

    private static Paragraph Text(string value, double size = 11)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(value);
        run.Font.Family = Latin;
        run.Font.Size = size;
        return paragraph;
    }

    internal static Document Spaced()
    {
        var document = new Document { Language = "en-US" };
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = Latin;

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(320), Unit.FromPoint(140));
        section.Margins.SetAll(Unit.FromPoint(20));

        var paragraph = section.Blocks.Add(new Paragraph());
        var tracked = paragraph.Inlines.Add("wide open text");
        tracked.Font.Family = Latin;
        tracked.Font.Size = 12;
        tracked.LetterSpacing = Unit.FromPoint(1.5);
        tracked.WordSpacing = Unit.FromPoint(4);
        tracked.HorizontalScale = 1.25;

        return document;
    }

    internal static Document Everything()
    {
        var document = new Document { Language = "en-US" };
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = Latin;
        document.Info.Title = "Scene contract";
        document.Info.Author = "Radzen";
        document.Info.Subject = "Scene dump fixture";
        document.Info.Keywords = "scene;dump;fixture";
        document.Info.Creator = "SceneDumpRendererTests";
        document.Info.CreationDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        document.Info.ModificationDate = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(360), Unit.FromPoint(320));
        section.Margins.SetAll(Unit.FromPoint(24));
        section.Header.Blocks.Add(Text("HEADER", 9));
        section.Footer.Blocks.Add(Text("FOOTER", 9));
        section.Watermark = new Watermark
        {
            Text = "DRAFT",
            Rotation = 30,
            Opacity = 0.25,
        };

        var heading = section.Blocks.Add(Text("Scene contract", 16));
        heading.Inlines[0].Anchor = "top";
        ((Run)heading.Inlines[0]).Font.Bold = true;

        var styled = section.Blocks.Add(new Paragraph());
        var plain = styled.Inlines.Add("Plain ");
        plain.Font.Family = Latin;
        var bold = styled.Inlines.Add("bold ");
        bold.Font.Family = Latin;
        bold.Font.Bold = true;
        var italic = styled.Inlines.Add("italic ");
        italic.Font.Family = Latin;
        italic.Font.Italic = true;
        var colored = styled.Inlines.Add("colored");
        colored.Font.Family = Latin;
        colored.Font.Color = Color.FromRgb(200, 30, 60);
        colored.Font.Underline = true;

        var linked = section.Blocks.Add(new Paragraph());
        var link = linked.Inlines.Add("Radzen site");
        link.Font.Family = Latin;
        link.Link = "https://www.radzen.com/";

        var inline = section.Blocks.Add(new Paragraph());
        inline.Inlines.Add("Inline ").Font.Family = Latin;
        inline.Inlines.Add(new InlineImage(PdfTestResources.Open("Images/rgb.png")));

        var form = section.Blocks.Add(new Paragraph());
        form.Inlines.Add("Name: ").Font.Family = Latin;
        form.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Full name" });

        var controls = section.Blocks.Add(new Paragraph());
        controls.Inlines.Add("Agree ").Font.Family = Latin;
        controls.Inlines.Add(new CheckBox("agree") { Checked = true, Label = "Agree to terms" });
        controls.Inlines.Add(" S").Font.Family = Latin;
        controls.Inlines.Add(new RadioButton("size", "S") { Selected = true });
        controls.Inlines.Add(" M").Font.Family = Latin;
        controls.Inlines.Add(new RadioButton("size", "M"));

        var picker = section.Blocks.Add(new Paragraph());
        var country = new DropDown("country") { Value = "BG", Label = "Country" };
        country.Options.Add("BG");
        country.Options.Add("US");
        picker.Inlines.Add(country);

        var spaced = section.Blocks.Add(new Paragraph());
        var tracked = spaced.Inlines.Add("Tracked out");
        tracked.Font.Family = Latin;
        tracked.LetterSpacing = Unit.FromPoint(1.5);
        tracked.WordSpacing = Unit.FromPoint(3);
        tracked.HorizontalScale = 1.25;
        var superscript = spaced.Inlines.Add("up");
        superscript.Font.Family = Latin;
        superscript.VerticalAlignment = RunVerticalAlignment.Superscript;
        var subscript = spaced.Inlines.Add("down");
        subscript.Font.Family = Latin;
        subscript.VerticalAlignment = RunVerticalAlignment.Subscript;

        var decorated = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(120),
            Padding = Unit.FromPoint(6),
            BackgroundGradient = new LinearGradient(
                Unit.FromPoint(0),
                Unit.FromPoint(0),
                Unit.FromPoint(120),
                Unit.FromPoint(0),
                new GradientStop(0, Color.FromRgb(255, 255, 255)),
                new GradientStop(1, Color.FromRgb(30, 90, 200))),
            Shadow = new BoxShadow
            {
                Color = Color.FromArgb(120, 0, 0, 0),
                OffsetX = Unit.FromPoint(2),
                OffsetY = Unit.FromPoint(3),
                BlurRadius = Unit.FromPoint(4),
                Spread = Unit.FromPoint(1),
            },
            BlendMode = BlendMode.Multiply,
        });
        decorated.Blocks.Add(Text("Decorated", 9));

        var rotated = section.Blocks.Add(new Container
        {
            Layout = ContainerLayout.Overlay,
            Rotation = 15,
            Width = Unit.FromPoint(120),
            Padding = Unit.FromPoint(4),
            Background = Color.FromRgb(250, 240, 210),
        });
        rotated.Blocks.Add(Text("Rotated", 9));

        var barcode = section.Blocks.Add(new Barcode(BarcodeType.Code39, "RZ7", Unit.FromPoint(90), Unit.FromPoint(24)) { ShowText = true });
        barcode.Font.Family = Latin;
        barcode.Font.Size = 8;
        barcode.AlternateText = "Barcode RZ7";

        var list = section.Blocks.Add(new ListBlock());
        list.Items.Add("First item");
        list.Items.Add("Second item");

        var clipped = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(26),
            Padding = Unit.FromPoint(4),
            Background = Color.FromRgb(240, 240, 250),
            CornerRadius = Unit.FromPoint(4),
        });
        clipped.Blocks.Add(Text("WW", 24));
        clipped.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.png")));

        section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));

        var table = section.Blocks.Add(new Table());
        table.Columns.Add(Unit.FromPoint(150));
        table.Columns.Add(Unit.FromPoint(110));
        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        header.Cells[0].Blocks.Add(Text("Product", 10));
        header.Cells[1].Blocks.Add(Text("Amount", 10));
        for (var i = 0; i < 6; i++)
        {
            var row = table.Rows.Add();
            row.Cells[0].Blocks.Add(Text($"Item {i}", 10));
            row.Cells[1].Blocks.Add(Text($"{i * 7}.00", 10));
        }

        var nesting = table.Rows.Add();
        var nested = nesting.Cells[0].Blocks.Add(new Table());
        nested.Columns.Add(Unit.FromPoint(60));
        nested.Columns.Add(Unit.FromPoint(60));
        var nestedHeader = nested.Rows.Add();
        nestedHeader.Cells[0].Blocks.Add(Text("Part", 8));
        nestedHeader.Cells[1].Blocks.Add(Text("Qty", 8));
        var nestedRow = nested.Rows.Add();
        nestedRow.Cells[0].Blocks.Add(Text("Bolt", 8));
        nestedRow.Cells[1].Blocks.Add(Text("12", 8));
        nesting.Cells[1].Blocks.Add(Text("Nested", 10));

        var back = section.Blocks.Add(new Paragraph());
        var jump = back.Inlines.Add("Back to top");
        jump.Font.Family = Latin;
        jump.LinkToAnchor = "top";

        return document;
    }

    private static string Dump() => SceneDumpRenderer.Render(DocumentLayouter.Layout(Everything()));

    [Fact]
    public void SceneDump_MatchesTheCheckedInScene()
        => Assert.Equal(ExpectedScene, Dump().TrimEnd(), ignoreLineEndingDifferences: true);

    [Fact]
    public void SceneDump_IsByteStableAcrossRenders()
        => Assert.Equal(
            Encoding.UTF8.GetBytes(Dump()),
            Encoding.UTF8.GetBytes(Dump()));

    [Theory]
    [InlineData(typeof(RegisteredFace))]
    [InlineData(typeof(CapturedFontFace))]
    public void SceneFontView_HandsOutTheFontProgramOnlyThroughTheExplicitSeam(Type face)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var offenders = face.GetProperties(All)
            .Where(property => property.GetMethod is { IsPrivate: false })
            .Select(property => (property.Name, Type: property.PropertyType))
            .Concat(face.GetFields(All)
                .Where(field => !field.IsPrivate)
                .Select(field => (field.Name, Type: field.FieldType)))
            .Where(member => member.Type == typeof(SfntFont))
            .Select(member => member.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);

        var seam = Assert.Single(
            face.GetProperties(All),
            property => property.Name == $"{typeof(IFontProgramSource).FullName}.Program");
        Assert.True(seam.GetMethod!.IsPrivate);
    }

    [Fact]
    public void SceneFontView_ReportsFamilyStyleAndMetrics()
    {
        var face = Assert.Single(DocumentLayouter.Layout(Everything()).Fonts.Faces);

        Assert.Equal(Latin, face.Family);
        Assert.False(face.Bold);
        Assert.False(face.Italic);
        Assert.True(face.Metrics.UnitsPerEm > 0);
        Assert.True(face.Metrics.Ascent > face.Metrics.Descent);
    }

    private static List<string> SceneTexts(string dump)
    {
        var texts = new List<string>();
        foreach (Match match in Regex.Matches(dump, "^\\s*run \"([^\"]*)\"", RegexOptions.Multiline))
        {
            texts.Add(match.Groups[1].Value);
        }

        return texts;
    }

    private static int Count(string dump, int page, string prefix)
    {
        var counts = PerPage(dump, prefix);
        return page < counts.Count ? counts[page] : 0;
    }

    private static List<int> PerPage(string dump, string prefix)
    {
        var counts = new List<int>();
        foreach (var line in dump.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("page ", StringComparison.Ordinal))
            {
                counts.Add(0);
            }
            else if (counts.Count > 0 && trimmed.StartsWith(prefix, StringComparison.Ordinal))
            {
                counts[^1]++;
            }
        }

        return counts;
    }

    private static List<int> PdfLinkCounts(DocumentReader reader, int pages)
    {
        var counts = new List<int>();
        for (var index = 0; index < pages; index++)
        {
            var page = BuildTestSupport.PageLeaves(reader)[index].Page;
            var links = 0;
            if (page.TryGetValue("Annots", out var annots) && reader.Resolve(annots!) is ArrayObject array)
            {
                foreach (var item in array)
                {
                    if (reader.Resolve(item) is DictionaryObject annotation
                        && annotation.TryGetValue("Subtype", out var subtype)
                        && reader.Resolve(subtype!) is NameObject { Value: "Link" })
                    {
                        links++;
                    }
                }
            }

            counts.Add(links);
        }

        return counts;
    }

    private static List<int> PdfImageCounts(DocumentReader reader, int pages)
    {
        var counts = new List<int>();
        for (var index = 0; index < pages; index++)
        {
            var (_, resources) = BuildTestSupport.PageLeaves(reader)[index];
            var images = 0;
            if (resources is not null
                && resources.TryGetValue("XObject", out var xobject)
                && reader.Resolve(xobject!) is DictionaryObject xobjects)
            {
                foreach (var key in xobjects.Keys)
                {
                    if (reader.Resolve(xobjects[key]) is StreamObject stream
                        && stream.Dictionary.TryGetValue("Subtype", out var subtype)
                        && subtype is NameObject { Value: "Image" })
                    {
                        images++;
                    }
                }
            }

            counts.Add(images);
        }

        return counts;
    }

    private readonly record struct Placement(double Left, double Top, double Right, double Bottom);

    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    private static List<double> PageHeights(string dump)
    {
        var heights = new List<double>();
        foreach (Match match in Regex.Matches(dump, @"^\s*page \d+ size=[\d.]+x([\d.]+)", RegexOptions.Multiline))
        {
            heights.Add(Number(match.Groups[1].Value));
        }

        return heights;
    }

    private static List<List<Placement>> PerPagePlacements(string dump, string pattern, Func<Match, Placement> read)
    {
        var pages = new List<List<Placement>>();
        foreach (var line in dump.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("page ", StringComparison.Ordinal))
            {
                pages.Add([]);
                continue;
            }

            if (pages.Count == 0)
            {
                continue;
            }

            var match = Regex.Match(trimmed, pattern);
            if (match.Success)
            {
                pages[^1].Add(read(match));
            }
        }

        return pages;
    }

    private static List<List<Placement>> SceneLinkRects(string dump)
        => PerPagePlacements(
            dump,
            @"^link rect=(-?[\d.]+),(-?[\d.]+),(-?[\d.]+),(-?[\d.]+) ",
            match => new Placement(
                Number(match.Groups[1].Value),
                Number(match.Groups[2].Value),
                Number(match.Groups[3].Value),
                Number(match.Groups[4].Value)));

    private static List<List<Placement>> SceneImageRects(string dump)
    {
        var pages = new List<List<Placement>>();
        var runX = 0.0;
        var runBaseline = 0.0;
        foreach (var line in dump.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("page ", StringComparison.Ordinal))
            {
                pages.Add([]);
                continue;
            }

            if (pages.Count == 0)
            {
                continue;
            }

            var run = Regex.Match(trimmed, @"^run "".*"" at=(-?[\d.]+),(-?[\d.]+) ");
            if (run.Success)
            {
                runX = Number(run.Groups[1].Value);
                runBaseline = Number(run.Groups[2].Value);
                continue;
            }

            var inline = Regex.Match(trimmed, @"^inlineImage media=\S+ size=([\d.]+)x([\d.]+)");
            if (inline.Success)
            {
                var width = Number(inline.Groups[1].Value);
                var height = Number(inline.Groups[2].Value);
                pages[^1].Add(new Placement(runX, runBaseline - height, runX + width, runBaseline));
                continue;
            }

            var placed = Regex.Match(
                trimmed,
                @"^image media=\S+ at=(-?[\d.]+),(-?[\d.]+) size=([\d.]+)x([\d.]+)");
            if (placed.Success)
            {
                var x = Number(placed.Groups[1].Value);
                var y = Number(placed.Groups[2].Value);
                pages[^1].Add(new Placement(
                    x,
                    y,
                    x + Number(placed.Groups[3].Value),
                    y + Number(placed.Groups[4].Value)));
            }
        }

        return pages;
    }

    private static List<Placement> PdfLinkRects(DocumentReader reader, int pageIndex, double pageHeight)
    {
        var rects = new List<Placement>();
        var page = BuildTestSupport.PageLeaves(reader)[pageIndex].Page;
        if (page.TryGetValue("Annots", out var annots) && reader.Resolve(annots!) is ArrayObject array)
        {
            foreach (var item in array)
            {
                if (reader.Resolve(item) is not DictionaryObject annotation
                    || annotation.TryGetValue("Subtype", out var subtype) is false
                    || reader.Resolve(subtype!) is not NameObject { Value: "Link" })
                {
                    continue;
                }

                var box = (ArrayObject)reader.Resolve(annotation["Rect"]);
                var values = box.Select(value => ((NumberObject)reader.Resolve(value)).DoubleValue).ToArray();
                rects.Add(new Placement(
                    values[0],
                    pageHeight - values[3],
                    values[2],
                    pageHeight - values[1]));
            }
        }

        return rects;
    }

    private static List<Placement> PdfImageRects(DocumentReader reader, int pageIndex, double pageHeight)
    {
        var (page, resources) = BuildTestSupport.PageLeaves(reader)[pageIndex];
        var images = new HashSet<string>(StringComparer.Ordinal);
        if (resources is not null
            && resources.TryGetValue("XObject", out var xobject)
            && reader.Resolve(xobject!) is DictionaryObject xobjects)
        {
            foreach (var key in xobjects.Keys)
            {
                if (reader.Resolve(xobjects[key]) is StreamObject stream
                    && stream.Dictionary.TryGetValue("Subtype", out var subtype)
                    && subtype is NameObject { Value: "Image" })
                {
                    images.Add(key);
                }
            }
        }

        var content = new StringBuilder();
        var resolved = reader.Resolve(page["Contents"]);
        if (resolved is StreamObject single)
        {
            content.Append(FormTestSupport.Decode(single));
        }
        else
        {
            foreach (var part in (ArrayObject)resolved)
            {
                content.Append(FormTestSupport.Decode((StreamObject)reader.Resolve(part)));
            }
        }

        var rects = new List<Placement>();
        foreach (Match match in Regex.Matches(
            content.ToString(),
            @"(-?[\d.]+) 0 0 (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) cm\s*/(\S+) Do"))
        {
            if (!images.Contains(match.Groups[5].Value))
            {
                continue;
            }

            var width = Number(match.Groups[1].Value);
            var height = Number(match.Groups[2].Value);
            var x = Number(match.Groups[3].Value);
            var bottom = Number(match.Groups[4].Value);
            rects.Add(new Placement(x, pageHeight - (bottom + height), x + width, pageHeight - bottom));
        }

        return rects;
    }

    private static List<Placement> Sorted(IEnumerable<Placement> placements)
        => [.. placements
            .OrderBy(placement => placement.Left)
            .ThenBy(placement => placement.Top)
            .ThenBy(placement => placement.Right)
            .ThenBy(placement => placement.Bottom)];

    private static void SamePlacements(IEnumerable<Placement> printed, IEnumerable<Placement> scene)
    {
        var expected = Sorted(printed);
        var actual = Sorted(scene);
        Assert.Equal(expected.Count, actual.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Left, actual[i].Left, 0.05);
            Assert.Equal(expected[i].Top, actual[i].Top, 0.05);
            Assert.Equal(expected[i].Right, actual[i].Right, 0.05);
            Assert.Equal(expected[i].Bottom, actual[i].Bottom, 0.05);
        }
    }

    [Fact]
    public void SceneDump_CarriesEverythingThePdfShows()
    {
        var document = Everything();
        var dump = SceneDumpRenderer.Render(DocumentLayouter.Layout(document));
        var reader = BuildTestSupport.Read(document);
        var reloaded = BuildTestSupport.Reload(document);

        var scenePages = PerPage(dump, "page ").Count;
        Assert.Equal(reloaded.Pages.Count, scenePages);

        var extracted = string.Concat(reloaded.Pages.Select(page => page.ExtractText()));
        var stripped = Regex.Replace(extracted, "\\s+", string.Empty);
        foreach (var text in SceneTexts(dump))
        {
            var expected = Regex.Replace(text, "\\s+", string.Empty);
            if (expected.Length > 0)
            {
                Assert.Contains(expected, stripped, StringComparison.Ordinal);
            }
        }

        Assert.Equal(PdfLinkCounts(reader, scenePages), PerPage(dump, "link "));

        var sceneImages = new List<int>();
        for (var page = 0; page < scenePages; page++)
        {
            sceneImages.Add(Count(dump, page, "image ") + Count(dump, page, "inlineImage "));
        }

        Assert.Equal(PdfImageCounts(reader, scenePages), sceneImages);

        var heights = PageHeights(dump);
        var sceneLinks = SceneLinkRects(dump);
        var sceneImageRects = SceneImageRects(dump);
        for (var page = 0; page < scenePages; page++)
        {
            SamePlacements(PdfLinkRects(reader, page, heights[page]), sceneLinks[page]);
            SamePlacements(PdfImageRects(reader, page, heights[page]), sceneImageRects[page]);
        }
    }

    [Fact]
    public void SpacedText_PositionsClustersByTheSpacingFormula()
        => Assert.Equal(
            ExpectedSpacedScene,
            SceneDumpRenderer.Render(DocumentLayouter.Layout(Spaced())).TrimEnd(),
            ignoreLineEndingDifferences: true);

    private const string ExpectedSpacedScene =
        """
        scene pages=1 language=en-US
          info title=- author=- subject=- keywords=- creator=- created=- modified=-
          page 0 size=320.00x140.00 content=20.00,20.00,280.00,100.00
            layer Body top=20.00
              item 0 z=0
                line at=20.00,20.00 size=131.11x13.80 baseline=10.86 role=Paragraph
                  run "wide" at=20.00,30.86 advance=36.47 family=Liberation Sans size=12.00 style=regular color=#000000FF opacity=1.00 spacing=1.50/4.00 scale=1.25
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=20.00 advance=24.68
                      cluster 0 u+0077 x=20.00 advance=8.67 kerning=0.00
                      cluster 1 u+0069 x=32.71 advance=2.67 kerning=0.00
                      cluster 2 u+0064 x=37.92 advance=6.67 kerning=0.00
                      cluster 3 u+0065 x=48.13 advance=6.67 kerning=0.00
                    /span
                  /run
                  run "open" at=69.39,30.86 advance=38.99 family=Liberation Sans size=12.00 style=regular color=#000000FF opacity=1.00 spacing=1.50/4.00 scale=1.25
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=69.39 advance=26.70
                      cluster 0 u+006F x=69.39 advance=6.67 kerning=0.00
                      cluster 1 u+0070 x=79.61 advance=6.67 kerning=0.00
                      cluster 2 u+0065 x=89.83 advance=6.67 kerning=0.00
                      cluster 3 u+006E x=100.04 advance=6.67 kerning=0.00
                    /span
                  /run
                  run "text" at=121.30,30.86 advance=29.80 family=Liberation Sans size=12.00 style=regular color=#000000FF opacity=1.00 spacing=1.50/4.00 scale=1.25
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=121.30 advance=19.34
                      cluster 0 u+0074 x=121.30 advance=3.33 kerning=0.00
                      cluster 1 u+0065 x=127.35 advance=6.67 kerning=0.00
                      cluster 2 u+0078 x=137.56 advance=6.00 kerning=0.00
                      cluster 3 u+0074 x=146.94 advance=3.33 kerning=0.00
                    /span
                  /run
                /line
              /item
            /layer Body
            layer Header top=35.43
            /layer Header
            layer Footer top=104.57
            /layer Footer
          /page
        /scene
        """;

    private const string ExpectedScene =
        """
        scene pages=3 language=en-US
          info title=Scene contract author=Radzen subject=Scene dump fixture keywords=scene;dump;fixture creator=SceneDumpRendererTests created=2026-01-02T03:04:05+00:00 modified=2026-02-03T04:05:06+00:00
          page 0 size=360.00x320.00 content=24.00,45.78,312.00,228.44
            layer Body top=45.78
              item 0 z=0
                line at=24.00,45.78 size=106.73x18.40 baseline=14.48 role=Paragraph
                  run "Scene contract" at=24.00,60.27 advance=106.73 family=Liberation Sans size=16.00 style=bold color=#000000FF opacity=1.00 anchor=top
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=106.73
                      cluster 0 u+0053 x=24.00 advance=10.67 kerning=0.00
                      cluster 1 u+0063 x=34.67 advance=8.00 kerning=0.00
                      cluster 2 u+0065 x=42.67 advance=8.90 kerning=0.00
                      cluster 3 u+006E x=51.57 advance=8.90 kerning=0.00
                      cluster 4 u+0065 x=60.47 advance=8.90 kerning=0.00
                      cluster 5 u+0020 x=69.37 advance=4.45 kerning=0.00
                      cluster 6 u+0063 x=73.81 advance=8.00 kerning=0.00
                      cluster 7 u+006F x=81.81 advance=8.90 kerning=0.00
                      cluster 8 u+006E x=90.71 advance=8.90 kerning=0.00
                      cluster 9 u+0074 x=99.61 advance=4.45 kerning=0.00
                      cluster 10 u+0072 x=104.05 advance=5.33 kerning=0.00
                      cluster 11 u+0061 x=109.38 advance=8.90 kerning=0.00
                      cluster 12 u+0063 x=118.28 advance=8.00 kerning=0.00
                      cluster 13 u+0074 x=126.28 advance=4.45 kerning=0.00
                    /span
                  /run
                /line
              /item
              item 1 z=1
                line at=24.00,64.18 size=102.28x11.50 baseline=9.05 role=Paragraph
                  run "Plain" at=24.00,73.23 advance=22.24 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=22.24
                      cluster 0 u+0050 x=24.00 advance=6.67 kerning=0.00
                      cluster 1 u+006C x=30.67 advance=2.22 kerning=0.00
                      cluster 2 u+0061 x=32.89 advance=5.56 kerning=0.00
                      cluster 3 u+0069 x=38.45 advance=2.22 kerning=0.00
                      cluster 4 u+006E x=40.67 advance=5.56 kerning=0.00
                    /span
                  /run
                  run "bold" at=49.01,73.23 advance=18.91 family=Liberation Sans size=10.00 style=bold color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=49.01 advance=18.91
                      cluster 0 u+0062 x=49.01 advance=5.56 kerning=0.00
                      cluster 1 u+006F x=54.58 advance=5.56 kerning=0.00
                      cluster 2 u+006C x=60.14 advance=2.22 kerning=0.00
                      cluster 3 u+0064 x=62.36 advance=5.56 kerning=0.00
                    /span
                  /run
                  run "italic" at=70.70,73.23 advance=20.00 family=Liberation Sans size=10.00 style=italic color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=70.70 advance=20.00
                      cluster 0 u+0069 x=70.70 advance=2.22 kerning=0.00
                      cluster 1 u+0074 x=72.92 advance=2.78 kerning=0.00
                      cluster 2 u+0061 x=75.70 advance=5.56 kerning=0.00
                      cluster 3 u+006C x=81.26 advance=2.22 kerning=0.00
                      cluster 4 u+0069 x=83.48 advance=2.22 kerning=0.00
                      cluster 5 u+0063 x=85.70 advance=5.00 kerning=0.00
                    /span
                  /run
                  run "colored" at=93.48,73.23 advance=32.80 family=Liberation Sans size=10.00 style=underline color=#C81E3CFF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=93.48 advance=32.80
                      cluster 0 u+0063 x=93.48 advance=5.00 kerning=0.00
                      cluster 1 u+006F x=98.48 advance=5.56 kerning=0.00
                      cluster 2 u+006C x=104.04 advance=2.22 kerning=0.00
                      cluster 3 u+006F x=106.27 advance=5.56 kerning=0.00
                      cluster 4 u+0072 x=111.83 advance=3.33 kerning=0.00
                      cluster 5 u+0065 x=115.16 advance=5.56 kerning=0.00
                      cluster 6 u+0064 x=120.72 advance=5.56 kerning=0.00
                    /span
                  /run
                /line
              /item
              item 2 z=2
                line at=24.00,75.68 size=52.81x11.50 baseline=9.05 role=Paragraph
                  run "Radzen site" at=24.00,84.73 advance=52.81 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 link=https://www.radzen.com/ role=Link
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=52.81
                      cluster 0 u+0052 x=24.00 advance=7.22 kerning=0.00
                      cluster 1 u+0061 x=31.22 advance=5.56 kerning=0.00
                      cluster 2 u+0064 x=36.78 advance=5.56 kerning=0.00
                      cluster 3 u+007A x=42.34 advance=5.00 kerning=0.00
                      cluster 4 u+0065 x=47.34 advance=5.56 kerning=0.00
                      cluster 5 u+006E x=52.91 advance=5.56 kerning=0.00
                      cluster 6 u+0020 x=58.47 advance=2.78 kerning=0.00
                      cluster 7 u+0073 x=61.25 advance=5.00 kerning=0.00
                      cluster 8 u+0069 x=66.25 advance=2.22 kerning=0.00
                      cluster 9 u+0074 x=68.47 advance=2.78 kerning=0.00
                      cluster 10 u+0065 x=71.25 advance=5.56 kerning=0.00
                    /span
                  /run
                /line
              /item
              item 3 z=3
                line at=24.00,87.18 size=74.68x48.00 baseline=48.00 role=Paragraph
                  run "Inline" at=24.00,135.18 advance=23.91 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=23.91
                      cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                      cluster 1 u+006E x=26.78 advance=5.56 kerning=0.00
                      cluster 2 u+006C x=32.34 advance=2.22 kerning=0.00
                      cluster 3 u+0069 x=34.56 advance=2.22 kerning=0.00
                      cluster 4 u+006E x=36.78 advance=5.56 kerning=0.00
                      cluster 5 u+0065 x=42.34 advance=5.56 kerning=0.00
                    /span
                  /run
                  run "" at=50.68,135.18 advance=48.00 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 role=Figure
                    inlineImage media=image/png size=48.00x48.00
                  /run
                /line
              /item
              item 4 z=4
                line at=24.00,135.18 size=132.23x14.00 baseline=11.00 role=Paragraph
                  run "Name:" at=24.00,146.18 advance=29.45 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=29.45
                      cluster 0 u+004E x=24.00 advance=7.22 kerning=0.00
                      cluster 1 u+0061 x=31.22 advance=5.56 kerning=0.00
                      cluster 2 u+006D x=36.78 advance=8.33 kerning=0.00
                      cluster 3 u+0065 x=45.11 advance=5.56 kerning=0.00
                      cluster 4 u+003A x=50.67 advance=2.78 kerning=0.00
                    /span
                  /run
                  run "" at=56.23,146.18 advance=100.00 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 role=Form
                    formField Text name=name value="Ada" label=Full name required=False chosen=False size=100.00x14.00 ascent=11.00 options=[]
                      span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=56.23 advance=17.79
                        cluster 0 u+0041 x=56.23 advance=6.67 kerning=0.00
                        cluster 1 u+0064 x=62.90 advance=5.56 kerning=0.00
                        cluster 2 u+0061 x=68.46 advance=5.56 kerning=0.00
                      /span
                    /formField
                  /run
                /line
              /item
              item 5 z=5
                line at=24.00,149.18 size=80.02x11.50 baseline=9.05 role=Paragraph
                  run "Agree" at=24.00,158.23 advance=26.68 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=26.68
                      cluster 0 u+0041 x=24.00 advance=6.67 kerning=0.00
                      cluster 1 u+0067 x=30.67 advance=5.56 kerning=0.00
                      cluster 2 u+0072 x=36.23 advance=3.33 kerning=0.00
                      cluster 3 u+0065 x=39.56 advance=5.56 kerning=0.00
                      cluster 4 u+0065 x=45.12 advance=5.56 kerning=0.00
                    /span
                  /run
                  run "" at=53.46,158.23 advance=10.00 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 role=Form
                    formField CheckBox name=agree value="" label=Agree to terms required=False chosen=True size=10.00x10.00 ascent=8.00 options=[]
                    /formField
                  /run
                  run "S" at=66.24,158.23 advance=6.67 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=66.24 advance=6.67
                      cluster 0 u+0053 x=66.24 advance=6.67 kerning=0.00
                    /span
                  /run
                  run "" at=72.91,158.23 advance=10.00 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 role=Form
                    formField Radio name=size value="S" label=- required=False chosen=True size=10.00x10.00 ascent=8.00 options=[]
                    /formField
                  /run
                  run "M" at=85.69,158.23 advance=8.33 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=85.69 advance=8.33
                      cluster 0 u+004D x=85.69 advance=8.33 kerning=0.00
                    /span
                  /run
                  run "" at=94.02,158.23 advance=10.00 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 role=Form
                    formField Radio name=size value="M" label=- required=False chosen=False size=10.00x10.00 ascent=8.00 options=[]
                    /formField
                  /run
                /line
              /item
              item 6 z=6
                line at=24.00,160.68 size=100.00x14.00 baseline=11.00 role=Paragraph
                  run "" at=24.00,171.68 advance=100.00 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 role=Form
                    formField DropDown name=country value="BG" label=Country required=False chosen=False size=100.00x14.00 ascent=11.00 options=[BG|US]
                      span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=14.45
                        cluster 0 u+0042 x=24.00 advance=6.67 kerning=0.00
                        cluster 1 u+0047 x=30.67 advance=7.78 kerning=0.00
                      /span
                    /formField
                  /run
                /line
              /item
              item 7 z=7
                line at=24.00,174.68 size=108.93x11.50 baseline=9.05 role=Paragraph
                  run "Tracked" at=24.00,183.73 advance=56.40 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 spacing=1.50/3.00 scale=1.25
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=36.12
                      cluster 0 u+0054 x=24.00 advance=6.11 kerning=0.00
                      cluster 1 u+0072 x=33.51 advance=3.33 kerning=0.00
                      cluster 2 u+0061 x=39.55 advance=5.56 kerning=0.00
                      cluster 3 u+0063 x=48.38 advance=5.00 kerning=0.00
                      cluster 4 u+006B x=56.50 advance=5.00 kerning=0.00
                      cluster 5 u+0065 x=64.62 advance=5.56 kerning=0.00
                      cluster 6 u+0064 x=73.45 advance=5.56 kerning=0.00
                    /span
                  /run
                  run "out" at=91.38,183.73 advance=21.13 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 spacing=1.50/3.00 scale=1.25
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=91.38 advance=13.90
                      cluster 0 u+006F x=91.38 advance=5.56 kerning=0.00
                      cluster 1 u+0075 x=100.20 advance=5.56 kerning=0.00
                      cluster 2 u+0074 x=109.03 advance=2.78 kerning=0.00
                    /span
                  /run
                  run "up" at=112.50,183.73 advance=6.48 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 script=0.58@3.30
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=112.50 advance=11.12
                      cluster 0 u+0075 x=112.50 advance=5.56 kerning=0.00
                      cluster 1 u+0070 x=115.75 advance=5.56 kerning=0.00
                    /span
                  /run
                  run "down" at=118.99,183.73 advance=13.94 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 script=0.58@-2.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=118.99 advance=23.91
                      cluster 0 u+0064 x=118.99 advance=5.56 kerning=0.00
                      cluster 1 u+006F x=122.23 advance=5.56 kerning=0.00
                      cluster 2 u+0077 x=125.47 advance=7.22 kerning=0.00
                      cluster 3 u+006E x=129.68 advance=5.56 kerning=0.00
                    /span
                  /run
                /line
              /item
              item 8 z=11
                line at=24.00,260.07 size=41.11x11.50 baseline=9.05 role=Paragraph
                  run "•" at=24.00,269.13 advance=3.50 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 marker
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=3.50
                      cluster 0 u+2022 x=24.00 advance=3.50 kerning=0.00
                    /span
                  /run
                  run "First item" at=42.00,269.13 advance=41.11 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=42.00 advance=41.11
                      cluster 0 u+0046 x=42.00 advance=6.11 kerning=0.00
                      cluster 1 u+0069 x=48.11 advance=2.22 kerning=0.00
                      cluster 2 u+0072 x=50.33 advance=3.33 kerning=0.00
                      cluster 3 u+0073 x=53.66 advance=5.00 kerning=0.00
                      cluster 4 u+0074 x=58.66 advance=2.78 kerning=0.00
                      cluster 5 u+0020 x=61.44 advance=2.78 kerning=0.00
                      cluster 6 u+0069 x=64.22 advance=2.22 kerning=0.00
                      cluster 7 u+0074 x=66.44 advance=2.78 kerning=0.00
                      cluster 8 u+0065 x=69.22 advance=5.56 kerning=0.00
                      cluster 9 u+006D x=74.78 advance=8.33 kerning=0.00
                    /span
                  /run
                /line
              /item
              item 9 z=8
                box at=24.00,186.18 size=120.00x22.35 opacity=1.00 transform=- gradient=Linear(0.00,0.00,0.00->120.00,0.00,0.00)[0.00:#FFFFFFFF 1.00:#1E5AC8FF] shadow=#00000078@2.00,3.00/4.00/1.00 blend=Multiply
                  item 10 z=0
                    line at=30.00,192.18 size=41.52x10.35 baseline=8.15 role=Paragraph
                      run "Decorated" at=30.00,200.32 advance=41.52 family=Liberation Sans size=9.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=30.00 advance=41.52
                          cluster 0 u+0044 x=30.00 advance=6.50 kerning=0.00
                          cluster 1 u+0065 x=36.50 advance=5.01 kerning=0.00
                          cluster 2 u+0063 x=41.50 advance=4.50 kerning=0.00
                          cluster 3 u+006F x=46.00 advance=5.01 kerning=0.00
                          cluster 4 u+0072 x=51.01 advance=3.00 kerning=0.00
                          cluster 5 u+0061 x=54.01 advance=5.01 kerning=0.00
                          cluster 6 u+0074 x=59.01 advance=2.50 kerning=0.00
                          cluster 7 u+0065 x=61.51 advance=5.01 kerning=0.00
                          cluster 8 u+0064 x=66.52 advance=5.01 kerning=0.00
                        /span
                      /run
                    /line
                  /item
                /box
              /item
              item 11 z=9
                box at=24.00,208.53 size=120.00x18.35 opacity=1.00 transform=0.97,-0.26,0.26,0.97,-53.48,29.16 fill=#FAF0D2FF
                  item 12 z=0
                    line at=28.00,212.53 size=31.52x10.35 baseline=8.15 role=Paragraph
                      run "Rotated" at=28.00,220.67 advance=31.52 family=Liberation Sans size=9.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=28.00 advance=31.52
                          cluster 0 u+0052 x=28.00 advance=6.50 kerning=0.00
                          cluster 1 u+006F x=34.50 advance=5.01 kerning=0.00
                          cluster 2 u+0074 x=39.50 advance=2.50 kerning=0.00
                          cluster 3 u+0061 x=42.01 advance=5.01 kerning=0.00
                          cluster 4 u+0074 x=47.01 advance=2.50 kerning=0.00
                          cluster 5 u+0065 x=49.51 advance=5.01 kerning=0.00
                          cluster 6 u+0064 x=54.52 advance=5.01 kerning=0.00
                        /span
                      /run
                    /line
                  /item
                /box
              /item
              item 13 z=10
                code modules=25 foreground=#000000FF at=24.00,226.87 size=90.00x33.20 role=Figure
                  module at=34.71,226.87 size=1.07x24.00
                  module at=37.93,226.87 size=1.07x24.00
                  module at=40.07,226.87 size=2.14x24.00
                  module at=43.29,226.87 size=2.14x24.00
                  module at=46.50,226.87 size=1.07x24.00
                  module at=48.64,226.87 size=2.14x24.00
                  module at=51.86,226.87 size=1.07x24.00
                  module at=54.00,226.87 size=1.07x24.00
                  module at=56.14,226.87 size=2.14x24.00
                  module at=60.43,226.87 size=1.07x24.00
                  module at=62.57,226.87 size=1.07x24.00
                  module at=65.79,226.87 size=2.14x24.00
                  module at=69.00,226.87 size=2.14x24.00
                  module at=72.21,226.87 size=1.07x24.00
                  module at=74.36,226.87 size=1.07x24.00
                  module at=76.50,226.87 size=1.07x24.00
                  module at=78.64,226.87 size=1.07x24.00
                  module at=81.86,226.87 size=1.07x24.00
                  module at=84.00,226.87 size=2.14x24.00
                  module at=87.21,226.87 size=2.14x24.00
                  module at=90.43,226.87 size=1.07x24.00
                  module at=93.64,226.87 size=1.07x24.00
                  module at=95.79,226.87 size=2.14x24.00
                  module at=99.00,226.87 size=2.14x24.00
                  module at=102.21,226.87 size=1.07x24.00
                  line at=24.00,250.87 size=15.11x9.20 baseline=7.24 role=Figure
                    run "RZ7" at=61.44,258.12 advance=15.11 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                      span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=61.44 advance=15.11
                        cluster 0 u+0052 x=61.44 advance=5.78 kerning=0.00
                        cluster 1 u+005A x=67.22 advance=4.89 kerning=0.00
                        cluster 2 u+0037 x=72.11 advance=4.45 kerning=0.00
                      /span
                    /run
                  /line
                /code
              /item
            /layer Body
            layer Header top=35.43
              item 14 z=0
                line at=24.00,35.43 size=37.51x10.35 baseline=8.15 artifact=Pagination
                  run "HEADER" at=24.00,43.58 advance=37.51 family=Liberation Sans size=9.00 style=regular color=#000000FF opacity=1.00 artifact=Pagination
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=37.51
                      cluster 0 u+0048 x=24.00 advance=6.50 kerning=0.00
                      cluster 1 u+0045 x=30.50 advance=6.00 kerning=0.00
                      cluster 2 u+0041 x=36.50 advance=6.00 kerning=0.00
                      cluster 3 u+0044 x=42.51 advance=6.50 kerning=0.00
                      cluster 4 u+0045 x=49.00 advance=6.00 kerning=0.00
                      cluster 5 u+0052 x=55.01 advance=6.50 kerning=0.00
                    /span
                  /run
                /line
              /item
            /layer Header
            layer Footer top=274.22
              item 15 z=0
                line at=24.00,274.22 size=37.50x10.35 baseline=8.15 artifact=Pagination
                  run "FOOTER" at=24.00,282.37 advance=37.50 family=Liberation Sans size=9.00 style=regular color=#000000FF opacity=1.00 artifact=Pagination
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=37.50
                      cluster 0 u+0046 x=24.00 advance=5.50 kerning=0.00
                      cluster 1 u+004F x=29.50 advance=7.00 kerning=0.00
                      cluster 2 u+004F x=36.50 advance=7.00 kerning=0.00
                      cluster 3 u+0054 x=43.50 advance=5.50 kerning=0.00
                      cluster 4 u+0045 x=49.00 advance=6.00 kerning=0.00
                      cluster 5 u+0052 x=55.00 advance=6.50 kerning=0.00
                    /span
                  /run
                /line
              /item
            /layer Footer
            link rect=24.00,75.68,76.81,86.85 uri=https://www.radzen.com/ anchor=- role=Link
            anchor top top=45.78
            watermark center=180.00,160.00 rotation=30.00 opacity=0.25
              watermarkText "DRAFT" family=Helvetica size=72.00 color=#000000FF at=-119.99,25.20 alpha=-
                span face=sans-serif bold=False italic=False ascent=718.00 descent=-207.00 upem=1000.00 x=-119.99 advance=239.98
                  cluster 0 u+0044 x=-119.99 advance=51.98 kerning=0.00
                  cluster 1 u+0052 x=-68.00 advance=51.98 kerning=0.00
                  cluster 2 u+0041 x=-16.02 advance=48.02 kerning=0.00
                  cluster 3 u+0046 x=32.00 advance=43.99 kerning=0.00
                  cluster 4 u+0054 x=76.00 advance=43.99 kerning=0.00
                /span
              /watermarkText
            /watermark
          /page
          page 1 size=360.00x320.00 content=24.00,45.78,312.00,228.44
            layer Body top=45.78
              item 0 z=12
                line at=24.00,45.78 size=55.59x11.50 baseline=9.05 role=Paragraph
                  run "•" at=24.00,54.83 advance=3.50 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 marker
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=3.50
                      cluster 0 u+2022 x=24.00 advance=3.50 kerning=0.00
                    /span
                  /run
                  run "Second item" at=42.00,54.83 advance=55.59 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=42.00 advance=55.59
                      cluster 0 u+0053 x=42.00 advance=6.67 kerning=0.00
                      cluster 1 u+0065 x=48.67 advance=5.56 kerning=0.00
                      cluster 2 u+0063 x=54.23 advance=5.00 kerning=0.00
                      cluster 3 u+006F x=59.23 advance=5.56 kerning=0.00
                      cluster 4 u+006E x=64.79 advance=5.56 kerning=0.00
                      cluster 5 u+0064 x=70.35 advance=5.56 kerning=0.00
                      cluster 6 u+0020 x=75.92 advance=2.78 kerning=0.00
                      cluster 7 u+0069 x=78.69 advance=2.22 kerning=0.00
                      cluster 8 u+0074 x=80.92 advance=2.78 kerning=0.00
                      cluster 9 u+0065 x=83.69 advance=5.56 kerning=0.00
                      cluster 10 u+006D x=89.26 advance=8.33 kerning=0.00
                    /span
                  /run
                /line
              /item
              item 1 z=13
                box at=24.00,57.28 size=26.00x81.20 opacity=1.00 transform=- fill=#F0F0FAFF radius=4.00 clip=24.00,57.28,26.00,81.20+lines
                  item 2 z=0
                    line at=28.00,61.28 size=22.65x27.60 baseline=21.73 role=Paragraph
                      run "W" at=28.00,83.01 advance=22.65 family=Liberation Sans size=24.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=28.00 advance=22.65
                          cluster 0 u+0057 x=28.00 advance=22.65 kerning=0.00
                        /span
                      /run
                    /line
                  /item
                  item 3 z=1
                    line at=28.00,88.88 size=22.65x27.60 baseline=21.73 role=Paragraph
                      run "W" at=28.00,110.61 advance=22.65 family=Liberation Sans size=24.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=28.00 advance=22.65
                          cluster 0 u+0057 x=28.00 advance=22.65 kerning=0.00
                        /span
                      /run
                    /line
                  /item
                  item 4 z=2
                    image media=image/png at=28.00,116.48 size=18.00x18.00 opacity=1.00 interpolate=False role=Figure
                  /item
                /box
              /item
              item 5 z=15
                tableFragment at=24.00,186.48 size=260.00x80.49 rows=7
                  row 0 header=True y=186.48 height=11.50 cells=2
                    cell 0,0 span=1x1 at=24.00,186.48 size=150.00x11.50 opacity=1.00 role=TableCell
                      item 6 z=0
                        line at=24.00,186.48 size=34.46x11.50 baseline=9.05 role=Paragraph
                          run "Product" at=24.00,195.53 advance=34.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=34.46
                              cluster 0 u+0050 x=24.00 advance=6.67 kerning=0.00
                              cluster 1 u+0072 x=30.67 advance=3.33 kerning=0.00
                              cluster 2 u+006F x=34.00 advance=5.56 kerning=0.00
                              cluster 3 u+0064 x=39.56 advance=5.56 kerning=0.00
                              cluster 4 u+0075 x=45.12 advance=5.56 kerning=0.00
                              cluster 5 u+0063 x=50.68 advance=5.00 kerning=0.00
                              cluster 6 u+0074 x=55.68 advance=2.78 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                    cell 0,1 span=1x1 at=174.00,186.48 size=110.00x11.50 opacity=1.00 role=TableCell
                      item 7 z=0
                        line at=174.00,186.48 size=34.46x11.50 baseline=9.05 role=Paragraph
                          run "Amount" at=174.00,195.53 advance=34.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=34.46
                              cluster 0 u+0041 x=174.00 advance=6.67 kerning=0.00
                              cluster 1 u+006D x=180.67 advance=8.33 kerning=0.00
                              cluster 2 u+006F x=189.00 advance=5.56 kerning=0.00
                              cluster 3 u+0075 x=194.56 advance=5.56 kerning=0.00
                              cluster 4 u+006E x=200.12 advance=5.56 kerning=0.00
                              cluster 5 u+0074 x=205.68 advance=2.78 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                  row 1 header=False y=197.98 height=11.50 cells=2
                    cell 1,0 span=1x1 at=24.00,197.98 size=150.00x11.50 opacity=1.00 role=TableCell
                      item 8 z=0
                        line at=24.00,197.98 size=27.79x11.50 baseline=9.05 role=Paragraph
                          run "Item 0" at=24.00,207.03 advance=27.79 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=27.79
                              cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                              cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                              cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                              cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                              cluster 4 u+0020 x=43.45 advance=2.78 kerning=0.00
                              cluster 5 u+0030 x=46.23 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                    cell 1,1 span=1x1 at=174.00,197.98 size=110.00x11.50 opacity=1.00 role=TableCell
                      item 9 z=0
                        line at=174.00,197.98 size=19.46x11.50 baseline=9.05 role=Paragraph
                          run "0.00" at=174.00,207.03 advance=19.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=19.46
                              cluster 0 u+0030 x=174.00 advance=5.56 kerning=0.00
                              cluster 1 u+002E x=179.56 advance=2.78 kerning=0.00
                              cluster 2 u+0030 x=182.34 advance=5.56 kerning=0.00
                              cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                  row 2 header=False y=209.47 height=11.50 cells=2
                    cell 2,0 span=1x1 at=24.00,209.47 size=150.00x11.50 opacity=1.00 role=TableCell
                      item 10 z=0
                        line at=24.00,209.47 size=27.79x11.50 baseline=9.05 role=Paragraph
                          run "Item 1" at=24.00,218.53 advance=27.79 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=27.79
                              cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                              cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                              cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                              cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                              cluster 4 u+0020 x=43.45 advance=2.78 kerning=0.00
                              cluster 5 u+0031 x=46.23 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                    cell 2,1 span=1x1 at=174.00,209.47 size=110.00x11.50 opacity=1.00 role=TableCell
                      item 11 z=0
                        line at=174.00,209.47 size=19.46x11.50 baseline=9.05 role=Paragraph
                          run "7.00" at=174.00,218.53 advance=19.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=19.46
                              cluster 0 u+0037 x=174.00 advance=5.56 kerning=0.00
                              cluster 1 u+002E x=179.56 advance=2.78 kerning=0.00
                              cluster 2 u+0030 x=182.34 advance=5.56 kerning=0.00
                              cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                  row 3 header=False y=220.97 height=11.50 cells=2
                    cell 3,0 span=1x1 at=24.00,220.97 size=150.00x11.50 opacity=1.00 role=TableCell
                      item 12 z=0
                        line at=24.00,220.97 size=27.79x11.50 baseline=9.05 role=Paragraph
                          run "Item 2" at=24.00,230.03 advance=27.79 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=27.79
                              cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                              cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                              cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                              cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                              cluster 4 u+0020 x=43.45 advance=2.78 kerning=0.00
                              cluster 5 u+0032 x=46.23 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                    cell 3,1 span=1x1 at=174.00,220.97 size=110.00x11.50 opacity=1.00 role=TableCell
                      item 13 z=0
                        line at=174.00,220.97 size=25.02x11.50 baseline=9.05 role=Paragraph
                          run "14.00" at=174.00,230.03 advance=25.02 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=25.02
                              cluster 0 u+0031 x=174.00 advance=5.56 kerning=0.00
                              cluster 1 u+0034 x=179.56 advance=5.56 kerning=0.00
                              cluster 2 u+002E x=185.12 advance=2.78 kerning=0.00
                              cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                              cluster 4 u+0030 x=193.46 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                  row 4 header=False y=232.47 height=11.50 cells=2
                    cell 4,0 span=1x1 at=24.00,232.47 size=150.00x11.50 opacity=1.00 role=TableCell
                      item 14 z=0
                        line at=24.00,232.47 size=27.79x11.50 baseline=9.05 role=Paragraph
                          run "Item 3" at=24.00,241.53 advance=27.79 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=27.79
                              cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                              cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                              cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                              cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                              cluster 4 u+0020 x=43.45 advance=2.78 kerning=0.00
                              cluster 5 u+0033 x=46.23 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                    cell 4,1 span=1x1 at=174.00,232.47 size=110.00x11.50 opacity=1.00 role=TableCell
                      item 15 z=0
                        line at=174.00,232.47 size=25.02x11.50 baseline=9.05 role=Paragraph
                          run "21.00" at=174.00,241.53 advance=25.02 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=25.02
                              cluster 0 u+0032 x=174.00 advance=5.56 kerning=0.00
                              cluster 1 u+0031 x=179.56 advance=5.56 kerning=0.00
                              cluster 2 u+002E x=185.12 advance=2.78 kerning=0.00
                              cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                              cluster 4 u+0030 x=193.46 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                  row 5 header=False y=243.97 height=11.50 cells=2
                    cell 5,0 span=1x1 at=24.00,243.97 size=150.00x11.50 opacity=1.00 role=TableCell
                      item 16 z=0
                        line at=24.00,243.97 size=27.79x11.50 baseline=9.05 role=Paragraph
                          run "Item 4" at=24.00,253.02 advance=27.79 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=27.79
                              cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                              cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                              cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                              cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                              cluster 4 u+0020 x=43.45 advance=2.78 kerning=0.00
                              cluster 5 u+0034 x=46.23 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                    cell 5,1 span=1x1 at=174.00,243.97 size=110.00x11.50 opacity=1.00 role=TableCell
                      item 17 z=0
                        line at=174.00,243.97 size=25.02x11.50 baseline=9.05 role=Paragraph
                          run "28.00" at=174.00,253.02 advance=25.02 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=25.02
                              cluster 0 u+0032 x=174.00 advance=5.56 kerning=0.00
                              cluster 1 u+0038 x=179.56 advance=5.56 kerning=0.00
                              cluster 2 u+002E x=185.12 advance=2.78 kerning=0.00
                              cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                              cluster 4 u+0030 x=193.46 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                  row 6 header=False y=255.47 height=11.50 cells=2
                    cell 6,0 span=1x1 at=24.00,255.47 size=150.00x11.50 opacity=1.00 role=TableCell
                      item 18 z=0
                        line at=24.00,255.47 size=27.79x11.50 baseline=9.05 role=Paragraph
                          run "Item 5" at=24.00,264.52 advance=27.79 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=27.79
                              cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                              cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                              cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                              cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                              cluster 4 u+0020 x=43.45 advance=2.78 kerning=0.00
                              cluster 5 u+0035 x=46.23 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                    cell 6,1 span=1x1 at=174.00,255.47 size=110.00x11.50 opacity=1.00 role=TableCell
                      item 19 z=0
                        line at=174.00,255.47 size=25.02x11.50 baseline=9.05 role=Paragraph
                          run "35.00" at=174.00,264.52 advance=25.02 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=25.02
                              cluster 0 u+0033 x=174.00 advance=5.56 kerning=0.00
                              cluster 1 u+0035 x=179.56 advance=5.56 kerning=0.00
                              cluster 2 u+002E x=185.12 advance=2.78 kerning=0.00
                              cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                              cluster 4 u+0030 x=193.46 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                /tableFragment
              /item
              item 20 z=14
                image media=image/jpeg at=24.00,138.48 size=48.00x48.00 opacity=1.00 interpolate=False role=Figure
              /item
            /layer Body
            layer Header top=35.43
              item 21 z=0
                line at=24.00,35.43 size=37.51x10.35 baseline=8.15 artifact=Pagination
                  run "HEADER" at=24.00,43.58 advance=37.51 family=Liberation Sans size=9.00 style=regular color=#000000FF opacity=1.00 artifact=Pagination
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=37.51
                      cluster 0 u+0048 x=24.00 advance=6.50 kerning=0.00
                      cluster 1 u+0045 x=30.50 advance=6.00 kerning=0.00
                      cluster 2 u+0041 x=36.50 advance=6.00 kerning=0.00
                      cluster 3 u+0044 x=42.51 advance=6.50 kerning=0.00
                      cluster 4 u+0045 x=49.00 advance=6.00 kerning=0.00
                      cluster 5 u+0052 x=55.01 advance=6.50 kerning=0.00
                    /span
                  /run
                /line
              /item
            /layer Header
            layer Footer top=274.22
              item 22 z=0
                line at=24.00,274.22 size=37.50x10.35 baseline=8.15 artifact=Pagination
                  run "FOOTER" at=24.00,282.37 advance=37.50 family=Liberation Sans size=9.00 style=regular color=#000000FF opacity=1.00 artifact=Pagination
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=37.50
                      cluster 0 u+0046 x=24.00 advance=5.50 kerning=0.00
                      cluster 1 u+004F x=29.50 advance=7.00 kerning=0.00
                      cluster 2 u+004F x=36.50 advance=7.00 kerning=0.00
                      cluster 3 u+0054 x=43.50 advance=5.50 kerning=0.00
                      cluster 4 u+0045 x=49.00 advance=6.00 kerning=0.00
                      cluster 5 u+0052 x=55.00 advance=6.50 kerning=0.00
                    /span
                  /run
                /line
              /item
            /layer Footer
            watermark center=180.00,160.00 rotation=30.00 opacity=0.25
              watermarkText "DRAFT" family=Helvetica size=72.00 color=#000000FF at=-119.99,25.20 alpha=-
                span face=sans-serif bold=False italic=False ascent=718.00 descent=-207.00 upem=1000.00 x=-119.99 advance=239.98
                  cluster 0 u+0044 x=-119.99 advance=51.98 kerning=0.00
                  cluster 1 u+0052 x=-68.00 advance=51.98 kerning=0.00
                  cluster 2 u+0041 x=-16.02 advance=48.02 kerning=0.00
                  cluster 3 u+0046 x=32.00 advance=43.99 kerning=0.00
                  cluster 4 u+0054 x=76.00 advance=43.99 kerning=0.00
                /span
              /watermarkText
            /watermark
          /page
          page 2 size=360.00x320.00 content=24.00,45.78,312.00,228.44
            layer Body top=45.78
              item 0 z=16
                line at=24.00,75.68 size=50.03x11.50 baseline=9.05 role=Paragraph
                  run "Back to top" at=24.00,84.73 advance=50.03 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 linkAnchor=top role=Link
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=50.03
                      cluster 0 u+0042 x=24.00 advance=6.67 kerning=0.00
                      cluster 1 u+0061 x=30.67 advance=5.56 kerning=0.00
                      cluster 2 u+0063 x=36.23 advance=5.00 kerning=0.00
                      cluster 3 u+006B x=41.23 advance=5.00 kerning=0.00
                      cluster 4 u+0020 x=46.23 advance=2.78 kerning=0.00
                      cluster 5 u+0074 x=49.01 advance=2.78 kerning=0.00
                      cluster 6 u+006F x=51.79 advance=5.56 kerning=0.00
                      cluster 7 u+0020 x=57.35 advance=2.78 kerning=0.00
                      cluster 8 u+0074 x=60.13 advance=2.78 kerning=0.00
                      cluster 9 u+006F x=62.91 advance=5.56 kerning=0.00
                      cluster 10 u+0070 x=68.47 advance=5.56 kerning=0.00
                    /span
                  /run
                /line
              /item
              item 1 z=15
                tableFragment at=24.00,45.78 size=260.00x29.90 rows=2
                  row 0 header=True y=45.78 height=11.50 cells=2
                    cell 0,0 span=1x1 at=24.00,45.78 size=150.00x11.50 opacity=1.00 role=TableCell
                      item 2 z=0
                        line at=24.00,45.78 size=34.46x11.50 baseline=9.05 role=Paragraph
                          run "Product" at=24.00,54.83 advance=34.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=34.46
                              cluster 0 u+0050 x=24.00 advance=6.67 kerning=0.00
                              cluster 1 u+0072 x=30.67 advance=3.33 kerning=0.00
                              cluster 2 u+006F x=34.00 advance=5.56 kerning=0.00
                              cluster 3 u+0064 x=39.56 advance=5.56 kerning=0.00
                              cluster 4 u+0075 x=45.12 advance=5.56 kerning=0.00
                              cluster 5 u+0063 x=50.68 advance=5.00 kerning=0.00
                              cluster 6 u+0074 x=55.68 advance=2.78 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                    cell 0,1 span=1x1 at=174.00,45.78 size=110.00x11.50 opacity=1.00 role=TableCell
                      item 3 z=0
                        line at=174.00,45.78 size=34.46x11.50 baseline=9.05 role=Paragraph
                          run "Amount" at=174.00,54.83 advance=34.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=34.46
                              cluster 0 u+0041 x=174.00 advance=6.67 kerning=0.00
                              cluster 1 u+006D x=180.67 advance=8.33 kerning=0.00
                              cluster 2 u+006F x=189.00 advance=5.56 kerning=0.00
                              cluster 3 u+0075 x=194.56 advance=5.56 kerning=0.00
                              cluster 4 u+006E x=200.12 advance=5.56 kerning=0.00
                              cluster 5 u+0074 x=205.68 advance=2.78 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                  row 7 header=False y=57.28 height=18.40 cells=2
                    cell 7,0 span=1x1 at=24.00,57.28 size=150.00x18.40 opacity=1.00 role=TableCell
                      item 4 z=0
                        table at=24.00,57.28 width=120.00
                          row 0 header=False y=57.28 height=9.20 cells=2
                            cell 0,0 span=1x1 at=24.00,57.28 size=60.00x9.20 opacity=1.00 role=TableCell
                              item 5 z=0
                                line at=24.00,57.28 size=14.67x9.20 baseline=7.24 role=Paragraph
                                  run "Part" at=24.00,64.52 advance=14.67 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=14.67
                                      cluster 0 u+0050 x=24.00 advance=5.34 kerning=0.00
                                      cluster 1 u+0061 x=29.34 advance=4.45 kerning=0.00
                                      cluster 2 u+0072 x=33.79 advance=2.66 kerning=0.00
                                      cluster 3 u+0074 x=36.45 advance=2.22 kerning=0.00
                                    /span
                                  /run
                                /line
                              /item
                            /cell
                            cell 0,1 span=1x1 at=84.00,57.28 size=60.00x9.20 opacity=1.00 role=TableCell
                              item 6 z=0
                                line at=84.00,57.28 size=12.45x9.20 baseline=7.24 role=Paragraph
                                  run "Qty" at=84.00,64.52 advance=12.45 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=84.00 advance=12.45
                                      cluster 0 u+0051 x=84.00 advance=6.22 kerning=0.00
                                      cluster 1 u+0074 x=90.22 advance=2.22 kerning=0.00
                                      cluster 2 u+0079 x=92.45 advance=4.00 kerning=0.00
                                    /span
                                  /run
                                /line
                              /item
                            /cell
                          /row
                          row 1 header=False y=66.48 height=9.20 cells=2
                            cell 1,0 span=1x1 at=24.00,66.48 size=60.00x9.20 opacity=1.00 role=TableCell
                              item 7 z=0
                                line at=24.00,66.48 size=13.79x9.20 baseline=7.24 role=Paragraph
                                  run "Bolt" at=24.00,73.72 advance=13.79 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=13.79
                                      cluster 0 u+0042 x=24.00 advance=5.34 kerning=0.00
                                      cluster 1 u+006F x=29.34 advance=4.45 kerning=0.00
                                      cluster 2 u+006C x=33.79 advance=1.78 kerning=0.00
                                      cluster 3 u+0074 x=35.56 advance=2.22 kerning=0.00
                                    /span
                                  /run
                                /line
                              /item
                            /cell
                            cell 1,1 span=1x1 at=84.00,66.48 size=60.00x9.20 opacity=1.00 role=TableCell
                              item 8 z=0
                                line at=84.00,66.48 size=8.90x9.20 baseline=7.24 role=Paragraph
                                  run "12" at=84.00,73.72 advance=8.90 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=84.00 advance=8.90
                                      cluster 0 u+0031 x=84.00 advance=4.45 kerning=0.00
                                      cluster 1 u+0032 x=88.45 advance=4.45 kerning=0.00
                                    /span
                                  /run
                                /line
                              /item
                            /cell
                          /row
                        /table
                      /item
                    /cell
                    cell 7,1 span=1x1 at=174.00,57.28 size=110.00x18.40 opacity=1.00 role=TableCell
                      item 9 z=0
                        line at=174.00,57.28 size=31.68x11.50 baseline=9.05 role=Paragraph
                          run "Nested" at=174.00,66.33 advance=31.68 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                            span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=31.68
                              cluster 0 u+004E x=174.00 advance=7.22 kerning=0.00
                              cluster 1 u+0065 x=181.22 advance=5.56 kerning=0.00
                              cluster 2 u+0073 x=186.78 advance=5.00 kerning=0.00
                              cluster 3 u+0074 x=191.78 advance=2.78 kerning=0.00
                              cluster 4 u+0065 x=194.56 advance=5.56 kerning=0.00
                              cluster 5 u+0064 x=200.12 advance=5.56 kerning=0.00
                            /span
                          /run
                        /line
                      /item
                    /cell
                  /row
                /tableFragment
              /item
            /layer Body
            layer Header top=35.43
              item 10 z=0
                line at=24.00,35.43 size=37.51x10.35 baseline=8.15 artifact=Pagination
                  run "HEADER" at=24.00,43.58 advance=37.51 family=Liberation Sans size=9.00 style=regular color=#000000FF opacity=1.00 artifact=Pagination
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=37.51
                      cluster 0 u+0048 x=24.00 advance=6.50 kerning=0.00
                      cluster 1 u+0045 x=30.50 advance=6.00 kerning=0.00
                      cluster 2 u+0041 x=36.50 advance=6.00 kerning=0.00
                      cluster 3 u+0044 x=42.51 advance=6.50 kerning=0.00
                      cluster 4 u+0045 x=49.00 advance=6.00 kerning=0.00
                      cluster 5 u+0052 x=55.01 advance=6.50 kerning=0.00
                    /span
                  /run
                /line
              /item
            /layer Header
            layer Footer top=274.22
              item 11 z=0
                line at=24.00,274.22 size=37.50x10.35 baseline=8.15 artifact=Pagination
                  run "FOOTER" at=24.00,282.37 advance=37.50 family=Liberation Sans size=9.00 style=regular color=#000000FF opacity=1.00 artifact=Pagination
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=37.50
                      cluster 0 u+0046 x=24.00 advance=5.50 kerning=0.00
                      cluster 1 u+004F x=29.50 advance=7.00 kerning=0.00
                      cluster 2 u+004F x=36.50 advance=7.00 kerning=0.00
                      cluster 3 u+0054 x=43.50 advance=5.50 kerning=0.00
                      cluster 4 u+0045 x=49.00 advance=6.00 kerning=0.00
                      cluster 5 u+0052 x=55.00 advance=6.50 kerning=0.00
                    /span
                  /run
                /line
              /item
            /layer Footer
            link rect=24.00,75.68,74.03,86.85 uri=- anchor=top role=Link
            watermark center=180.00,160.00 rotation=30.00 opacity=0.25
              watermarkText "DRAFT" family=Helvetica size=72.00 color=#000000FF at=-119.99,25.20 alpha=-
                span face=sans-serif bold=False italic=False ascent=718.00 descent=-207.00 upem=1000.00 x=-119.99 advance=239.98
                  cluster 0 u+0044 x=-119.99 advance=51.98 kerning=0.00
                  cluster 1 u+0052 x=-68.00 advance=51.98 kerning=0.00
                  cluster 2 u+0041 x=-16.02 advance=48.02 kerning=0.00
                  cluster 3 u+0046 x=32.00 advance=43.99 kerning=0.00
                  cluster 4 u+0054 x=76.00 advance=43.99 kerning=0.00
                /span
              /watermarkText
            /watermark
          /page
        /scene
        """;
}
