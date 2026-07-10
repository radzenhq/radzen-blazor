#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

using Colors = Radzen.Documents.Pdf.Colors;

public class ColorsTests
{
    private static void AssertRgb(Color c, byte r, byte g, byte b)
    {
        Assert.Equal(r, c.R);
        Assert.Equal(g, c.G);
        Assert.Equal(b, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void Black() => AssertRgb(Colors.Black, 0, 0, 0);

    [Fact]
    public void White() => AssertRgb(Colors.White, 255, 255, 255);

    [Fact]
    public void Red() => AssertRgb(Colors.Red, 255, 0, 0);

    [Fact]
    public void Green() => AssertRgb(Colors.Green, 0, 128, 0);

    [Fact]
    public void Blue() => AssertRgb(Colors.Blue, 0, 0, 255);

    [Fact]
    public void Gray() => AssertRgb(Colors.Gray, 128, 128, 128);

    [Fact]
    public void LightGray() => AssertRgb(Colors.LightGray, 211, 211, 211);

    [Fact]
    public void DarkBlue() => AssertRgb(Colors.DarkBlue, 0, 0, 139);
}
