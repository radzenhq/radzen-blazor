using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor.Rendering;
using Radzen.Blazor.Spreadsheet;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Tests.Spreadsheet;

public class CustomCellEditorTests
{
    public class TextProbeEditor : ComponentBase
    {
        [Parameter]
        public SpreadsheetCellEditContext Context { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "class", "text-probe");
            builder.AddAttribute(2, "value", Context.Value);
            builder.AddAttribute(3, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, args => Context.Value = args.Value as string));
            builder.AddAttribute(4, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, OnKeyDownAsync));
            builder.CloseElement();
        }

        private async Task OnKeyDownAsync(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await Context.CommitAsync(Context.Value);
            }
        }
    }

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<VirtualRegion>("Radzen.createVirtualItemContainer", _ => true)
            .SetResult(new VirtualRegion { Width = 800, Height = 600, ScrollWidth = 800, ScrollHeight = 600 });
        ctx.Services.AddRadzenComponents();
        return ctx;
    }

    private static (Worksheet Sheet, IRenderedComponent<RadzenSpreadsheet> Spreadsheet) Render(TestContext ctx)
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 10);
        sheet.Cells.SetCustomType(new CellRef(0, 0), "text");

        var cellTypes = new Dictionary<string, SpreadsheetCellType>
        {
            ["text"] = new SpreadsheetCellType { EditorType = typeof(TextProbeEditor) }
        };

        var spreadsheet = ctx.RenderComponent<RadzenSpreadsheet>(p => p
            .Add(x => x.Workbook, workbook)
            .Add(x => x.CellTypes, cellTypes));

        return (sheet, spreadsheet);
    }

    private static int FocusCalls(TestContext ctx) =>
        ctx.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

    private static Task PressAsync(IRenderedComponent<RadzenSpreadsheet> spreadsheet, string key, string code) =>
        spreadsheet.InvokeAsync(() => spreadsheet.Instance.OnKeyDownAsync(new KeyboardEventArgs { Key = key, Code = code }, true));

    [Fact]
    public async Task TypingOnCustomCell_OpensEditorWithTypedCharacter()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(0, 0)));

        await PressAsync(spreadsheet, "a", "KeyA");

        Assert.Equal("a", spreadsheet.Find("input.text-probe").GetAttribute("value"));
    }

    [Fact]
    public async Task Enter_CommitsContextValueAndMovesDown()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(0, 0)));
        await PressAsync(spreadsheet, "a", "KeyA");
        spreadsheet.Find("input.text-probe").Input("0071");

        await PressAsync(spreadsheet, "Enter", "Enter");

        Assert.Equal(71d, sheet.Cells[0, 0].Value);
        Assert.Equal(new CellRef(1, 0), sheet.Selection.Cell);
        Assert.Empty(spreadsheet.FindAll("input.text-probe"));
    }

    [Fact]
    public async Task Escape_DiscardsContextValue()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(0, 0)));
        await PressAsync(spreadsheet, "a", "KeyA");
        spreadsheet.Find("input.text-probe").Input("hello");

        await PressAsync(spreadsheet, "Escape", "Escape");

        Assert.Null(sheet.Cells[0, 0].Value);
        Assert.Empty(spreadsheet.FindAll("input.text-probe"));
    }

    [Fact]
    public async Task Escape_ReturnsFocusToGrid()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(1, 0)));
        await PressAsync(spreadsheet, "a", "KeyA");
        var focusCalls = FocusCalls(ctx);

        await PressAsync(spreadsheet, "Escape", "Escape");

        Assert.Equal(focusCalls + 1, FocusCalls(ctx));
    }

    [Fact]
    public async Task EnterWithoutChanges_ReturnsFocusToGrid()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(1, 0)));
        await PressAsync(spreadsheet, "F2", "F2");
        var focusCalls = FocusCalls(ctx);

        await PressAsync(spreadsheet, "Enter", "Enter");

        Assert.Equal(new CellRef(2, 0), sheet.Selection.Cell);
        Assert.Equal(focusCalls + 1, FocusCalls(ctx));
    }

    [Fact]
    public async Task CommitWithoutChanges_ClosesEditorAndReturnsFocusToGrid()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(0, 0)));
        await PressAsync(spreadsheet, "F2", "F2");
        var focusCalls = FocusCalls(ctx);

        await spreadsheet.Find("input.text-probe").KeyDownAsync(new KeyboardEventArgs { Key = "Enter", Code = "Enter" });

        Assert.Empty(spreadsheet.FindAll("input.text-probe"));
        Assert.Equal(focusCalls + 1, FocusCalls(ctx));
    }

    [Fact]
    public async Task PasteWhileEditing_LeavesGridUnchanged()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(1, 0)));
        await PressAsync(spreadsheet, "a", "KeyA");

        await spreadsheet.InvokeAsync(() => spreadsheet.Instance.OnPasteAsync("aaa\tbbb"));

        Assert.Null(sheet.Cells[1, 0].Value);
        Assert.Null(sheet.Cells[1, 1].Value);
    }

    [Fact]
    public async Task PasteWithoutEditing_PastesIntoGrid()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(1, 0)));

        await spreadsheet.InvokeAsync(() => spreadsheet.Instance.OnPasteAsync("aaa\tbbb"));

        Assert.Equal("aaa", sheet.Cells[1, 0].Value);
        Assert.Equal("bbb", sheet.Cells[1, 1].Value);
    }

    [Fact]
    public async Task CopyWhileEditing_LeavesClipboardEmpty()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(1, 0)));
        await PressAsync(spreadsheet, "a", "KeyA");

        await spreadsheet.InvokeAsync(() => spreadsheet.Instance.OnCopyAsync());

        Assert.Empty(spreadsheet.FindAll(".rz-spreadsheet-copy-overlay"));
    }

    [Fact]
    public async Task CopyWithoutEditing_CopiesSelection()
    {
        using var ctx = CreateContext();
        var (sheet, spreadsheet) = Render(ctx);
        await spreadsheet.InvokeAsync(() => sheet.Selection.Select(new CellRef(1, 0)));

        await spreadsheet.InvokeAsync(() => spreadsheet.Instance.OnCopyAsync());

        Assert.NotEmpty(spreadsheet.FindAll(".rz-spreadsheet-copy-overlay"));
    }
}
