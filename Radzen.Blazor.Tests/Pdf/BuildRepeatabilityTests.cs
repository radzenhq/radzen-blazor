#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class BuildRepeatabilityTests
{
    [Fact]
    public void PlainDocument_ProducesRepeatableBuilds()
    {
        static byte[] Build()
        {
            var builder = new DocumentBuilder();
            var section = builder.Sections.Add();
            section.Blocks.Add(FeatureEmissionTestHelpers.Text("Plain paragraph one."));
            var list = section.Blocks.AddList(ListStyle.Number);
            list.AddItem("Alpha");
            list.AddItem("Beta");
            return builder.ToArray();
        }

        Assert.Equal(Build(), Build());
    }
}
