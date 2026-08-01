using System.Linq;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class LaidOutIdentityTests
{
    [Fact]
    public void RepeatedTableFragments_CarryStableExplicitNodeIdentity()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(120));
        section.Margins.SetAll(Unit.FromPoint(10));
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        header.Cells[0].Blocks.AddParagraph("Header");
        for (var i = 0; i < 12; i++)
        {
            table.Rows.Add().Cells[0].Blocks.AddParagraph($"Row {i}");
        }

        var pages = DocumentLayouter.Layout(document).Pages;
        var fragments = pages.Select(page => Assert.Single(page.Body.Tables)).ToArray();

        Assert.True(fragments.Length > 1);
        Assert.Equal(fragments.Length, fragments.Select(fragment => fragment.Id).Distinct().Count());
        Assert.Equal(fragments.Length, fragments.Select(fragment => fragment.Fragment.Id).Distinct().Count());
        Assert.Single(fragments.Select(fragment => fragment.Layout.Id).Distinct());
        Assert.Equal(pages.Length, pages.Select(page => page.Id).Distinct().Count());
        Assert.Equal(pages.Length, pages.Select(page => page.Body.Id).Distinct().Count());
    }
}
