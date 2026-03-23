using System;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class AutofillCommandTests
{
    // ── Numeric series ───────────────────────────────────────────────────

    [Fact]
    public void FillDown_NumericSeries()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;
        sheet.Cells[1, 0].Value = 2.0;

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(4, 0));
        var command = new AutofillCommand(sheet, source, target, AutofillDirection.Down);

        Assert.True(command.Execute());
        Assert.Equal(3.0, sheet.Cells[2, 0].Value);
        Assert.Equal(4.0, sheet.Cells[3, 0].Value);
        Assert.Equal(5.0, sheet.Cells[4, 0].Value);
    }

    [Fact]
    public void FillDown_IntegerInput_ProducesDoubles()
    {
        // CellData converts int to double internally, so series output is double
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 20;

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(3, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal(30.0, sheet.Cells[2, 0].Value);
        Assert.Equal(40.0, sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void FillDown_StepOfTwo()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 2.0;
        sheet.Cells[1, 0].Value = 4.0;

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(3, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal(6.0, sheet.Cells[2, 0].Value);
        Assert.Equal(8.0, sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void FillUp_NumericSeries()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[3, 0].Value = 1.0;
        sheet.Cells[4, 0].Value = 2.0;

        var source = new RangeRef(new CellRef(3, 0), new CellRef(4, 0));
        var target = new RangeRef(new CellRef(1, 0), new CellRef(4, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Up).Execute();

        Assert.Equal(0.0, sheet.Cells[2, 0].Value);
        Assert.Equal(-1.0, sheet.Cells[1, 0].Value);
    }

    [Fact]
    public void FillRight_NumericSeries()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;
        sheet.Cells[0, 1].Value = 3.0;

        var source = new RangeRef(new CellRef(0, 0), new CellRef(0, 1));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(0, 4));

        new AutofillCommand(sheet, source, target, AutofillDirection.Right).Execute();

        Assert.Equal(5.0, sheet.Cells[0, 2].Value);
        Assert.Equal(7.0, sheet.Cells[0, 3].Value);
        Assert.Equal(9.0, sheet.Cells[0, 4].Value);
    }

    [Fact]
    public void FillLeft_NumericSeries()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 3].Value = 1.0;
        sheet.Cells[0, 4].Value = 2.0;

        var source = new RangeRef(new CellRef(0, 3), new CellRef(0, 4));
        var target = new RangeRef(new CellRef(0, 1), new CellRef(0, 4));

        new AutofillCommand(sheet, source, target, AutofillDirection.Left).Execute();

        Assert.Equal(0.0, sheet.Cells[0, 2].Value);
        Assert.Equal(-1.0, sheet.Cells[0, 1].Value);
    }

    // ── Date series ──────────────────────────────────────────────────────

    [Fact]
    public void FillDown_DateSeries()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = new DateTime(2024, 1, 1);
        sheet.Cells[1, 0].Value = new DateTime(2024, 1, 2);

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(3, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal(new DateTime(2024, 1, 3), sheet.Cells[2, 0].Value);
        Assert.Equal(new DateTime(2024, 1, 4), sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void FillDown_WeeklyDateSeries()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = new DateTime(2024, 1, 1);
        sheet.Cells[1, 0].Value = new DateTime(2024, 1, 8);

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(3, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal(new DateTime(2024, 1, 15), sheet.Cells[2, 0].Value);
        Assert.Equal(new DateTime(2024, 1, 22), sheet.Cells[3, 0].Value);
    }

    // ── Formula adjustment ───────────────────────────────────────────────

    [Fact]
    public void FillDown_AdjustsFormulas()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;
        sheet.Cells[0, 1].Formula = "=A1+1";

        var source = new RangeRef(new CellRef(0, 1), new CellRef(0, 1));
        var target = new RangeRef(new CellRef(0, 1), new CellRef(2, 1));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal("=A2+1", sheet.Cells[1, 1].Formula);
        Assert.Equal("=A3+1", sheet.Cells[2, 1].Formula);
    }

    [Fact]
    public void FillRight_AdjustsFormulas()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;
        sheet.Cells[0, 1].Formula = "=A1*2";

        var source = new RangeRef(new CellRef(0, 1), new CellRef(0, 1));
        var target = new RangeRef(new CellRef(0, 1), new CellRef(0, 3));

        new AutofillCommand(sheet, source, target, AutofillDirection.Right).Execute();

        Assert.Equal("=B1*2", sheet.Cells[0, 2].Formula);
        Assert.Equal("=C1*2", sheet.Cells[0, 3].Formula);
    }

    [Fact]
    public void FillDown_PreservesAbsoluteRefs()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 100.0;
        sheet.Cells[0, 1].Formula = "=$A$1+1";

        var source = new RangeRef(new CellRef(0, 1), new CellRef(0, 1));
        var target = new RangeRef(new CellRef(0, 1), new CellRef(1, 1));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal("=$A$1+1", sheet.Cells[1, 1].Formula);
    }

    // ── Plain copy (fallback) ────────────────────────────────────────────

    [Fact]
    public void FillDown_SingleValue_Copies()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "hello";

        var source = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(2, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal("hello", sheet.Cells[1, 0].Value);
        Assert.Equal("hello", sheet.Cells[2, 0].Value);
    }

    [Fact]
    public void FillDown_MixedTypes_Copies()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(5, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal("A", sheet.Cells[2, 0].Value);
        Assert.Equal("B", sheet.Cells[3, 0].Value);
        Assert.Equal("A", sheet.Cells[4, 0].Value);
        Assert.Equal("B", sheet.Cells[5, 0].Value);
    }

    // ── Multi-column/row fill ────────────────────────────────────────────

    [Fact]
    public void FillDown_MultipleColumns()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;
        sheet.Cells[0, 1].Value = "X";
        sheet.Cells[1, 0].Value = 2.0;
        sheet.Cells[1, 1].Value = "X";

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(3, 1));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        // Column 0: numeric series
        Assert.Equal(3.0, sheet.Cells[2, 0].Value);
        Assert.Equal(4.0, sheet.Cells[3, 0].Value);
        // Column 1: text copy
        Assert.Equal("X", sheet.Cells[2, 1].Value);
        Assert.Equal("X", sheet.Cells[3, 1].Value);
    }

    [Fact]
    public void FillRight_MultipleRows()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 10.0;
        sheet.Cells[0, 1].Value = 20.0;
        sheet.Cells[1, 0].Value = "Y";
        sheet.Cells[1, 1].Value = "Y";

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(1, 3));

        new AutofillCommand(sheet, source, target, AutofillDirection.Right).Execute();

        Assert.Equal(30.0, sheet.Cells[0, 2].Value);
        Assert.Equal(40.0, sheet.Cells[0, 3].Value);
        Assert.Equal("Y", sheet.Cells[1, 2].Value);
        Assert.Equal("Y", sheet.Cells[1, 3].Value);
    }

    // ── Format copy ──────────────────────────────────────────────────────

    [Fact]
    public void FillDown_CopiesFormat()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;
        sheet.Cells[0, 0].Format.Bold = true;

        var source = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(1, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.True(sheet.Cells[1, 0].Format.Bold);
    }

    // ── Undo ─────────────────────────────────────────────────────────────

    [Fact]
    public void Unexecute_RestoresOriginalValues()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;
        sheet.Cells[1, 0].Value = 2.0;
        sheet.Cells[2, 0].Value = "original";

        var source = new RangeRef(new CellRef(0, 0), new CellRef(1, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(3, 0));
        var command = new AutofillCommand(sheet, source, target, AutofillDirection.Down);

        command.Execute();
        Assert.Equal(3.0, sheet.Cells[2, 0].Value);

        command.Unexecute();
        Assert.Equal("original", sheet.Cells[2, 0].Value);
        Assert.Null(sheet.Cells[3, 0].Value);
    }

    [Fact]
    public void Unexecute_RestoresFormulas()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;
        sheet.Cells[1, 0].Formula = "=A1+1";
        sheet.Cells[2, 0].Formula = "=A1*3";

        var source = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(2, 0));
        var command = new AutofillCommand(sheet, source, target, AutofillDirection.Down);

        command.Execute();

        command.Unexecute();
        Assert.Equal("=A1+1", sheet.Cells[1, 0].Formula);
        Assert.Equal("=A1*3", sheet.Cells[2, 0].Formula);
    }

    // ── Edge cases ───────────────────────────────────────────────────────

    [Fact]
    public void Execute_InvalidSource_ReturnsFalse()
    {
        var sheet = new Worksheet(10, 10);
        var command = new AutofillCommand(sheet, RangeRef.Invalid, RangeRef.Invalid, AutofillDirection.Down);
        Assert.False(command.Execute());
    }

    [Fact]
    public void Execute_TargetEqualsSource_ReturnsFalse()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 1.0;

        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 0));
        var command = new AutofillCommand(sheet, range, range, AutofillDirection.Down);
        Assert.False(command.Execute());
    }

    [Fact]
    public void FillDown_ThreeValueSeries()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 5.0;
        sheet.Cells[1, 0].Value = 10.0;
        sheet.Cells[2, 0].Value = 15.0;

        var source = new RangeRef(new CellRef(0, 0), new CellRef(2, 0));
        var target = new RangeRef(new CellRef(0, 0), new CellRef(4, 0));

        new AutofillCommand(sheet, source, target, AutofillDirection.Down).Execute();

        Assert.Equal(20.0, sheet.Cells[3, 0].Value);
        Assert.Equal(25.0, sheet.Cells[4, 0].Value);
    }
}
