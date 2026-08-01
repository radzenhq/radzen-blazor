#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

public class ColorPublicContractTests
{
    [Theory]
    [InlineData("#F0A", 255, 255, 0, 170)]
    [InlineData("f0A", 255, 255, 0, 170)]
    [InlineData("#F0A8", 136, 255, 0, 170)]
    [InlineData("f0A8", 136, 255, 0, 170)]
    [InlineData("#12ABef", 255, 18, 171, 239)]
    [InlineData("12abEF", 255, 18, 171, 239)]
    [InlineData("#11223380", 128, 17, 34, 51)]
    [InlineData("11223380", 128, 17, 34, 51)]
    public void FromHex_SupportsEveryCssHexForm(
        string text,
        byte alpha,
        byte red,
        byte green,
        byte blue)
    {
        var color = Color.FromHex(text);

        Assert.Equal(alpha, color.A);
        Assert.Equal(red, color.R);
        Assert.Equal(green, color.G);
        Assert.Equal(blue, color.B);
    }

    [Fact]
    public void FromHex_PutsCssAlphaLast()
        => Assert.Equal(Color.FromArgb(0x44, 0x11, 0x22, 0x33), Color.FromHex("#11223344"));

    [Fact]
    public void ChannelFactoriesSetEveryChannel()
    {
        Assert.Equal(Color.FromArgb(255, 10, 20, 30), Color.FromRgb(10, 20, 30));

        var color = Color.FromArgb(40, 10, 20, 30);
        Assert.Equal(40, color.A);
        Assert.Equal(10, color.R);
        Assert.Equal(20, color.G);
        Assert.Equal(30, color.B);
    }

    [Fact]
    public void NamedColorsHaveTheirCssChannelValues()
    {
        Assert.Equal(Color.FromHex("#000000"), Color.Black);
        Assert.Equal(Color.FromHex("#FFFFFF"), Color.White);
        Assert.Equal(Color.FromHex("#FF0000"), Color.Red);
        Assert.Equal(Color.FromHex("#008000"), Color.Green);
        Assert.Equal(Color.FromHex("#0000FF"), Color.Blue);
        Assert.Equal(Color.FromHex("#FFFF00"), Color.Yellow);
        Assert.Equal(Color.FromHex("#FFA500"), Color.Orange);
        Assert.Equal(Color.FromHex("#808080"), Color.Gray);
        Assert.Equal(Color.FromHex("#D3D3D3"), Color.LightGray);
        Assert.Equal(Color.FromHex("#A9A9A9"), Color.DarkGray);
        Assert.Equal(Color.FromHex("#00008B"), Color.DarkBlue);
        Assert.Equal(Color.FromHex("#663399"), Color.RebeccaPurple);
        Assert.Equal(Color.FromArgb(0, 0, 0, 0), Color.Transparent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("12")]
    [InlineData("#12")]
    [InlineData("#GGGGGG")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("#GGG")]
    [InlineData(" 112233")]
    [InlineData("112233 ")]
    [InlineData("##123")]
    public void FromHex_InvalidFormsThrowFormatException(string text)
        => Assert.Throws<FormatException>(() => Color.FromHex(text));

    [Fact]
    public void FromHex_NullThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => Color.FromHex(null!));

    [Fact]
    public void GreyAliasesMatchTheirGraySpelling()
    {
        Assert.Equal(Color.Gray, Color.Grey);
        Assert.Equal(Color.DarkGray, Color.DarkGrey);
        Assert.Equal(Color.LightSlateGray, Color.LightSlateGrey);
    }

    [Fact]
    public void EveryCssKeywordHasAMatchingStaticPropertyAndTableEntry()
    {
        var properties = typeof(Color)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(Color) && property.Name != "Transparent")
            .ToList();

        Assert.Equal(148, NamedColors.Count);
        Assert.Equal(148, properties.Count);
        Assert.All(properties, property =>
        {
            Assert.True(NamedColors.TryGet(property.Name, out var rgb));
            Assert.Equal(
                Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb),
                (Color)property.GetValue(null)!);
        });
    }

    [Fact]
    public void FromNameIsCaseInsensitiveAndRejectsUnknownKeywords()
    {
        Assert.Equal(Color.RebeccaPurple, Color.FromName("rebeccapurple"));
        Assert.Equal(Color.DarkSlateGray, Color.FromName("DARKSLATEGRAY"));
        Assert.Throws<FormatException>(() => Color.FromName("burntsienna"));
        Assert.Throws<ArgumentNullException>(() => Color.FromName(null!));
    }

    [Fact]
    public void EqualityAndHashCodeUseAllFourChannels()
    {
        var first = Color.FromArgb(4, 1, 2, 3);
        var equal = Color.FromHex("#01020304");
        var differentAlpha = Color.FromArgb(5, 1, 2, 3);

        Assert.True(first == equal);
        Assert.False(first != equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(first, differentAlpha);
        Assert.False(first.Equals((object)"#01020304"));
    }
}
