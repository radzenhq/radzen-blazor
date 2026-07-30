#nullable enable

using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class DirtyReencodeTests
{
    private static PortableDocument BuildContentDocument()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();

        page.Content.Add(new TextContent("Original", 72, 700)
        {
            Color = Color.Blue,
            Font = new Font { Size = 12 },
        });

        var path = page.Content.Add(new PathContent { Stroke = true, Thickness = 2 });
        path.MoveTo(10, 10);
        path.LineTo(200, 10);

        return document;
    }

    [Fact]
    public void UntouchedPage_ReadOnly_ReemitsByteIdenticalContentStream()
    {
        var original = BuildContentDocument().ToArray();

        var loaded = InterpreterTestSupport.Load(original);
        var content = loaded.Pages[0].Content;
        Assert.True(content.Count > 0);

        var resaved = loaded.ToArray();

        Assert.Equal(
            InterpreterTestSupport.PageContentBytes(original, 0),
            InterpreterTestSupport.PageContentBytes(resaved, 0));
    }

    [Fact]
    public void UntouchedPage_MaterializedMultipleTimes_StaysByteIdentical()
    {
        var original = BuildContentDocument().ToArray();

        var loaded = InterpreterTestSupport.Load(original);
        _ = loaded.Pages[0].Content.Count;
        _ = loaded.Pages[0].Content[0];
        _ = loaded.Pages[0].Content;

        var resaved = loaded.ToArray();

        Assert.Equal(
            InterpreterTestSupport.PageContentBytes(original, 0),
            InterpreterTestSupport.PageContentBytes(resaved, 0));
    }

    [Fact]
    public void TouchedPage_AddElement_ChangesContentStreamBytes()
    {
        var original = BuildContentDocument().ToArray();

        var loaded = InterpreterTestSupport.Load(original);
        loaded.Pages[0].Content.Add(new TextContent("EXTRA", 100, 100));
        var resaved = loaded.ToArray();

        Assert.NotEqual(
            InterpreterTestSupport.PageContentBytes(original, 0),
            InterpreterTestSupport.PageContentBytes(resaved, 0));
    }

    [Fact]
    public void TouchedPage_AddElement_SurvivesReload()
    {
        var loaded = InterpreterTestSupport.Load(BuildContentDocument().ToArray());
        var before = loaded.Pages[0].Content.Count;

        loaded.Pages[0].Content.Add(new TextContent("EXTRA", 100, 100));

        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());
        var content = reloaded.Pages[0].Content;

        Assert.Equal(before + 1, content.Count);
        Assert.Contains(content, e => e is TextContent t && t.Text == "EXTRA");
    }

    [Fact]
    public void TouchedPage_MutateElementColor_SurvivesReload()
    {
        var loaded = InterpreterTestSupport.Load(BuildContentDocument().ToArray());

        var text = Assert.IsType<TextContent>(loaded.Pages[0].Content[0]);
        Assert.Equal(Color.Blue, text.Color);
        text.Color = Color.Red;

        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());
        var roundTripped = Assert.IsType<TextContent>(reloaded.Pages[0].Content[0]);

        Assert.Equal(Color.Red, roundTripped.Color);
    }

    [Fact]
    public void TouchedPage_MutateElementText_SurvivesReload()
    {
        var loaded = InterpreterTestSupport.Load(BuildContentDocument().ToArray());

        var text = Assert.IsType<TextContent>(loaded.Pages[0].Content[0]);
        text.Text = "Changed";

        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());
        var roundTripped = reloaded.Pages[0].Content.OfType<TextContent>().First();

        Assert.Equal("Changed", roundTripped.Text);
    }

    [Fact]
    public void TouchedPage_MutateElementTransform_SurvivesReload()
    {
        var loaded = InterpreterTestSupport.Load(BuildContentDocument().ToArray());

        var element = loaded.Pages[0].Content[0];
        element.Transform = Matrix.Translate(15, 25);

        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());
        var roundTripped = reloaded.Pages[0].Content[0];

        InterpreterTestSupport.AssertMatrix(Matrix.Translate(15, 25), roundTripped.Transform);
    }
}
