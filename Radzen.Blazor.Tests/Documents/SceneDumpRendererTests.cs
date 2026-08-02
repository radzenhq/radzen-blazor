#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System;
using Radzen.Blazor.Pdf.Tests;
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

    internal static Document Everything()
    {
        var document = new Document { Language = "en-US" };
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = Latin;

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

        var styled = section.Blocks.AddParagraph();
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

        var linked = section.Blocks.AddParagraph();
        var link = linked.Inlines.Add("Radzen site");
        link.Font.Family = Latin;
        link.Link = "https://www.radzen.com/";

        var inline = section.Blocks.AddParagraph();
        inline.Inlines.Add("Inline ").Font.Family = Latin;
        inline.Inlines.AddImage(PdfTestResources.Open("Images/rgb.png"));

        var form = section.Blocks.AddParagraph();
        form.Inlines.Add("Name: ").Font.Family = Latin;
        form.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Full name" });

        var list = section.Blocks.AddList();
        list.AddItem("First item");
        list.AddItem("Second item");

        var clipped = section.Blocks.Add(new Container
        {
            Width = Unit.FromPoint(26),
            Padding = Unit.FromPoint(4),
            Background = Color.FromRgb(240, 240, 250),
            CornerRadius = Unit.FromPoint(4),
        });
        clipped.Blocks.Add(Text("WW", 24));
        clipped.Blocks.AddImage(PdfTestResources.Open("Images/rgb.png"));

        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        var table = section.Blocks.AddTable();
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
        var nested = nesting.Cells[0].Blocks.AddTable();
        nested.Columns.Add(Unit.FromPoint(60));
        nested.Columns.Add(Unit.FromPoint(60));
        var nestedHeader = nested.Rows.Add();
        nestedHeader.Cells[0].Blocks.Add(Text("Part", 8));
        nestedHeader.Cells[1].Blocks.Add(Text("Qty", 8));
        var nestedRow = nested.Rows.Add();
        nestedRow.Cells[0].Blocks.Add(Text("Bolt", 8));
        nestedRow.Cells[1].Blocks.Add(Text("12", 8));
        nesting.Cells[1].Blocks.Add(Text("Nested", 10));

        var back = section.Blocks.AddParagraph();
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

    [Fact]
    public void SceneDumpRenderer_TouchesNoPdfType()
    {
        var offenders = PdfTypesReachedBy(typeof(SceneDumpRenderer))
            .Select(type => type.FullName!)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void SceneFontView_HandsOutNoFontProgram()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var offenders = typeof(RegisteredFace).GetProperties(All)
            .Where(property => property.GetMethod is { IsPrivate: false })
            .Select(property => (property.Name, Type: property.PropertyType))
            .Concat(typeof(RegisteredFace).GetFields(All)
                .Where(field => !field.IsPrivate)
                .Select(field => (field.Name, Type: field.FieldType)))
            .Where(member => member.Type == typeof(SfntFont) || member.Type == typeof(FontSourceData))
            .Select(member => member.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
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

    [Fact]
    public void SceneDumpRenderer_IsolationCheck_DetectsAPdfTouch()
        => Assert.NotEmpty(PdfTypesReachedBy(typeof(PdfTouchingProbe)));

    private sealed class PdfTouchingProbe
    {
        public static string Name() => nameof(DocumentRenderer);

        public static object Touch() => new DocumentRenderer();
    }

    private static IEnumerable<Type> PdfTypesReachedBy(Type type)
    {
        foreach (var used in UsedTypes(type))
        {
            if (used.FullName is { } name && name.StartsWith("Radzen.Documents.Pdf", StringComparison.Ordinal))
            {
                yield return used;
            }
        }
    }

    private static IEnumerable<Type> UsedTypes(Type type)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var field in type.GetFields(All))
        {
            yield return field.FieldType;
        }

        foreach (var method in type.GetMethods(All).Cast<MethodBase>().Concat(type.GetConstructors(All)))
        {
            if (method is MethodInfo { ReturnType: { } returnType })
            {
                yield return returnType;
            }

            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }

            var body = method.GetMethodBody();
            if (body is null)
            {
                continue;
            }

            foreach (var local in body.LocalVariables)
            {
                yield return local.LocalType;
            }

            foreach (var referenced in Referenced(method, body))
            {
                yield return referenced;
            }
        }
    }

    private static readonly Dictionary<short, OpCode> Opcodes = BuildOpcodes();

    private static Dictionary<short, OpCode> BuildOpcodes()
    {
        var table = new Dictionary<short, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opcode)
            {
                table[opcode.Value] = opcode;
            }
        }

        return table;
    }

    private static IEnumerable<Type> Referenced(MethodBase method, MethodBody body)
    {
        var il = body.GetILAsByteArray();
        if (il is null)
        {
            yield break;
        }

        var module = method.Module;
        var typeArguments = method.DeclaringType?.IsGenericType == true
            ? method.DeclaringType.GetGenericArguments()
            : null;
        var methodArguments = method.IsGenericMethodDefinition ? method.GetGenericArguments() : null;

        var i = 0;
        while (i < il.Length)
        {
            short code = il[i++];
            if (code == 0xFE)
            {
                code = (short)(0xFE00 | il[i++]);
            }

            if (!Opcodes.TryGetValue(code, out var opcode))
            {
                yield break;
            }

            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    i += 1;
                    break;
                case OperandType.InlineVar:
                    i += 2;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    i += 8;
                    break;
                case OperandType.InlineSwitch:
                    var count = BitConverter.ToInt32(il, i);
                    i += 4 + (4 * count);
                    break;
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    var token = BitConverter.ToInt32(il, i);
                    i += 4;
                    foreach (var resolved in Resolve(module, token, typeArguments, methodArguments))
                    {
                        yield return resolved;
                    }

                    break;
                default:
                    i += 4;
                    break;
            }
        }
    }

    private static IEnumerable<Type> Resolve(
        Module module,
        int token,
        Type[]? typeArguments,
        Type[]? methodArguments)
    {
        MemberInfo? member;
        try
        {
            member = module.ResolveMember(token, typeArguments, methodArguments);
        }
        catch (ArgumentException)
        {
            yield break;
        }

        if (member is Type resolvedType)
        {
            yield return resolvedType;
            yield break;
        }

        if (member?.DeclaringType is { } declaring)
        {
            yield return declaring;
        }

        switch (member)
        {
            case FieldInfo field:
                yield return field.FieldType;
                break;
            case MethodInfo signature:
                yield return signature.ReturnType;
                foreach (var parameter in signature.GetParameters())
                {
                    yield return parameter.ParameterType;
                }

                break;
            case ConstructorInfo constructor:
                foreach (var parameter in constructor.GetParameters())
                {
                    yield return parameter.ParameterType;
                }

                break;
        }
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
    }

    private const string ExpectedScene =
        """
        scene pages=2 language=en-US
          page 0 size=360.00x320.00 content=24.00,45.78,312.00,228.44
            layer Body top=45.78
              line at=24.00,45.78 size=106.73x18.40 baseline=14.48 role=Paragraph
                run "Scene" at=24.00,60.27 advance=45.37 family=Liberation Sans size=16.00 style=bold color=#000000FF opacity=1.00 anchor=top
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=45.37
                    cluster 0 u+0053 x=24.00 advance=10.67 kerning=0.00
                    cluster 1 u+0063 x=34.67 advance=8.00 kerning=0.00
                    cluster 2 u+0065 x=42.67 advance=8.90 kerning=0.00
                    cluster 3 u+006E x=51.57 advance=8.90 kerning=0.00
                    cluster 4 u+0065 x=60.47 advance=8.90 kerning=0.00
                  /span
                /run
                run "contract" at=73.81,60.27 advance=56.91 family=Liberation Sans size=16.00 style=bold color=#000000FF opacity=1.00 anchor=top
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=73.81 advance=56.91
                    cluster 0 u+0063 x=73.81 advance=8.00 kerning=0.00
                    cluster 1 u+006F x=81.81 advance=8.90 kerning=0.00
                    cluster 2 u+006E x=90.71 advance=8.90 kerning=0.00
                    cluster 3 u+0074 x=99.61 advance=4.45 kerning=0.00
                    cluster 4 u+0072 x=104.05 advance=5.33 kerning=0.00
                    cluster 5 u+0061 x=109.38 advance=8.90 kerning=0.00
                    cluster 6 u+0063 x=118.28 advance=8.00 kerning=0.00
                    cluster 7 u+0074 x=126.28 advance=4.45 kerning=0.00
                  /span
                /run
              /line
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
              line at=24.00,75.68 size=52.81x11.50 baseline=9.05 role=Paragraph
                run "Radzen" at=24.00,84.73 advance=34.47 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 link=https://www.radzen.com/ role=Link
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=34.47
                    cluster 0 u+0052 x=24.00 advance=7.22 kerning=0.00
                    cluster 1 u+0061 x=31.22 advance=5.56 kerning=0.00
                    cluster 2 u+0064 x=36.78 advance=5.56 kerning=0.00
                    cluster 3 u+007A x=42.34 advance=5.00 kerning=0.00
                    cluster 4 u+0065 x=47.34 advance=5.56 kerning=0.00
                    cluster 5 u+006E x=52.91 advance=5.56 kerning=0.00
                  /span
                /run
                run "site" at=61.25,84.73 advance=15.56 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 link=https://www.radzen.com/ role=Link
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=61.25 advance=15.56
                    cluster 0 u+0073 x=61.25 advance=5.00 kerning=0.00
                    cluster 1 u+0069 x=66.25 advance=2.22 kerning=0.00
                    cluster 2 u+0074 x=68.47 advance=2.78 kerning=0.00
                    cluster 3 u+0065 x=71.25 advance=5.56 kerning=0.00
                  /span
                /run
              /line
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
                /run
              /line
              line at=24.00,149.18 size=41.11x11.50 baseline=9.05 role=Paragraph
                run "•" at=24.00,158.23 advance=3.50 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 marker
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=3.50
                    cluster 0 u+2022 x=24.00 advance=3.50 kerning=0.00
                  /span
                /run
                run "First" at=42.00,158.23 advance=19.44 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=42.00 advance=19.44
                    cluster 0 u+0046 x=42.00 advance=6.11 kerning=0.00
                    cluster 1 u+0069 x=48.11 advance=2.22 kerning=0.00
                    cluster 2 u+0072 x=50.33 advance=3.33 kerning=0.00
                    cluster 3 u+0073 x=53.66 advance=5.00 kerning=0.00
                    cluster 4 u+0074 x=58.66 advance=2.78 kerning=0.00
                  /span
                /run
                run "item" at=64.22,158.23 advance=18.89 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=64.22 advance=18.89
                    cluster 0 u+0069 x=64.22 advance=2.22 kerning=0.00
                    cluster 1 u+0074 x=66.44 advance=2.78 kerning=0.00
                    cluster 2 u+0065 x=69.22 advance=5.56 kerning=0.00
                    cluster 3 u+006D x=74.78 advance=8.33 kerning=0.00
                  /span
                /run
              /line
              line at=24.00,160.68 size=55.59x11.50 baseline=9.05 role=Paragraph
                run "•" at=24.00,169.73 advance=3.50 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 marker
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=3.50
                    cluster 0 u+2022 x=24.00 advance=3.50 kerning=0.00
                  /span
                /run
                run "Second" at=42.00,169.73 advance=33.92 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=42.00 advance=33.92
                    cluster 0 u+0053 x=42.00 advance=6.67 kerning=0.00
                    cluster 1 u+0065 x=48.67 advance=5.56 kerning=0.00
                    cluster 2 u+0063 x=54.23 advance=5.00 kerning=0.00
                    cluster 3 u+006F x=59.23 advance=5.56 kerning=0.00
                    cluster 4 u+006E x=64.79 advance=5.56 kerning=0.00
                    cluster 5 u+0064 x=70.35 advance=5.56 kerning=0.00
                  /span
                /run
                run "item" at=78.69,169.73 advance=18.89 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=78.69 advance=18.89
                    cluster 0 u+0069 x=78.69 advance=2.22 kerning=0.00
                    cluster 1 u+0074 x=80.92 advance=2.78 kerning=0.00
                    cluster 2 u+0065 x=83.69 advance=5.56 kerning=0.00
                    cluster 3 u+006D x=89.26 advance=8.33 kerning=0.00
                  /span
                /run
              /line
              box at=24.00,172.18 size=26.00x81.20 opacity=1.00 transform=- fill=#F0F0FAFF radius=4.00 clip=24.00,172.18,26.00,81.20+lines
                line at=28.00,176.18 size=22.65x27.60 baseline=21.73 role=Paragraph
                  run "W" at=28.00,197.90 advance=22.65 family=Liberation Sans size=24.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=28.00 advance=22.65
                      cluster 0 u+0057 x=28.00 advance=22.65 kerning=0.00
                    /span
                  /run
                /line
                line at=28.00,203.77 size=22.65x27.60 baseline=21.73 role=Paragraph
                  run "W" at=28.00,225.50 advance=22.65 family=Liberation Sans size=24.00 style=regular color=#000000FF opacity=1.00
                    span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=28.00 advance=22.65
                      cluster 0 u+0057 x=28.00 advance=22.65 kerning=0.00
                    /span
                  /run
                /line
                image media=image/png at=28.00,231.37 size=18.00x18.00 opacity=1.00 interpolate=False role=Figure
              /box
            /layer Body
            layer Header top=35.43
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
            /layer Header
            layer Footer top=274.22
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
              line at=24.00,192.67 size=50.03x11.50 baseline=9.05 role=Paragraph
                run "Back" at=24.00,201.73 advance=22.23 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 linkAnchor=top role=Link
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=22.23
                    cluster 0 u+0042 x=24.00 advance=6.67 kerning=0.00
                    cluster 1 u+0061 x=30.67 advance=5.56 kerning=0.00
                    cluster 2 u+0063 x=36.23 advance=5.00 kerning=0.00
                    cluster 3 u+006B x=41.23 advance=5.00 kerning=0.00
                  /span
                /run
                run "to" at=49.01,201.73 advance=8.34 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 linkAnchor=top role=Link
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=49.01 advance=8.34
                    cluster 0 u+0074 x=49.01 advance=2.78 kerning=0.00
                    cluster 1 u+006F x=51.79 advance=5.56 kerning=0.00
                  /span
                /run
                run "top" at=60.13,201.73 advance=13.90 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00 linkAnchor=top role=Link
                  span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=60.13 advance=13.90
                    cluster 0 u+0074 x=60.13 advance=2.78 kerning=0.00
                    cluster 1 u+006F x=62.91 advance=5.56 kerning=0.00
                    cluster 2 u+0070 x=68.47 advance=5.56 kerning=0.00
                  /span
                /run
              /line
              tableFragment at=24.00,93.78 size=260.00x98.89 rows=8
                row 0 header=True y=93.78 height=11.50 background=- cells=2
                  cell 0,0 span=1x1 at=24.00,93.78 size=150.00x11.50 opacity=1.00 role=TableCell
                    line at=24.00,93.78 size=34.46x11.50 baseline=9.05 role=Paragraph
                      run "Product" at=24.00,102.83 advance=34.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
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
                  /cell
                  cell 0,1 span=1x1 at=174.00,93.78 size=110.00x11.50 opacity=1.00 role=TableCell
                    line at=174.00,93.78 size=34.46x11.50 baseline=9.05 role=Paragraph
                      run "Amount" at=174.00,102.83 advance=34.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
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
                  /cell
                /row
                row 1 header=False y=105.28 height=11.50 background=- cells=2
                  cell 1,0 span=1x1 at=24.00,105.28 size=150.00x11.50 opacity=1.00 role=TableCell
                    line at=24.00,105.28 size=27.79x11.50 baseline=9.05 role=Paragraph
                      run "Item" at=24.00,114.33 advance=19.45 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=19.45
                          cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                          cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                          cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                          cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                        /span
                      /run
                      run "0" at=46.23,114.33 advance=5.56 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=46.23 advance=5.56
                          cluster 0 u+0030 x=46.23 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                  cell 1,1 span=1x1 at=174.00,105.28 size=110.00x11.50 opacity=1.00 role=TableCell
                    line at=174.00,105.28 size=19.46x11.50 baseline=9.05 role=Paragraph
                      run "0.00" at=174.00,114.33 advance=19.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=19.46
                          cluster 0 u+0030 x=174.00 advance=5.56 kerning=0.00
                          cluster 1 u+002E x=179.56 advance=2.78 kerning=0.00
                          cluster 2 u+0030 x=182.34 advance=5.56 kerning=0.00
                          cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                /row
                row 2 header=False y=116.78 height=11.50 background=- cells=2
                  cell 2,0 span=1x1 at=24.00,116.78 size=150.00x11.50 opacity=1.00 role=TableCell
                    line at=24.00,116.78 size=27.79x11.50 baseline=9.05 role=Paragraph
                      run "Item" at=24.00,125.83 advance=19.45 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=19.45
                          cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                          cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                          cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                          cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                        /span
                      /run
                      run "1" at=46.23,125.83 advance=5.56 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=46.23 advance=5.56
                          cluster 0 u+0031 x=46.23 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                  cell 2,1 span=1x1 at=174.00,116.78 size=110.00x11.50 opacity=1.00 role=TableCell
                    line at=174.00,116.78 size=19.46x11.50 baseline=9.05 role=Paragraph
                      run "7.00" at=174.00,125.83 advance=19.46 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=19.46
                          cluster 0 u+0037 x=174.00 advance=5.56 kerning=0.00
                          cluster 1 u+002E x=179.56 advance=2.78 kerning=0.00
                          cluster 2 u+0030 x=182.34 advance=5.56 kerning=0.00
                          cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                /row
                row 3 header=False y=128.28 height=11.50 background=- cells=2
                  cell 3,0 span=1x1 at=24.00,128.28 size=150.00x11.50 opacity=1.00 role=TableCell
                    line at=24.00,128.28 size=27.79x11.50 baseline=9.05 role=Paragraph
                      run "Item" at=24.00,137.33 advance=19.45 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=19.45
                          cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                          cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                          cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                          cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                        /span
                      /run
                      run "2" at=46.23,137.33 advance=5.56 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=46.23 advance=5.56
                          cluster 0 u+0032 x=46.23 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                  cell 3,1 span=1x1 at=174.00,128.28 size=110.00x11.50 opacity=1.00 role=TableCell
                    line at=174.00,128.28 size=25.02x11.50 baseline=9.05 role=Paragraph
                      run "14.00" at=174.00,137.33 advance=25.02 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=25.02
                          cluster 0 u+0031 x=174.00 advance=5.56 kerning=0.00
                          cluster 1 u+0034 x=179.56 advance=5.56 kerning=0.00
                          cluster 2 u+002E x=185.12 advance=2.78 kerning=0.00
                          cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                          cluster 4 u+0030 x=193.46 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                /row
                row 4 header=False y=139.78 height=11.50 background=- cells=2
                  cell 4,0 span=1x1 at=24.00,139.78 size=150.00x11.50 opacity=1.00 role=TableCell
                    line at=24.00,139.78 size=27.79x11.50 baseline=9.05 role=Paragraph
                      run "Item" at=24.00,148.83 advance=19.45 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=19.45
                          cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                          cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                          cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                          cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                        /span
                      /run
                      run "3" at=46.23,148.83 advance=5.56 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=46.23 advance=5.56
                          cluster 0 u+0033 x=46.23 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                  cell 4,1 span=1x1 at=174.00,139.78 size=110.00x11.50 opacity=1.00 role=TableCell
                    line at=174.00,139.78 size=25.02x11.50 baseline=9.05 role=Paragraph
                      run "21.00" at=174.00,148.83 advance=25.02 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=25.02
                          cluster 0 u+0032 x=174.00 advance=5.56 kerning=0.00
                          cluster 1 u+0031 x=179.56 advance=5.56 kerning=0.00
                          cluster 2 u+002E x=185.12 advance=2.78 kerning=0.00
                          cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                          cluster 4 u+0030 x=193.46 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                /row
                row 5 header=False y=151.28 height=11.50 background=- cells=2
                  cell 5,0 span=1x1 at=24.00,151.28 size=150.00x11.50 opacity=1.00 role=TableCell
                    line at=24.00,151.28 size=27.79x11.50 baseline=9.05 role=Paragraph
                      run "Item" at=24.00,160.33 advance=19.45 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=19.45
                          cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                          cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                          cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                          cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                        /span
                      /run
                      run "4" at=46.23,160.33 advance=5.56 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=46.23 advance=5.56
                          cluster 0 u+0034 x=46.23 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                  cell 5,1 span=1x1 at=174.00,151.28 size=110.00x11.50 opacity=1.00 role=TableCell
                    line at=174.00,151.28 size=25.02x11.50 baseline=9.05 role=Paragraph
                      run "28.00" at=174.00,160.33 advance=25.02 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=25.02
                          cluster 0 u+0032 x=174.00 advance=5.56 kerning=0.00
                          cluster 1 u+0038 x=179.56 advance=5.56 kerning=0.00
                          cluster 2 u+002E x=185.12 advance=2.78 kerning=0.00
                          cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                          cluster 4 u+0030 x=193.46 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                /row
                row 6 header=False y=162.78 height=11.50 background=- cells=2
                  cell 6,0 span=1x1 at=24.00,162.78 size=150.00x11.50 opacity=1.00 role=TableCell
                    line at=24.00,162.78 size=27.79x11.50 baseline=9.05 role=Paragraph
                      run "Item" at=24.00,171.83 advance=19.45 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=19.45
                          cluster 0 u+0049 x=24.00 advance=2.78 kerning=0.00
                          cluster 1 u+0074 x=26.78 advance=2.78 kerning=0.00
                          cluster 2 u+0065 x=29.56 advance=5.56 kerning=0.00
                          cluster 3 u+006D x=35.12 advance=8.33 kerning=0.00
                        /span
                      /run
                      run "5" at=46.23,171.83 advance=5.56 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=46.23 advance=5.56
                          cluster 0 u+0035 x=46.23 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                  cell 6,1 span=1x1 at=174.00,162.78 size=110.00x11.50 opacity=1.00 role=TableCell
                    line at=174.00,162.78 size=25.02x11.50 baseline=9.05 role=Paragraph
                      run "35.00" at=174.00,171.83 advance=25.02 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
                        span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=174.00 advance=25.02
                          cluster 0 u+0033 x=174.00 advance=5.56 kerning=0.00
                          cluster 1 u+0035 x=179.56 advance=5.56 kerning=0.00
                          cluster 2 u+002E x=185.12 advance=2.78 kerning=0.00
                          cluster 3 u+0030 x=187.90 advance=5.56 kerning=0.00
                          cluster 4 u+0030 x=193.46 advance=5.56 kerning=0.00
                        /span
                      /run
                    /line
                  /cell
                /row
                row 7 header=False y=174.28 height=18.40 background=- cells=2
                  cell 7,0 span=1x1 at=24.00,174.28 size=150.00x18.40 opacity=1.00 role=TableCell
                    table at=24.00,174.28 width=120.00
                      row 0 header=False y=174.28 height=9.20 background=- cells=2
                        cell 0,0 span=1x1 at=24.00,174.28 size=60.00x9.20 opacity=1.00 role=TableCell
                          line at=24.00,174.28 size=14.67x9.20 baseline=7.24 role=Paragraph
                            run "Part" at=24.00,181.52 advance=14.67 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                              span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=14.67
                                cluster 0 u+0050 x=24.00 advance=5.34 kerning=0.00
                                cluster 1 u+0061 x=29.34 advance=4.45 kerning=0.00
                                cluster 2 u+0072 x=33.79 advance=2.66 kerning=0.00
                                cluster 3 u+0074 x=36.45 advance=2.22 kerning=0.00
                              /span
                            /run
                          /line
                        /cell
                        cell 0,1 span=1x1 at=84.00,174.28 size=60.00x9.20 opacity=1.00 role=TableCell
                          line at=84.00,174.28 size=12.45x9.20 baseline=7.24 role=Paragraph
                            run "Qty" at=84.00,181.52 advance=12.45 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                              span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=84.00 advance=12.45
                                cluster 0 u+0051 x=84.00 advance=6.22 kerning=0.00
                                cluster 1 u+0074 x=90.22 advance=2.22 kerning=0.00
                                cluster 2 u+0079 x=92.45 advance=4.00 kerning=0.00
                              /span
                            /run
                          /line
                        /cell
                      /row
                      row 1 header=False y=183.47 height=9.20 background=- cells=2
                        cell 1,0 span=1x1 at=24.00,183.47 size=60.00x9.20 opacity=1.00 role=TableCell
                          line at=24.00,183.47 size=13.79x9.20 baseline=7.24 role=Paragraph
                            run "Bolt" at=24.00,190.72 advance=13.79 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                              span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=24.00 advance=13.79
                                cluster 0 u+0042 x=24.00 advance=5.34 kerning=0.00
                                cluster 1 u+006F x=29.34 advance=4.45 kerning=0.00
                                cluster 2 u+006C x=33.79 advance=1.78 kerning=0.00
                                cluster 3 u+0074 x=35.56 advance=2.22 kerning=0.00
                              /span
                            /run
                          /line
                        /cell
                        cell 1,1 span=1x1 at=84.00,183.47 size=60.00x9.20 opacity=1.00 role=TableCell
                          line at=84.00,183.47 size=8.90x9.20 baseline=7.24 role=Paragraph
                            run "12" at=84.00,190.72 advance=8.90 family=Liberation Sans size=8.00 style=regular color=#000000FF opacity=1.00
                              span face=Liberation Sans bold=False italic=False ascent=1854.00 descent=-434.00 upem=2048.00 x=84.00 advance=8.90
                                cluster 0 u+0031 x=84.00 advance=4.45 kerning=0.00
                                cluster 1 u+0032 x=88.45 advance=4.45 kerning=0.00
                              /span
                            /run
                          /line
                        /cell
                      /row
                    /table
                  /cell
                  cell 7,1 span=1x1 at=174.00,174.28 size=110.00x18.40 opacity=1.00 role=TableCell
                    line at=174.00,174.28 size=31.68x11.50 baseline=9.05 role=Paragraph
                      run "Nested" at=174.00,183.33 advance=31.68 family=Liberation Sans size=10.00 style=regular color=#000000FF opacity=1.00
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
                  /cell
                /row
              /tableFragment
              image media=image/jpeg at=24.00,45.78 size=48.00x48.00 opacity=1.00 interpolate=False role=Figure
            /layer Body
            layer Header top=35.43
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
            /layer Header
            layer Footer top=274.22
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
            /layer Footer
            link rect=24.00,192.67,74.03,203.85 uri=- anchor=top role=Link
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
