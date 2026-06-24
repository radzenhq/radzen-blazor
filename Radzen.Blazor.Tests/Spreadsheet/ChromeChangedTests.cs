using System;
using Bunit;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Every decoration mutation (table add/remove, custom cell type, validation add/remove)
/// must raise <see cref="Worksheet.ChromeChanged"/> so subscribed cell views refresh their
/// chrome (menu chevron, table styling, custom renderer). These tests pin that invariant at
/// the model level, plus one render test proving the handler actually re-renders.
/// </summary>
public class ChromeChangedTests
{
    [Fact]
    public void ChromeChanged_Fires_OnAddTable()
    {
        var sheet = new Worksheet(20, 20);
        var fired = false;
        sheet.ChromeChanged += () => fired = true;

        sheet.AddTable("T1", new RangeRef(new CellRef(0, 0), new CellRef(4, 4)));

        Assert.True(fired);
    }

    [Fact]
    public void ChromeChanged_Fires_OnRemoveTable()
    {
        var sheet = new Worksheet(20, 20);
        var table = sheet.AddTable("T1", new RangeRef(new CellRef(0, 0), new CellRef(4, 4)));
        var fired = false;
        sheet.ChromeChanged += () => fired = true;

        sheet.RemoveTable(table);

        Assert.True(fired);
    }

    [Fact]
    public void ChromeChanged_Fires_OnConvertToRange()
    {
        var sheet = new Worksheet(20, 20);
        var table = sheet.AddTable("T1", new RangeRef(new CellRef(0, 0), new CellRef(4, 4)));
        var fired = false;
        sheet.ChromeChanged += () => fired = true;

        table.ConvertToRange();

        Assert.True(fired);
    }

    [Fact]
    public void ChromeChanged_Fires_OnSetCustomType()
    {
        var sheet = new Worksheet(20, 20);
        var fired = false;
        sheet.ChromeChanged += () => fired = true;

        sheet.Cells.SetCustomType(new CellRef(0, 0), "myType");

        Assert.True(fired);
    }

    [Fact]
    public void ChromeChanged_Fires_OnValidationAdd()
    {
        var sheet = new Worksheet(20, 20);
        var fired = false;
        sheet.ChromeChanged += () => fired = true;

        sheet.Validation.Add(new CellRef(0, 0).ToRange(), new DataValidationRule { Type = DataValidationType.List, Formula1 = "A,B,C" });

        Assert.True(fired);
    }

    [Fact]
    public void ChromeChanged_Fires_OnValidationRemove()
    {
        var sheet = new Worksheet(20, 20);
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "A,B,C" };
        sheet.Validation.Add(new CellRef(0, 0).ToRange(), rule);
        var fired = false;
        sheet.ChromeChanged += () => fired = true;

        sheet.Validation.Remove(new CellRef(0, 0).ToRange(), rule);

        Assert.True(fired);
    }

    [Fact]
    public void ChromeChanged_Fires_OnValidationRemoveAll()
    {
        var sheet = new Worksheet(20, 20);
        sheet.Validation.Add(new CellRef(0, 0).ToRange(), new DataValidationRule { Type = DataValidationType.List, Formula1 = "A,B,C" });
        var fired = false;
        sheet.ChromeChanged += () => fired = true;

        sheet.Validation.RemoveAll(new CellRef(0, 0).ToRange());

        Assert.True(fired);
    }

    [Fact]
    public void ChromeChanged_FiresOnce_OnParamsAddOfSingleValidator()
    {
        var sheet = new Worksheet(20, 20);
        var count = 0;
        sheet.ChromeChanged += () => count++;

        sheet.Validation.Add(new CellRef(0, 0).ToRange(), new ICellValidator[] { new DataValidationRule { Type = DataValidationType.List, Formula1 = "A,B,C" } });

        Assert.Equal(1, count);
    }

    [Fact]
    public void ValidationStore_BareConstruction_DoesNotRaiseOrThrow()
    {
        var store = new ValidationStore();
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "A,B,C" };

        store.Add(new CellRef(0, 0).ToRange(), rule);
        Assert.True(store.Remove(new CellRef(0, 0).ToRange(), rule));
    }

    [Fact]
    public void GetValidatorsForCell_ReturnsSharedEmpty_WhenNoMatch()
    {
        var store = new ValidationStore();

        var a = store.GetValidatorsForCell(new CellRef(0, 0));
        var b = store.GetValidatorsForCell(new CellRef(3, 3));

        Assert.Empty(a);
        Assert.Same(a, b);
    }

    [Fact]
    public void GetValidatorsForCell_ReturnsMatches_WhenRangeContainsCell()
    {
        var store = new ValidationStore();
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "A,B,C" };
        store.Add(new RangeRef(new CellRef(0, 0), new CellRef(5, 5)), rule);

        var result = store.GetValidatorsForCell(new CellRef(2, 2));

        Assert.Single(result);
        Assert.Same(rule, result[0]);
    }
}

/// <summary>
/// Verifies the renamed <c>OnChromeChanged</c> handler in <see cref="CellView"/> actually
/// re-renders: adding list validation at the cell's position must surface the menu chevron
/// without any position change, and removing it must hide the chevron again.
/// </summary>
public class ChromeChangedRenderTests : TestContext
{
    private const string ChevronClass = "rz-spreadsheet-cell-chevron";

    [Fact]
    public async System.Threading.Tasks.Task CellView_ShowsChevron_AfterListValidationAdded_AndHidesAfterRemoveAll()
    {
        var sheet = new Worksheet(20, 20);
        var cell = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));

        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 0)
            .Add(c => c.Column, 0));

        Assert.DoesNotContain(ChevronClass, cut.Markup);

        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "Yes,No,Maybe" };
        await cut.InvokeAsync(() => sheet.Validation.Add(cell, rule));
        Assert.Contains(ChevronClass, cut.Markup);

        await cut.InvokeAsync(() => sheet.Validation.RemoveAll(cell));
        Assert.DoesNotContain(ChevronClass, cut.Markup);
    }
}
