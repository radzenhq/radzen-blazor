#nullable enable
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

public class DocumentTests
{
    [Fact]
    public void Section_MarginConvenienceSetsAllEdges()
    {
        var s = new Document().Sections.Add();
        s.Margins.SetAll(Unit.FromPoint(18));
        Assert.Equal(18, s.Margins.Top.Point, 9);
        Assert.Equal(18, s.Margins.Right.Point, 9);
        Assert.Equal(18, s.Margins.Bottom.Point, 9);
        Assert.Equal(18, s.Margins.Left.Point, 9);
    }
}
