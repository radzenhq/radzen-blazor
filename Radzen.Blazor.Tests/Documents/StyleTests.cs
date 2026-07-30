#nullable enable
using System;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Documents.Tests;

public class StyleTests
{
    private static StyleCollection NewStyles() => new Document().Styles;

    [Fact]
    public void Add_WithExplicitBaseStyle()
    {
        var styles = NewStyles();
        styles.Add("Caption");
        var s = styles.Add("SubCaption", "Caption");
        Assert.Equal("Caption", s.BaseStyle);
    }

    [Fact]
    public void Add_DuplicateName_Throws()
    {
        var styles = NewStyles();
        styles.Add("Caption");
        Assert.Throws<ArgumentException>(() => styles.Add("Caption"));
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
        Assert.Throws<ArgumentException>(() => styles.Add("Caption", "DoesNotExist"));
    }

    [Fact]
    public void BuiltInHeadingStyles_CarryTheirHeadingLevel()
    {
        var styles = NewStyles();
        for (var level = 1; level <= 6; level++)
        {
            Assert.Equal(level, styles["Heading" + level].HeadingLevel);
        }
    }

    [Fact]
    public void HeadingLevel_OutsideOneToSix_Throws()
    {
        var style = NewStyles().Add("Caption");
        Assert.Throws<ArgumentOutOfRangeException>(() => style.HeadingLevel = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => style.HeadingLevel = 7);
        style.HeadingLevel = null;
        Assert.Null(style.HeadingLevel);
    }

    [Fact]
    public void BaseStyle_ChainIsWalkable()
    {
        var styles = NewStyles();
        styles.Add("Caption");
        styles.Add("SubCaption", "Caption");

        var current = styles["SubCaption"];
        Assert.Equal("Caption", current.BaseStyle);
        current = styles[current.BaseStyle!];
        Assert.Equal("Normal", current.BaseStyle);
        current = styles[current.BaseStyle!];
        Assert.Null(current.BaseStyle);
    }
}
