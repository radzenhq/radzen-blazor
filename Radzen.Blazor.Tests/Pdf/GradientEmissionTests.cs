#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class GradientEmissionTests
{
    [Fact]
    public void GradientBoxBackground_EmitsPatternFill_AndShadingResource()
    {
        var document = new Radzen.Documents.Document();
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

        var content = FeatureEmissionTestHelpers.Content(document);
        Assert.Contains("/Pattern cs", content, StringComparison.Ordinal);
        Assert.Contains("scn", content, StringComparison.Ordinal);

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
        var document = new Document();
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
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, 0));
        Assert.Contains("/Pattern cs", content, StringComparison.Ordinal);
        Assert.Contains("scn", content, StringComparison.Ordinal);

        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var patterns = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Pattern"]!));
        var pattern = Assert.IsType<DictionaryObject>(reader.Resolve(patterns[Assert.Single(patterns.Keys)]));
        var shading = Assert.IsType<DictionaryObject>(reader.Resolve(pattern["Shading"]!));
        Assert.Equal(3, ((NumberObject)shading["ShadingType"]).IntValue);
    }
}
