using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class CellMenuFilterSelectionTests
{
    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddRadzenComponents();
        return ctx;
    }

    private static Worksheet CreateSheet(params object?[] values)
    {
        var sheet = new Workbook().AddSheet("Sheet1", values.Length + 1, 2);
        sheet.Cells[0, 0].Value = "Product";

        for (var i = 0; i < values.Length; i++)
        {
            sheet.Cells[i + 1, 0].Value = values[i];
        }

        sheet.AutoFilter.Range = new RangeRef(new CellRef(0, 0), new CellRef(values.Length, 1));
        return sheet;
    }

    private static void Filter(Worksheet sheet, params object?[] values) =>
        sheet.AddFilter(new SheetFilter(
            new InListCriterion { Column = 0, Values = values },
            new RangeRef(new CellRef(0, 0), new CellRef(sheet.RowCount - 1, 0))));

    private static void AddColumnFilter(Worksheet sheet, FilterCriterion criterion) =>
        sheet.AddFilter(new SheetFilter(criterion, new RangeRef(new CellRef(0, 0), new CellRef(sheet.RowCount - 1, 0))));

    private static IRenderedComponent<CellMenu> Render(TestContext ctx, Worksheet sheet, System.Action<SheetFilter?>? apply = null, System.Action? cancel = null, System.Action? clear = null) =>
        ctx.RenderComponent<CellMenu>(p => p
            .Add(x => x.Worksheet, sheet)
            .Add(x => x.Row, 0)
            .Add(x => x.Column, 0)
            .Add(x => x.Cancel, EventCallback.Factory.Create(new object(), () => cancel?.Invoke()))
            .Add(x => x.Clear, EventCallback.Factory.Create(new object(), () => clear?.Invoke()))
            .Add(x => x.Apply, EventCallback.Factory.Create<SheetFilter?>(new object(), filter => apply?.Invoke(filter))));

    private static Dictionary<string, bool> ValueStates(IRenderedComponent<CellMenu> menu)
    {
        var labels = menu.FindComponents<RadzenLabel>().ToDictionary(label => label.Instance.Component!, label => label.Instance.Text!);

        return menu.FindComponents<RadzenCheckBox<bool>>().ToDictionary(box => labels[box.Instance.Name!], box => box.Instance.Value);
    }

    private static bool? SelectAllState(IRenderedComponent<CellMenu> menu) =>
        menu.FindComponent<RadzenCheckBox<bool?>>().Instance.Value;

    private static void Toggle(IRenderedComponent<CellMenu> menu, string text)
    {
        var name = menu.FindComponents<RadzenLabel>().Single(label => label.Instance.Text == text).Instance.Component;

        menu.FindComponents<RadzenCheckBox<bool>>().Single(box => box.Instance.Name == name).Find("input").Change(true);
    }

    private static void ClickOk(IRenderedComponent<CellMenu> menu) =>
        menu.FindAll("button").Single(button => button.TextContent.Contains("OK")).Click();

    [Fact]
    public void Reopen_ShowsTheFilteredTextValueChecked()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet("Laptops", "Cables", "Mice");
        Filter(sheet, "Cables");

        var menu = Render(ctx, sheet);

        Assert.Equal(new Dictionary<string, bool> { ["Cables"] = true, ["Laptops"] = false, ["Mice"] = false }, ValueStates(menu));
        Assert.Null(SelectAllState(menu));
    }

    [Fact]
    public void Reopen_ShowsFilteredNumbersChecked()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(1d, 2d, 3d);
        Filter(sheet, 2d);

        Assert.Equal(new Dictionary<string, bool> { ["1"] = false, ["2"] = true, ["3"] = false }, ValueStates(Render(ctx, sheet)));
    }

    [Fact]
    public void Reopen_ShowsTextThatLooksLikeANumberChecked()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(null, "abc");
        sheet.Cells[1, 0].Format.NumberFormat = "@";
        sheet.Cells[1, 0].Value = "123";
        Filter(sheet, "123");

        Assert.Equal(new Dictionary<string, bool> { ["123"] = true, ["abc"] = false }, ValueStates(Render(ctx, sheet)));
    }

    [Fact]
    public void Reopen_ShowsNumbersFilteredByTextChecked()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(1d, 2d);
        Filter(sheet, "2");

        Assert.Equal(new Dictionary<string, bool> { ["1"] = false, ["2"] = true }, ValueStates(Render(ctx, sheet)));
    }

    [Fact]
    public void Reopen_ShowsBlankCheckedWhenTheFilterIncludesIt()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet("Cables", null, "Mice");
        Filter(sheet, "Cables", null);

        var states = ValueStates(Render(ctx, sheet));

        Assert.True(states["Cables"]);
        Assert.False(states["Mice"]);
        Assert.True(states.Single(state => state.Key.Contains("Blank")).Value);
    }

    [Fact]
    public void Reopen_ChecksEverythingWithoutAFilter()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet("Laptops", "Cables");

        var menu = Render(ctx, sheet);

        Assert.All(ValueStates(menu).Values, Assert.True);
        Assert.True(SelectAllState(menu));
    }

    [Fact]
    public void Ok_LeavesTheFilterAloneWhenNothingChanged()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet("Laptops", "Cables", "Mice");
        Filter(sheet, "Cables");
        var applied = false;
        var cancelled = false;

        ClickOk(Render(ctx, sheet, _ => applied = true, () => cancelled = true));

        Assert.False(applied);
        Assert.True(cancelled);
    }

    [Fact]
    public void Ok_DropsAValueTheUserUnchecked()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(1d, 2d, 3d);
        Filter(sheet, "1", "2");
        SheetFilter? applied = null;
        var menu = Render(ctx, sheet, filter => applied = filter);

        Toggle(menu, "1");
        ClickOk(menu);

        Assert.Equal([2d], Assert.IsType<InListCriterion>(applied!.Criterion).Values);
    }

    [Fact]
    public void Ok_KeepsValuesHiddenByAnotherColumnFilter()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet("EMEA", "AMER", "APAC", "LATAM");
        sheet.Cells[1, 1].Value = "Open";
        sheet.Cells[2, 1].Value = "Closed";
        sheet.Cells[3, 1].Value = "Open";
        sheet.Cells[4, 1].Value = "Open";
        var range = new RangeRef(new CellRef(0, 1), new CellRef(4, 1));
        Filter(sheet, "EMEA", "AMER");
        sheet.AddFilter(new SheetFilter(new InListCriterion { Column = 1, Values = ["Open"] }, range));
        SheetFilter? applied = null;
        var menu = Render(ctx, sheet, filter => applied = filter);

        Toggle(menu, "APAC");
        ClickOk(menu);

        Assert.Equal(["EMEA", "AMER", "APAC"], Assert.IsType<InListCriterion>(applied!.Criterion).Values);
    }

    [Fact]
    public void Reopen_ChecksTheValuesACustomFilterKeeps()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(50d, 150d, 250d);
        AddColumnFilter(sheet, new GreaterThanCriterion { Column = 0, Value = 100d });

        var menu = Render(ctx, sheet);

        Assert.Equal(new Dictionary<string, bool> { ["50"] = false, ["150"] = true, ["250"] = true }, ValueStates(menu));
        Assert.Null(SelectAllState(menu));
    }

    [Fact]
    public void Reopen_ChecksTheValuesACombinedCustomFilterKeeps()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet("Laptops", "Cables", "Mice");
        AddColumnFilter(sheet, new OrCriterion
        {
            Criteria = [new StartsWithCriterion { Column = 0, Value = "La" }, new EndsWithCriterion { Column = 0, Value = "ce" }]
        });

        Assert.Equal(new Dictionary<string, bool> { ["Cables"] = false, ["Laptops"] = true, ["Mice"] = true }, ValueStates(Render(ctx, sheet)));
    }

    [Fact]
    public void Reopen_ChecksTheValuesATopFilterKeeps()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(1d, 2d, 3d, 4d);
        AddColumnFilter(sheet, new TopFilterCriterion { Column = 0, Count = 2 });

        Assert.Equal(new Dictionary<string, bool> { ["1"] = false, ["2"] = false, ["3"] = true, ["4"] = true }, ValueStates(Render(ctx, sheet)));
    }

    [Fact]
    public void Reopen_ChecksTheValuesADynamicFilterKeeps()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(1d, 2d, 9d);
        AddColumnFilter(sheet, new DynamicFilterCriterion { Column = 0, Type = DynamicFilterType.AboveAverage });

        Assert.Equal(new Dictionary<string, bool> { ["1"] = false, ["2"] = false, ["9"] = true }, ValueStates(Render(ctx, sheet)));
    }

    [Fact]
    public void Reopen_ChecksBlankForABlanksFilter()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet("Cables", null);
        AddColumnFilter(sheet, new IsNullCriterion { Column = 0 });

        var states = ValueStates(Render(ctx, sheet));

        Assert.False(states["Cables"]);
        Assert.True(states.Single(state => state.Key.Contains("Blank")).Value);
    }

    [Fact]
    public void Reopen_IgnoresAFilterOnAnotherColumn()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet("EMEA", "AMER");
        sheet.Cells[1, 1].Value = 1d;
        sheet.Cells[2, 1].Value = 5d;
        sheet.AddFilter(new SheetFilter(new GreaterThanCriterion { Column = 1, Value = 2d }, new RangeRef(new CellRef(0, 1), new CellRef(2, 1))));

        var menu = Render(ctx, sheet);

        Assert.Equal(new Dictionary<string, bool> { ["AMER"] = true }, ValueStates(menu));
        Assert.True(SelectAllState(menu));
    }

    [Fact]
    public void Ok_LeavesACustomFilterAloneWhenNothingChanged()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(50d, 150d, 250d);
        var criterion = new GreaterThanCriterion { Column = 0, Value = 100d };
        AddColumnFilter(sheet, criterion);
        var applied = false;
        var cleared = false;

        ClickOk(Render(ctx, sheet, _ => applied = true, clear: () => cleared = true));

        Assert.False(applied);
        Assert.False(cleared);
        Assert.Same(criterion, Assert.Single(sheet.Filters).Criterion);
    }

    [Fact]
    public void Ok_TurnsACustomFilterIntoAValueListWhenTheUserChangesIt()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(50d, 150d, 250d);
        AddColumnFilter(sheet, new GreaterThanCriterion { Column = 0, Value = 100d });
        SheetFilter? applied = null;
        var menu = Render(ctx, sheet, filter => applied = filter);

        Toggle(menu, "250");
        ClickOk(menu);

        Assert.Equal([150d], Assert.IsType<InListCriterion>(applied!.Criterion).Values);
    }

    [Fact]
    public void Reopen_AfterXlsxRoundTrip_ShowsTheFilteredValuesChecked()
    {
        using var ctx = CreateContext();
        var sheet = CreateSheet(1d, 2d, 3d);
        Filter(sheet, 2d);
        using var stream = new MemoryStream();
        sheet.Workbook.SaveToStream(stream);
        stream.Position = 0;
        var loaded = Workbook.LoadFromStream(stream).Sheets[0];

        Assert.Equal(new Dictionary<string, bool> { ["1"] = false, ["2"] = true, ["3"] = false }, ValueStates(Render(ctx, loaded)));
    }
}
