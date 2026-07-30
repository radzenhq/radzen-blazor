#nullable enable
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class BuildRepeatabilityTests
{
    [Fact]
    public void PlainDocument_ProducesRepeatableBuilds()
    {
        static byte[] Build()
        {
            var document = new Document();
            var section = document.Sections.Add();
            section.Blocks.Add(FeatureEmissionTestHelpers.Text("Plain paragraph one."));
            var list = section.Blocks.AddList(ListStyle.Number);
            list.AddItem("Alpha");
            list.AddItem("Beta");
            return new DocumentRenderer().ToArray(document);
        }

        Assert.Equal(Build(), Build());
    }
}
