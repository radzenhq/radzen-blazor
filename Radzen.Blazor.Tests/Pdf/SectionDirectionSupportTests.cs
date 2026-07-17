using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class SectionDirectionSupportTests
{
    [Fact]
    public void DefaultSection_Builds()
    {
        var builder = new DocumentBuilder();
        builder.Sections.Add();
        Assert.NotEmpty(builder.ToArray());
    }

    [Fact]
    public void RightToLeftDirection_ThrowsOnBuild()
    {
        var builder = new DocumentBuilder();
        builder.Sections.Add().Direction = FlowDirection.RightToLeft;
        Assert.Throws<NotSupportedException>(() => builder.ToArray());
    }

    [Fact]
    public void VerticalWritingMode_ThrowsOnBuild()
    {
        var builder = new DocumentBuilder();
        builder.Sections.Add().WritingMode = WritingMode.VerticalRightToLeft;
        Assert.Throws<NotSupportedException>(() => builder.ToArray());
    }
}
