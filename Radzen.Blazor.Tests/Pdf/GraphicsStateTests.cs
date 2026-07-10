#nullable enable

using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// C4a: graphics-state folding. A foreign content stream using q/Q save-restore,
// nested cm concatenation and Td/Tm text positioning materializes into elements
// whose Transform is the ABSOLUTE composed matrix at the point they are painted.
public class GraphicsStateTests
{
    private static ContentCollection Materialize(string rawStream)
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(rawStream));

        var reloaded = InterpreterTestSupport.Load(document.ToArray());
        return reloaded.Pages[0].Content;
    }

    private const string Nested =
        "q\n" +
        "2 0 0 2 10 20 cm\n" +
        "q\n" +
        "1 0 0 1 5 5 cm\n" +
        "BT\n" +
        "/F0 12 Tf\n" +
        "3 4 Td\n" +
        "(A) Tj\n" +
        "ET\n" +
        "Q\n" +
        "0 0 m\n" +
        "100 0 l\n" +
        "S\n" +
        "Q\n";

    [Fact]
    public void Nested_ProducesTextThenPath()
    {
        var content = Materialize(Nested);

        Assert.Equal(2, content.Count);
        Assert.IsType<TextContent>(content[0]);
        Assert.IsType<PathContent>(content[1]);
    }

    [Fact]
    public void Nested_Text_ComposesBothCmAndTd()
    {
        var content = Materialize(Nested);

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal("A", text.Text);

        // Td(3,4) then inner cm(1,0,0,1,5,5) then outer cm(2,0,0,2,10,20).
        InterpreterTestSupport.AssertMatrix(2, 0, 0, 2, 26, 38, text.Transform);
    }

    [Fact]
    public void Nested_Path_UsesOuterCtmOnlyAfterInnerRestore()
    {
        var content = Materialize(Nested);

        var path = Assert.IsType<PathContent>(content[1]);
        Assert.True(path.Stroke);

        // Inner q..Q is restored, so the path sees only the outer cm(2,0,0,2,10,20).
        InterpreterTestSupport.AssertMatrix(2, 0, 0, 2, 10, 20, path.Transform);
    }

    [Fact]
    public void SingleCm_TranslatesElementTransform()
    {
        var content = Materialize(
            "1 0 0 1 40 60 cm\n" +
            "10 10 m\n" +
            "20 20 l\n" +
            "S\n");

        var path = Assert.IsType<PathContent>(content[0]);
        InterpreterTestSupport.AssertMatrix(Matrix.Translate(40, 60), path.Transform);
    }

    [Fact]
    public void Tm_SetsAbsoluteTextMatrixComposedWithCtm()
    {
        var content = Materialize(
            "q\n" +
            "1 0 0 1 100 100 cm\n" +
            "BT\n" +
            "/F0 10 Tf\n" +
            "2 0 0 2 5 7 Tm\n" +
            "(B) Tj\n" +
            "ET\n" +
            "Q\n");

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal("B", text.Text);

        // Tm(2,0,0,2,5,7) then cm Translate(100,100) => (2,0,0,2,105,107).
        InterpreterTestSupport.AssertMatrix(2, 0, 0, 2, 105, 107, text.Transform);
    }

    [Fact]
    public void QRestore_IsolatesTransformBetweenSiblings()
    {
        var content = Materialize(
            "q 1 0 0 1 10 0 cm 0 0 m 1 1 l S Q\n" +
            "q 1 0 0 1 0 20 cm 0 0 m 1 1 l S Q\n");

        Assert.Equal(2, content.Count);
        InterpreterTestSupport.AssertMatrix(Matrix.Translate(10, 0), content[0].Transform);
        InterpreterTestSupport.AssertMatrix(Matrix.Translate(0, 20), content[1].Transform);
    }

    [Fact]
    public void NoTransform_YieldsIdentity()
    {
        var content = Materialize("0 0 m 5 5 l S\n");

        InterpreterTestSupport.AssertMatrix(Matrix.Identity, content[0].Transform);
    }
}
