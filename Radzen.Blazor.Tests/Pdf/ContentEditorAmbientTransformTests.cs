#nullable enable

using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentEditorAmbientTransformTests
{
    [Fact]
    public void ModifiedTextUnderAmbientCm_KeepsItsTransform()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Content("q 2 0 0 2 0 0 cm BT /F1 12 Tf 10 10 Td (Hi) Tj ET Q"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        var run = loaded.Pages[0].Content.OfType<TextContent>().Single();
        var expected = run.Transform;
        run.Color = Color.Red;

        var reloaded = InterpreterTestSupport.SaveAndLoad(loaded);
        var actual = reloaded.Pages[0].Content.OfType<TextContent>().Single();

        InterpreterTestSupport.AssertMatrix(expected, actual.Transform);
    }

    [Fact]
    public void ModifiedPathUnderAmbientCm_KeepsItsTransform()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Content("q 3 0 0 3 50 20 cm 0 0 m 10 10 l S Q"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        var path = loaded.Pages[0].Content.OfType<PathContent>().Single();
        var expected = path.Transform;
        path.StrokeColor = Color.Red;

        var reloaded = InterpreterTestSupport.SaveAndLoad(loaded);
        var actual = reloaded.Pages[0].Content.OfType<PathContent>().Single();

        InterpreterTestSupport.AssertMatrix(expected, actual.Transform);
    }

    [Fact]
    public void ModifiedTextUnderRotatedCm_KeepsItsTransform()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Content("q 0 2 -3 0 300 100 cm BT /F1 10 Tf 10 20 Td (Turn) Tj ET Q"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        var run = loaded.Pages[0].Content.OfType<TextContent>().Single();
        var expected = run.Transform;
        run.Text = "Spun";

        var reloaded = InterpreterTestSupport.SaveAndLoad(loaded);
        var actual = reloaded.Pages[0].Content.OfType<TextContent>().Single();

        Assert.Equal("Spun", actual.Text);
        InterpreterTestSupport.AssertMatrix(expected, actual.Transform);
    }

    [Fact]
    public void ModifiedTextUnderIdentityCm_KeepsItsTransform()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Content("BT /F1 12 Tf 10 10 Td (Hi) Tj ET"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        var run = loaded.Pages[0].Content.OfType<TextContent>().Single();
        var expected = run.Transform;
        run.Color = Color.Red;

        var reloaded = InterpreterTestSupport.SaveAndLoad(loaded);
        var actual = reloaded.Pages[0].Content.OfType<TextContent>().Single();

        InterpreterTestSupport.AssertMatrix(expected, actual.Transform);
    }

    [Fact]
    public void ModifiedTextUnderMalformedCm_KeepsItsTransform()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Content("q 1 2 3 4 5 cm BT /F1 12 Tf 10 10 Td (Hi) Tj ET Q"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        var run = loaded.Pages[0].Content.OfType<TextContent>().Single();
        var expected = run.Transform;
        run.Color = Color.Red;

        var reloaded = InterpreterTestSupport.SaveAndLoad(loaded);
        var actual = reloaded.Pages[0].Content.OfType<TextContent>().Single();

        InterpreterTestSupport.AssertMatrix(expected, actual.Transform);
    }

    [Fact]
    public void ModifiedTextAfterUnbalancedQ_KeepsItsTransform()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Content("2 0 0 2 0 0 cm Q BT /F1 12 Tf 10 10 Td (Hi) Tj ET"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        var run = loaded.Pages[0].Content.OfType<TextContent>().Single();
        var expected = run.Transform;
        run.Color = Color.Red;

        var reloaded = InterpreterTestSupport.SaveAndLoad(loaded);
        var actual = reloaded.Pages[0].Content.OfType<TextContent>().Single();

        InterpreterTestSupport.AssertMatrix(expected, actual.Transform);
    }

    private static byte[] Content(string value) => Encoding.ASCII.GetBytes(value);
}
