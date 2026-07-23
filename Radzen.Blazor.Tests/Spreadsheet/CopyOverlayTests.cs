using System;
using System.Collections.Generic;
using Bunit;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CopyOverlayTests : TestContext
{
    [Fact]
    public void CopyOverlay_RendersNothing_When_Clipboard_Empty()
    {
        var sheet = new Worksheet(10, 10);
        var spreadsheet = new RadzenSpreadsheet();
        var context = new CopyMockContext(sheet);

        var cut = RenderComponent<CopyOverlay>(parameters => parameters
            .Add(p => p.Worksheet, sheet)
            .Add(p => p.Spreadsheet, spreadsheet)
            .Add(p => p.Context, context));

        Assert.Empty(cut.FindAll(".rz-spreadsheet-copy-overlay"));
    }

    [Fact]
    public void CopyOverlay_RendersSvg_When_Clipboard_Has_Source()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Selection.Select(RangeRef.Parse("B2:C3"));
        
        var spreadsheet = new RadzenSpreadsheet();
        spreadsheet.clipboard.Copy(sheet);

        var context = new CopyMockContext(sheet);

        var cut = RenderComponent<CopyOverlay>(parameters => parameters
            .Add(p => p.Worksheet, sheet)
            .Add(p => p.Spreadsheet, spreadsheet)
            .Add(p => p.Context, context));

        var svgs = cut.FindAll(".rz-spreadsheet-copy-overlay");
        Assert.Single(svgs);

        var rect = cut.Find("rect");
        Assert.NotNull(rect);
    }

    [Fact]
    public void CopyOverlay_DoesNotRender_On_Different_Sheet()
    {
        var sheet1 = new Worksheet(10, 10);
        var sheet2 = new Worksheet(10, 10);
        sheet1.Selection.Select(RangeRef.Parse("A1:B2"));

        var spreadsheet = new RadzenSpreadsheet();
        spreadsheet.clipboard.Copy(sheet1); // Copied on sheet1

        var context = new CopyMockContext(sheet2);

        var cut = RenderComponent<CopyOverlay>(parameters => parameters
            .Add(p => p.Worksheet, sheet2)
            .Add(p => p.Spreadsheet, spreadsheet)
            .Add(p => p.Context, context));

        Assert.Empty(cut.FindAll(".rz-spreadsheet-copy-overlay")); // Not drawn on sheet2
    }
}

#nullable enable
public class CopyMockContext : IVirtualGridContext
{
    private SheetView? view;

    public CopyMockContext(Worksheet? sheet = null)
    {
        if (sheet != null)
        {
            view = new SheetView(sheet);
        }
    }

    public PixelRectangle GetRectangle(int row, int column)
    {
        return new PixelRectangle(row * 24, column * 100, (row + 1) * 24, (column + 1) * 100);
    }

    public PixelRectangle GetRectangle(int top, int left, int bottom, int right)
    {
        return new PixelRectangle(new PixelRange(left * 100, (right + 1) * 100), new PixelRange(top * 24, (bottom + 1) * 24));
    }

    public IEnumerable<RangeInfo> GetRanges(RangeRef range) =>
        view != null ? view.GetRanges(range) : [new RangeInfo { Range = range }];

#pragma warning disable CS0067
    public event Action? Scrolled;
#pragma warning restore CS0067
}