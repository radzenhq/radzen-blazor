using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class SectionDirectionSupportTests
{
    [Fact]
    public void DefaultSection_Builds()
    {
        var document = new Document();
        document.Sections.Add();
        Assert.NotEmpty(new DocumentRenderer().ToArray(document));
    }

    [Fact]
    public void RightToLeftDirection_ThrowsOnBuild()
    {
        var document = new Document();
        document.Sections.Add().Direction = FlowDirection.RightToLeft;
        Assert.Throws<NotSupportedException>(() => new DocumentRenderer().ToArray(document));
    }

    [Fact]
    public void VerticalWritingMode_ThrowsOnBuild()
    {
        var document = new Document();
        document.Sections.Add().WritingMode = WritingMode.VerticalRightToLeft;
        Assert.Throws<NotSupportedException>(() => new DocumentRenderer().ToArray(document));
    }
}
