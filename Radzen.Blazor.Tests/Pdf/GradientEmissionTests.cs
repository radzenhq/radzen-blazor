#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class GradientEmissionTests
{
    [Fact]
    public void GradientBoxBackground_EmitsPatternFill_AndShadingResource()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            BackgroundGradient = new LinearGradient(
                0, 0, 100, 0,
                new GradientStop(0, Color.FromRgb(255, 0, 0)),
                new GradientStop(1, Color.FromRgb(0, 0, 255))),
        });
        container.Blocks.Add(FeatureEmissionTestHelpers.Text("Boxed"));

        var content = FeatureEmissionTestHelpers.ContentBytes(document);
        Assert.Contains("Pattern", ContentOperationTestHelpers.ResourceNames(content, "cs"));
        Assert.Contains("scn", ContentOperationTestHelpers.Operators(content));

        var reader = BuildTestSupport.Read(document);
        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var patterns = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Pattern"]!));
        var pattern = Assert.IsType<DictionaryObject>(reader.Resolve(patterns[Assert.Single(patterns.Keys)]));
        Assert.Equal(2, ((NumberObject)pattern["PatternType"]).IntValue);
        var shading = Assert.IsType<DictionaryObject>(reader.Resolve(pattern["Shading"]!));
        Assert.Equal(2, ((NumberObject)shading["ShadingType"]).IntValue);
    }

    [Fact]
    public void PathGradientFill_EmitsPatternFill_AndShadingResource()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        var path = new PathContent
        {
            Fill = true,
            FillGradient = new RadialGradient(
                50, 50, 0, 50, 50, 40,
                new GradientStop(0, Color.FromRgb(0, 0, 0)),
                new GradientStop(1, Color.FromRgb(255, 255, 255))),
        };
        path.MoveTo(0, 0);
        path.LineTo(100, 0);
        path.LineTo(50, 100);
        path.Close();
        page.Content.Add(path);

        var reader = ContentTestHelpers.Reload(document);
        var content = ContentTestHelpers.PageContent(reader, 0);
        Assert.Contains("Pattern", ContentOperationTestHelpers.ResourceNames(content, "cs"));
        Assert.Contains("scn", ContentOperationTestHelpers.Operators(content));

        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var patterns = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Pattern"]!));
        var pattern = Assert.IsType<DictionaryObject>(reader.Resolve(patterns[Assert.Single(patterns.Keys)]));
        var shading = Assert.IsType<DictionaryObject>(reader.Resolve(pattern["Shading"]!));
        Assert.Equal(3, ((NumberObject)shading["ShadingType"]).IntValue);
    }

    [Fact]
    public void ContentWriter_SameBrush_RegistersOnePattern()
    {
        using var writer = new ContentWriter();
        var brush = new LinearGradient(0, 0, 10, 0,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        var first = writer.RegisterPattern(brush);
        var second = writer.RegisterPattern(brush);

        Assert.Equal(first, second);
        Assert.Single(writer.Patterns);
    }

    [Fact]
    public void ContentWriter_DistinctBrushes_RegisterSeparatePatterns()
    {
        using var writer = new ContentWriter();
        var a = new LinearGradient(0, 0, 10, 0, new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue));
        var b = new LinearGradient(0, 0, 10, 0, new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue));

        Assert.NotEqual(writer.RegisterPattern(a), writer.RegisterPattern(b));
        Assert.Equal(2, writer.Patterns.Count);
    }
}
