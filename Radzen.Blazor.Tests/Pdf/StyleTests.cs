#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class StyleTests
{
    private static StyleCollection NewStyles() => new DocumentBuilder().Styles;

    [Fact]
    public void Normal_AlwaysExists()
    {
        var styles = NewStyles();
        Assert.NotNull(styles.Normal);
        Assert.Equal("Normal", styles.Normal.Name);
        Assert.True(styles.Contains("Normal"));
    }

    [Fact]
    public void Normal_FontDefaults()
    {
        var normal = NewStyles().Normal;
        Assert.NotNull(normal.Font);
        Assert.Equal("Helvetica", normal.Font.Name);
        Assert.Equal(10, normal.Font.Size, 9);
    }

    [Fact]
    public void Normal_BaseStyleIsNull()
    {
        Assert.Null(NewStyles().Normal.BaseStyle);
    }

    [Fact]
    public void Style_DefaultAlignmentLeft()
    {
        Assert.Equal(HorizontalAlignment.Left, NewStyles().Normal.Alignment);
    }

    [Fact]
    public void Add_ReturnsStyleWithDefaultBaseNormal()
    {
        var styles = NewStyles();
        var s = styles.Add("Heading1");
        Assert.Equal("Heading1", s.Name);
        Assert.Equal("Normal", s.BaseStyle);
        Assert.True(styles.Contains("Heading1"));
    }

    [Fact]
    public void Add_WithExplicitBaseStyle()
    {
        var styles = NewStyles();
        styles.Add("Heading1");
        var s = styles.Add("Heading2", "Heading1");
        Assert.Equal("Heading1", s.BaseStyle);
    }

    [Fact]
    public void Add_DuplicateName_Throws()
    {
        var styles = NewStyles();
        styles.Add("Heading1");
        Assert.Throws<ArgumentException>(() => styles.Add("Heading1"));
    }

    [Fact]
    public void Add_DuplicateOfNormal_Throws()
    {
        var styles = NewStyles();
        Assert.Throws<ArgumentException>(() => styles.Add("Normal"));
    }

    [Fact]
    public void Add_UnknownBaseStyle_Throws()
    {
        var styles = NewStyles();
        Assert.Throws<ArgumentException>(() => styles.Add("Heading1", "DoesNotExist"));
    }

    [Fact]
    public void BaseStyle_ChainIsWalkable()
    {
        var styles = NewStyles();
        styles.Add("Heading1");
        styles.Add("Heading2", "Heading1");

        var current = styles["Heading2"];
        Assert.Equal("Heading1", current.BaseStyle);
        current = styles[current.BaseStyle!];
        Assert.Equal("Normal", current.BaseStyle);
        current = styles[current.BaseStyle!];
        Assert.Null(current.BaseStyle);
    }
}
