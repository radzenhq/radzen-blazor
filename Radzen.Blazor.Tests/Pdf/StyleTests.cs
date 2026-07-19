#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class StyleTests
{
    private static StyleCollection NewStyles() => new DocumentBuilder().Styles;

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
