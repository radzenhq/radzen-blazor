#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class BlendModeTests
{
    [Fact]
    public void BlendModeBox_EmitsBmInExtGState()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(200, 200, 200),
            BlendMode = BlendMode.Multiply,
        });
        container.Blocks.Add(FeatureEmissionTestHelpers.Text("Blended"));

        var reader = BuildTestSupport.Read(builder);
        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var states = Assert.IsType<DictionaryObject>(reader.Resolve(resources["ExtGState"]!));

        var foundBlend = false;
        foreach (var key in states.Keys)
        {
            if (reader.Resolve(states[key]) is DictionaryObject state
                && state.TryGetValue("BM", out var bm)
                && reader.Resolve(bm!) is NameObject name && name.Value == "Multiply")
            {
                foundBlend = true;
            }
        }

        Assert.True(foundBlend, "a box ExtGState must carry /BM /Multiply");
    }
}
