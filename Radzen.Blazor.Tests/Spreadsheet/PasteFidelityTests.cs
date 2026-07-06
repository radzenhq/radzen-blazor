using Xunit;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Excel-parity cell transfer: paste, cut and autofill carry the full cell state
// (value, formula, format, quote prefix, hyperlink) and undo/redo restore it symmetrically.
public class PasteFidelityTests
{
    private static (Worksheet sheet, SpreadsheetClipboard clipboard, UndoRedoStack stack) Setup()
    {
        var sheet = new Worksheet(20, 5);
        return (sheet, new SpreadsheetClipboard(), new UndoRedoStack(sheet));
    }

    private static string CopyRange(Worksheet sheet, SpreadsheetClipboard clipboard, string range)
    {
        sheet.Selection.Select(RangeRef.Parse(range));
        clipboard.Copy(sheet);
        return sheet.GetDelimitedString(RangeRef.Parse(range));
    }

    private static string CutRange(Worksheet sheet, SpreadsheetClipboard clipboard, string range)
    {
        sheet.Selection.Select(RangeRef.Parse(range));
        clipboard.Cut(sheet);
        return sheet.GetDelimitedString(RangeRef.Parse(range));
    }

    [Fact]
    public void Paste_CopiesFormatQuotePrefixAndHyperlink()
    {
        var (sheet, clipboard, stack) = Setup();
        var src = sheet.Cells[0, 0];
        src.SetValue("'0123");
        src.Format.Bold = true;
        src.Format.BackgroundColor = "#FF0000";
        src.Hyperlink = new Hyperlink { Url = "https://radzen.com", Text = "Radzen" };

        var text = CopyRange(sheet, clipboard, "A1:A1");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));

        var dst = sheet.Cells[0, 2];
        Assert.Equal("0123", dst.Value);
        Assert.True(dst.QuotePrefix);
        Assert.True(dst.FormatOrNull?.Bold);
        Assert.Equal("#FF0000", dst.FormatOrNull?.BackgroundColor);
        Assert.Equal("https://radzen.com", dst.Hyperlink?.Url);
    }

    [Fact]
    public void Paste_ValueOverFormulaCell_ClearsFormula()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 0].Value = 5d;
        sheet.Cells[0, 2].Formula = "=A1*2";

        var text = CopyRange(sheet, clipboard, "A1:A1");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));

        Assert.Null(sheet.Cells[0, 2].Formula);
        Assert.Equal(5d, sheet.Cells[0, 2].Value);

        sheet.Cells[0, 0].Value = 9d; // recalc must not overwrite the pasted value
        Assert.Equal(5d, sheet.Cells[0, 2].Value);
    }

    [Fact]
    public void Paste_UnformattedSource_ClearsDestinationFormat()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 0].Value = "plain";
        sheet.Cells[0, 2].Value = "styled";
        sheet.Cells[0, 2].Format.Bold = true;

        var text = CopyRange(sheet, clipboard, "A1:A1");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));

        Assert.Equal("plain", sheet.Cells[0, 2].Value);
        Assert.Null(sheet.Cells[0, 2].FormatOrNull);
    }

    [Fact]
    public void Paste_UnpopulatedSourceCell_BlanksPopulatedDestination()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 0].Value = "A";
        // A2 stays unpopulated
        sheet.Cells[0, 2].Value = "old";
        sheet.Cells[1, 2].Value = "stale";
        sheet.Cells[1, 2].Format.Bold = true;
        sheet.Cells[1, 2].Hyperlink = new Hyperlink { Url = "https://x" };

        var text = CopyRange(sheet, clipboard, "A1:A2");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));

        Assert.Equal("A", sheet.Cells[0, 2].Value);
        Assert.Null(sheet.Cells[1, 2].Value);
        Assert.Null(sheet.Cells[1, 2].FormatOrNull);
        Assert.Null(sheet.Cells[1, 2].Hyperlink);
    }

    [Fact]
    public void Paste_UnpopulatedSourceOverUnpopulatedDestination_DoesNotMaterialize()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 0].Value = "A";

        var text = CopyRange(sheet, clipboard, "A1:A2");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));

        Assert.False(sheet.Cells.TryGet(1, 2, out _));
    }

    [Fact]
    public void TiledPaste_CopiesFormatsPerTile()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 0].Value = "X";
        sheet.Cells[0, 0].Format.Bold = true;

        var text = CopyRange(sheet, clipboard, "A1:A1");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C3"), text));

        for (var row = 0; row <= 2; row++)
        {
            Assert.Equal("X", sheet.Cells[row, 2].Value);
            Assert.True(sheet.Cells[row, 2].FormatOrNull?.Bold);
        }
    }

    [Fact]
    public void Cut_ClearsSourceAndTransfersEverything()
    {
        var (sheet, clipboard, stack) = Setup();
        var src = sheet.Cells[0, 0];
        src.SetValue("'007");
        src.Format.Bold = true;
        src.Hyperlink = new Hyperlink { Url = "https://radzen.com" };

        var text = CutRange(sheet, clipboard, "A1:A1");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));

        Assert.Null(src.Value);
        Assert.Null(src.FormatOrNull);
        Assert.False(src.QuotePrefix);
        Assert.Null(src.Hyperlink);

        var dst = sheet.Cells[0, 2];
        Assert.Equal("007", dst.Value);
        Assert.True(dst.QuotePrefix);
        Assert.True(dst.FormatOrNull?.Bold);
        Assert.Equal("https://radzen.com", dst.Hyperlink?.Url);
    }

    [Fact]
    public void SetValue_PlainValueOnFormulaCell_ClearsFormula()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = 2d;
        sheet.Cells[0, 1].Formula = "=A1*3";
        Assert.Equal(6d, sheet.Cells[0, 1].Value);

        sheet.Cells[0, 1].SetValue("42");

        Assert.Null(sheet.Cells[0, 1].Formula);
        Assert.Equal(42d, sheet.Cells[0, 1].Value);

        sheet.Cells[0, 0].Value = 10d; // recalc must not resurrect the formula
        Assert.Equal(42d, sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void SetValue_QuotePrefix_StoresLiteralTextWithoutTypeInference()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].SetValue("'0123");

        Assert.Equal("0123", sheet.Cells[0, 0].Value);
        Assert.Equal(CellDataType.String, sheet.Cells[0, 0].ValueType);
        Assert.True(sheet.Cells[0, 0].QuotePrefix);
        Assert.Equal("'0123", sheet.Cells[0, 0].GetValue());
    }

    [Fact]
    public void TypedString_SurvivesCaptureAndUndo()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 2].Data = CellData.FromString("0123"); // explicitly String-typed, like CsvReader with ParseValues=false
        sheet.Cells[0, 0].Value = "other";

        var text = CopyRange(sheet, clipboard, "A1:A1");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));
        stack.Undo();

        Assert.Equal("0123", sheet.Cells[0, 2].Value);
        Assert.Equal(CellDataType.String, sheet.Cells[0, 2].ValueType);
    }

    [Fact]
    public void Undo_PasteFormulaOverEmpty_LeavesCellEmpty()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 0].Value = 1d;
        sheet.Cells[1, 0].Value = 2d;
        sheet.Cells[2, 0].Formula = "=SUM(A1:A2)";

        var text = CopyRange(sheet, clipboard, "A3:A3");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C3:C3"), text));
        Assert.Equal("=SUM(C1:C2)", sheet.Cells[2, 2].Formula);

        stack.Undo();

        Assert.Null(sheet.Cells[2, 2].Formula);
        Assert.Null(sheet.Cells[2, 2].Value);
    }

    [Fact]
    public void Undo_PasteFormat_RestoresPriorState()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 0].Value = "X";
        sheet.Cells[0, 0].Format.Bold = true;
        sheet.Cells[0, 2].Value = "plain"; // populated, unformatted

        var text = CopyRange(sheet, clipboard, "A1:A2");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));
        Assert.True(sheet.Cells[0, 2].FormatOrNull?.Bold);

        stack.Undo();

        Assert.Equal("plain", sheet.Cells[0, 2].Value);
        Assert.Null(sheet.Cells[0, 2].FormatOrNull);
        Assert.False(sheet.Cells.TryGet(1, 2, out _)); // unpopulated destination stays unmaterialized
    }

    [Fact]
    public void RedoUndo_SnapshotIsolatedFromLaterEdits()
    {
        var (sheet, clipboard, stack) = Setup();
        sheet.Cells[0, 0].Value = "X";
        sheet.Cells[0, 0].Format.Bold = true;
        sheet.Cells[0, 2].Value = "orig";
        sheet.Cells[0, 2].Format.Italic = true;

        var text = CopyRange(sheet, clipboard, "A1:A1");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));
        stack.Undo();

        sheet.Cells[0, 2].Format.Underline = true; // must not leak into any snapshot

        stack.Redo();
        Assert.True(sheet.Cells[0, 2].FormatOrNull?.Bold);
        Assert.False(sheet.Cells[0, 2].FormatOrNull?.Underline);

        stack.Undo();
        Assert.True(sheet.Cells[0, 2].FormatOrNull?.Italic);
        Assert.False(sheet.Cells[0, 2].FormatOrNull?.Bold);
    }

    [Fact]
    public void CutUndoRedo_RestoresAndReclearsSource()
    {
        var (sheet, clipboard, stack) = Setup();
        var src = sheet.Cells[0, 0];
        src.SetValue("'A");
        src.Format.Bold = true;
        src.Hyperlink = new Hyperlink { Url = "https://x" };

        var text = CutRange(sheet, clipboard, "A1:A1");
        stack.Execute(new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text));

        stack.Undo();
        Assert.Equal("A", src.Value);
        Assert.True(src.QuotePrefix);
        Assert.True(src.FormatOrNull?.Bold);
        Assert.Equal("https://x", src.Hyperlink?.Url);

        stack.Redo();
        Assert.Null(src.Value);
        Assert.Null(src.FormatOrNull);
        Assert.Null(src.Hyperlink);
        Assert.Equal("A", sheet.Cells[0, 2].Value);
    }

    [Fact]
    public void MergeUndo_OnUnpopulatedTopLeft_LeavesNoFormat()
    {
        var sheet = new Worksheet(20, 5);
        var stack = new UndoRedoStack(sheet);

        stack.Execute(new MergeCellsCommand(sheet, RangeRef.Parse("C1:D2"), center: true));
        Assert.Equal(TextAlign.Center, sheet.Cells[0, 2].FormatOrNull?.TextAlign);

        stack.Undo();

        Assert.False(sheet.Cells.TryGet(0, 2, out var cell) && cell.FormatOrNull is not null);
    }

    [Fact]
    public void SortUndo_RestoresQuotePrefixHyperlinkAndEmptiness()
    {
        var sheet = new Worksheet(20, 5);
        var stack = new UndoRedoStack(sheet);
        sheet.Cells[0, 0].SetValue("'B");
        sheet.Cells[0, 0].Hyperlink = new Hyperlink { Url = "https://b" };
        sheet.Cells[1, 0].SetValue("'A");
        // A3 unpopulated inside the sort range

        stack.Execute(new SortCommand(sheet, RangeRef.Parse("A1:A3"), SortOrder.Ascending, 0));
        Assert.Equal("A", sheet.Cells[0, 0].Value);

        stack.Undo();

        Assert.Equal("B", sheet.Cells[0, 0].Value);
        Assert.True(sheet.Cells[0, 0].QuotePrefix);
        Assert.Equal("https://b", sheet.Cells[0, 0].Hyperlink?.Url);
        Assert.True(!sheet.Cells.TryGet(2, 0, out var third) || third.IsEmpty);
    }

    [Fact]
    public void Autofill_FromUnformattedAndUnpopulatedSources_ClearsDestinationFormat()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = "plain"; // populated, unformatted
        // A2 unpopulated
        sheet.Cells[2, 0].Value = "old";
        sheet.Cells[2, 0].Format.Bold = true;
        sheet.Cells[3, 0].Value = "older";
        sheet.Cells[3, 0].Format.Bold = true;

        new AutofillCommand(sheet, RangeRef.Parse("A1:A2"), RangeRef.Parse("A1:A4"), AutofillDirection.Down).Execute();

        Assert.Null(sheet.Cells[2, 0].FormatOrNull);
        Assert.Null(sheet.Cells[3, 0].FormatOrNull);
    }

    [Fact]
    public void AutofillUndo_RemovesCopiedFormats()
    {
        var sheet = new Worksheet(20, 5);
        var stack = new UndoRedoStack(sheet);
        sheet.Cells[0, 0].Value = 1d;
        sheet.Cells[0, 0].Format.Bold = true;

        stack.Execute(new AutofillCommand(sheet, RangeRef.Parse("A1:A1"), RangeRef.Parse("A1:A3"), AutofillDirection.Down));
        Assert.True(sheet.Cells[2, 0].FormatOrNull?.Bold);

        stack.Undo();

        Assert.True(!sheet.Cells.TryGet(2, 0, out var cell) || cell.FormatOrNull is null);
    }

    [Fact]
    public void UnbatchedUnexecute_RecalculatesDependents()
    {
        var (sheet, clipboard, _) = Setup();
        sheet.Cells[0, 0].Value = 7d;
        sheet.Cells[0, 2].Value = 1d;
        sheet.Cells[5, 0].Formula = "=C1*10";

        var text = CopyRange(sheet, clipboard, "A1:A1");
        var command = new PasteCommand(clipboard, sheet, RangeRef.Parse("C1:C1"), text);
        command.Execute();
        Assert.Equal(70d, sheet.Cells[5, 0].Value);

        command.Unexecute(); // direct call, no UndoRedoStack batch

        Assert.Equal(1d, sheet.Cells[0, 2].Value);
        Assert.Equal(10d, sheet.Cells[5, 0].Value);
    }
}
