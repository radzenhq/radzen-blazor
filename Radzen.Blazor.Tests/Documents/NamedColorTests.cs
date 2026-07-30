#nullable enable
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Documents.Tests;


public class NamedColorTests
{
    private static void AssertRgb(Color c, byte r, byte g, byte b)
    {
        Assert.Equal(r, c.R);
        Assert.Equal(g, c.G);
        Assert.Equal(b, c.B);
        Assert.Equal(255, c.A);
    }

    [Fact]
    public void Black() => AssertRgb(Color.Black, 0, 0, 0);

    [Fact]
    public void White() => AssertRgb(Color.White, 255, 255, 255);

    [Fact]
    public void Red() => AssertRgb(Color.Red, 255, 0, 0);

    [Fact]
    public void Green() => AssertRgb(Color.Green, 0, 128, 0);

    [Fact]
    public void Blue() => AssertRgb(Color.Blue, 0, 0, 255);

    [Fact]
    public void Gray() => AssertRgb(Color.Gray, 128, 128, 128);

    [Fact]
    public void LightGray() => AssertRgb(Color.LightGray, 211, 211, 211);

    [Fact]
    public void DarkBlue() => AssertRgb(Color.DarkBlue, 0, 0, 139);
}
