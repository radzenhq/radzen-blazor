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
        var clipboard = new SpreadsheetClipboard();
        var context = new CopyMockContext(sheet);

        var cut = RenderComponent<CopyOverlay>(parameters => parameters
            .Add(p => p.Worksheet, sheet)
            .Add(p => p.Context, context)
            .AddCascadingValue(clipboard));

        Assert.Empty(cut.FindAll(".rz-spreadsheet-copy-overlay"));
    }

    [Fact]
    public void CopyOverlay_RendersEdges_When_Clipboard_Has_Source()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Selection.Select(RangeRef.Parse("B2:C3"));
        
        var clipboard = new SpreadsheetClipboard();
        clipboard.Copy(sheet);

        var context = new CopyMockContext(sheet);

        var cut = RenderComponent<CopyOverlay>(parameters => parameters
            .Add(p => p.Worksheet, sheet)
            .Add(p => p.Context, context)
            .AddCascadingValue(clipboard));

        var overlays = cut.FindAll(".rz-spreadsheet-copy-overlay");
        Assert.Single(overlays);

        Assert.NotEmpty(cut.FindAll(".rz-copy-edge"));
    }

    [Fact]
    public void CopyOverlay_DoesNotRender_On_Different_Sheet()
    {
        var sheet1 = new Worksheet(10, 10);
        var sheet2 = new Worksheet(10, 10);
        sheet1.Selection.Select(RangeRef.Parse("A1:B2"));

        var clipboard = new SpreadsheetClipboard();
        clipboard.Copy(sheet1); // Copied on sheet1

        var context = new CopyMockContext(sheet2);

        var cut = RenderComponent<CopyOverlay>(parameters => parameters
            .Add(p => p.Worksheet, sheet2)
            .Add(p => p.Context, context)
            .AddCascadingValue(clipboard));

        Assert.Empty(cut.FindAll(".rz-spreadsheet-copy-overlay")); // Not drawn on sheet2
    }
    [Fact]
    public async void RendersEdges_WhenClipboardChanged_FiresAfterInitialRender()
    {
        var sheet = new Worksheet(10, 10);
        var clipboard = new SpreadsheetClipboard();
        var context = new CopyMockContext(sheet);

        var cut = RenderComponent<CopyOverlay>(parameters => parameters
            .Add(p => p.Worksheet, sheet)
            .Add(p => p.Context, context)
            .AddCascadingValue(clipboard));

        Assert.Empty(cut.FindAll(".rz-copy-edge"));

        sheet.Selection.Select(RangeRef.Parse("A1"));
        await cut.InvokeAsync(() => clipboard.Copy(sheet));
        Assert.NotEmpty(cut.FindAll(".rz-copy-edge"));
    }

    [Fact]
    public void RendersFrozenClasses_AndTopEdgeOnly_OnSplitRange()
    {
        var sheet = new Worksheet(10, 10);
        var clipboard = new SpreadsheetClipboard();
        sheet.Selection.Select(RangeRef.Parse("A1"));
        clipboard.Copy(sheet);

        var frozenContext = new CopyMockContext(sheet);
        frozenContext.SetOverrideRanges(new List<RangeInfo>
        {
            new RangeInfo
            {
                Range = RangeRef.Parse("A1"),
                Top = true,
                Right = true,
                Bottom = false,
                Left = true,
                FrozenRow = true,
                FrozenColumn = false
            }
        });

        var cut = RenderComponent<CopyOverlay>(parameters => parameters
            .Add(p => p.Worksheet, sheet)
            .Add(p => p.Context, frozenContext)
            .AddCascadingValue(clipboard));

        Assert.NotNull(cut.Find(".rz-spreadsheet-frozen-row"));
        Assert.NotEmpty(cut.FindAll(".rz-copy-edge-top"));
        Assert.Empty(cut.FindAll(".rz-copy-edge-bottom"));
    }
}

#nullable enable
public class CopyMockContext : IVirtualGridContext
{
    private SheetView? view;
    private IEnumerable<RangeInfo>? overrideRanges;

    public CopyMockContext(Worksheet? sheet = null)
    {
        if (sheet != null)
        {
            view = new SheetView(sheet);
        }
    }

    public void SetOverrideRanges(IEnumerable<RangeInfo> ranges)
    {
        overrideRanges = ranges;
    }

    public PixelRectangle GetRectangle(int row, int column)
    {
        return new PixelRectangle(row * 24, column * 100, (row + 1) * 24, (column + 1) * 100);
    }

    public PixelRectangle GetRectangle(int top, int left, int bottom, int right)
    {
        return new PixelRectangle(new PixelRange(left * 100, (right + 1) * 100), new PixelRange(top * 24, (bottom + 1) * 24));
    }

    public IEnumerable<RangeInfo> GetRanges(RangeRef range)
    {
        if (overrideRanges != null)
        {
            return overrideRanges;
        }

        return view != null ? view.GetRanges(range) : [new RangeInfo { Range = range, Top = true, Right = true, Bottom = true, Left = true }];
    }

#pragma warning disable CS0067
    public event Action? Scrolled;
#pragma warning restore CS0067
}
