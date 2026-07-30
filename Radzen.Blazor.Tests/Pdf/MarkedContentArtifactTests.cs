#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class MarkedContentArtifactTests
{
    private static ContentCollection Materialize(string rawStream)
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(rawStream));

        var reloaded = InterpreterTestSupport.Load(document.ToArray());
        return reloaded.Pages[0].Content;
    }

    [Fact]
    public void StructureTag_DoesNotMarkContentAsArtifact()
    {
        var content = Materialize(
            "/P <</MCID 0>> BDC\n" +
            "BT /F0 12 Tf 10 700 Td (Hi) Tj ET\n" +
            "EMC\n");

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal("Hi", text.Text);
        Assert.False(text.IsArtifact);
    }

    [Fact]
    public void ArtifactTag_MarksContentAsArtifact()
    {
        var content = Materialize(
            "/Artifact BDC\n" +
            "BT /F0 12 Tf 10 700 Td (Deco) Tj ET\n" +
            "EMC\n");

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal("Deco", text.Text);
        Assert.True(text.IsArtifact);
    }

    [Fact]
    public void NestedTags_TrackArtifactPerLevelAndStayBalanced()
    {
        var content = Materialize(
            "/P <</MCID 0>> BDC\n" +
            "/Artifact BDC\n" +
            "BT /F0 12 Tf 10 700 Td (Inside) Tj ET\n" +
            "EMC\n" +
            "BT /F0 12 Tf 10 680 Td (Outside) Tj ET\n" +
            "EMC\n");

        var inside = Assert.IsType<TextContent>(content[0]);
        Assert.Equal("Inside", inside.Text);
        Assert.True(inside.IsArtifact);

        var outside = Assert.IsType<TextContent>(content[1]);
        Assert.Equal("Outside", outside.Text);
        Assert.False(outside.IsArtifact);
    }
}
