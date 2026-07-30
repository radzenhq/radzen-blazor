#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentInterpreterTests
{
    private static PortableDocument BuildAndReload()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();

        page.Content.Add(new TextContent("Hello", 72, 700)
        {
            Font = new Font { Size = 14 },
            Color = Color.Blue,
            Transform = Matrix.Scale(2, 2),
        });

        var filled = page.Content.Add(new PathContent
        {
            Fill = true,
            FillColor = Color.Red,
            Transform = Matrix.Translate(5, 5),
        });
        filled.MoveTo(0, 0);
        filled.LineTo(100, 0);
        filled.LineTo(100, 50);
        filled.Close();

        var stroked = page.Content.Add(new PathContent
        {
            Stroke = true,
            Thickness = 3,
            StrokeColor = Color.Blue,
        });
        stroked.MoveTo(10, 10);
        stroked.LineTo(90, 10);

        return InterpreterTestSupport.SaveAndLoad(document);
    }

    [Fact]
    public void Materialize_ProducesOneElementPerDrawingOperation()
    {
        var reloaded = BuildAndReload();

        Assert.Equal(3, reloaded.Pages[0].Content.Count);
    }

    [Fact]
    public void Materialize_TextShowingOp_BecomesTextContent()
    {
        var content = BuildAndReload().Pages[0].Content;

        Assert.IsType<TextContent>(content[0]);
    }

    [Fact]
    public void Materialize_Text_RoundTripsTextValue()
    {
        var content = BuildAndReload().Pages[0].Content;

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal("Hello", text.Text);
    }

    [Fact]
    public void Materialize_Text_RoundTripsFillColor()
    {
        var content = BuildAndReload().Pages[0].Content;

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal(Color.Blue, text.Color);
    }

    [Fact]
    public void Materialize_Text_RoundTripsFontSize()
    {
        var content = BuildAndReload().Pages[0].Content;

        var text = Assert.IsType<TextContent>(content[0]);
        Assert.Equal(14, text.Font.Size!.Value.Point, 3);
    }

    [Fact]
    public void Materialize_Text_FoldsPositionAndCtmIntoAbsoluteTransform()
    {
        var content = BuildAndReload().Pages[0].Content;

        var text = Assert.IsType<TextContent>(content[0]);

        var expected = Matrix.Translate(72, 700) * Matrix.Scale(2, 2);
        InterpreterTestSupport.AssertMatrix(expected, text.Transform);
    }

    [Fact]
    public void Materialize_FillPathOp_BecomesFilledPathContent()
    {
        var content = BuildAndReload().Pages[0].Content;

        var path = Assert.IsType<PathContent>(content[1]);
        Assert.True(path.Fill);
        Assert.False(path.Stroke);
    }

    [Fact]
    public void Materialize_FillPath_RoundTripsFillColor()
    {
        var content = BuildAndReload().Pages[0].Content;

        var path = Assert.IsType<PathContent>(content[1]);
        Assert.Equal(Color.Red, path.FillColor);
    }

    [Fact]
    public void Materialize_FillPath_RoundTripsAbsoluteTransform()
    {
        var content = BuildAndReload().Pages[0].Content;

        var path = Assert.IsType<PathContent>(content[1]);
        InterpreterTestSupport.AssertMatrix(Matrix.Translate(5, 5), path.Transform);
    }

    [Fact]
    public void Materialize_StrokePathOp_BecomesStrokedPathContent()
    {
        var content = BuildAndReload().Pages[0].Content;

        var path = Assert.IsType<PathContent>(content[2]);
        Assert.True(path.Stroke);
        Assert.False(path.Fill);
    }

    [Fact]
    public void Materialize_StrokePath_RoundTripsThickness()
    {
        var content = BuildAndReload().Pages[0].Content;

        var path = Assert.IsType<PathContent>(content[2]);
        Assert.Equal(3, path.Thickness, 3);
    }

    [Fact]
    public void Materialize_StrokePath_RoundTripsStrokeColor()
    {
        var content = BuildAndReload().Pages[0].Content;

        var path = Assert.IsType<PathContent>(content[2]);
        Assert.Equal(Color.Blue, path.StrokeColor);
    }

    [Fact]
    public void Materialize_IsIdempotentAcrossRepeatedAccess()
    {
        var reloaded = BuildAndReload();

        var first = reloaded.Pages[0].Content;
        var second = reloaded.Pages[0].Content;

        Assert.Equal(first.Count, second.Count);
        Assert.Same(first[0], second[0]);
    }
}
