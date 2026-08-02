#nullable enable
using System.Linq;
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Blazor.Tests.Isolated;
namespace Radzen.Blazor.Documents.Tests;

public class RepeatingHeaderTests
{
    [Fact]
    public void SingleHeader_RepeatsOnEveryFragment()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 12);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        Assert.True(fragments.Count >= 3);
        foreach (var fragment in fragments)
        {
            Assert.Equal([0], TablePaginationSupport.HeaderRows(fragment).ToArray());
        }
    }

    [Fact]
    public void MultipleHeaderRows_AllRepeatInOrder()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 2, bodies: 8);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        Assert.True(fragments.Count >= 3);
        foreach (var fragment in fragments)
        {
            Assert.Equal([0, 1], TablePaginationSupport.HeaderRows(fragment).ToArray());
            Assert.True(fragment.Rows[0].IsHeader);
            Assert.True(fragment.Rows[1].IsHeader);
            Assert.Equal(0, fragment.Rows[0].Y, 6);
            Assert.Equal(lh, fragment.Rows[1].Y, 6);
            Assert.False(fragment.Rows[2].IsHeader);
            Assert.Equal(2 * lh, fragment.Rows[2].Y, 6);
        }
    }

    [Fact]
    public void MoreHeaderRows_ReduceBodyCapacity()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var available = TablePaginationSupport.Capacity(lh, 5);

        var oneHeader = TablePaginationSupport.Build(headers: 1, bodies: 20);
        var twoHeader = TablePaginationSupport.Build(headers: 2, bodies: 20);
        var oneLayout = IsolatedTableLayout.LayoutIsolated(oneHeader, 300, fonts);
        var twoLayout = IsolatedTableLayout.LayoutIsolated(twoHeader, 300, fonts);

        var oneFragments = IsolatedTablePaginator.Paginate(oneLayout, oneHeader, available);
        var twoFragments = IsolatedTablePaginator.Paginate(twoLayout, twoHeader, available);

        Assert.Equal(5, oneFragments.Count);
        Assert.Equal(7, twoFragments.Count);
        Assert.True(twoFragments.Count > oneFragments.Count);
    }

    [Fact]
    public void HeaderHeight_CountedInEveryFragmentHeight()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 2, bodies: 8);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        foreach (var fragment in fragments)
        {
            Assert.Equal(fragment.Rows.Length * lh, fragment.Height, 6);
        }
    }

    [Fact]
    public void BorderResolution_BottomAtFragmentEnd_HeaderRedrawnAtNextTop()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 1, bodies: 10);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 5));

        for (var i = 0; i < fragments.Count; i++)
        {
            var fragment = fragments[i];
            var last = fragment.Rows[^1];
            Assert.Equal(fragment.Height, last.Y + last.Height, 6);

            if (i + 1 < fragments.Count)
            {
                var nextTop = fragments[i + 1].Rows[0];
                Assert.True(nextTop.IsHeader);
                Assert.Equal(0, nextTop.Y, 6);
                Assert.Equal(fragment.Rows[0].SourceRow, nextTop.SourceRow);
            }
        }
    }

    [Fact]
    public void HeaderRows_IdenticalSourceAcrossAllFragments()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 2, bodies: 15);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 6));

        var expected = new[] { 0, 1 };
        foreach (var fragment in fragments)
        {
            Assert.Equal(expected, TablePaginationSupport.HeaderRows(fragment).ToArray());
            Assert.All(
                fragment.Rows.Where(r => r.IsHeader),
                r => Assert.Equal(layout.RowHeights[r.SourceRow], r.Height, 6));
        }
    }

    [Fact]
    public void SingleFragment_HeaderPresentButNotRepeated()
    {
        var fonts = TablePaginationSupport.Fonts();
        var lh = TablePaginationSupport.LineHeight();
        var table = TablePaginationSupport.Build(headers: 2, bodies: 2);
        var layout = IsolatedTableLayout.LayoutIsolated(table, 300, fonts);

        var fragments = IsolatedTablePaginator.Paginate(layout, table, TablePaginationSupport.Capacity(lh, 20));

        Assert.Single(fragments);
        Assert.Equal(4, fragments[0].Rows.Length);
        Assert.Equal([0, 1], TablePaginationSupport.HeaderRows(fragments[0]).ToArray());
        Assert.Equal([2, 3], TablePaginationSupport.BodyRows(fragments[0]).ToArray());
    }
}
