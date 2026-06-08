using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Changing a single formula must re-evaluate only that cell and its transitive dependents,
/// not every formula in the sheet.
/// </summary>
public class FormulaRecalcScopeTests
{
    [Fact]
    public void FormulaChange_DoesNotReevaluateUnrelatedFormulas()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = 1d;       // A1
        sheet.Cells[1, 0].Formula = "=A1";  // A2 depends on A1
        sheet.Cells[5, 0].Value = 10d;      // A6
        sheet.Cells[6, 0].Formula = "=A6";  // A7 depends on A6 - unrelated to A1/A2

        var unrelatedEvals = 0;
        sheet.Cells[6, 0].Changed += _ => unrelatedEvals++;

        sheet.Cells[1, 0].Formula = "=A1*2"; // change A2's formula

        Assert.Equal(0, unrelatedEvals);
    }

    [Fact]
    public void FormulaChange_ReevaluatesChangedCellAndDependents()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = 5d;        // A1
        sheet.Cells[1, 0].Formula = "=A1";   // A2 = 5
        sheet.Cells[2, 0].Formula = "=A2*2"; // A3 = 10, depends on A2

        sheet.Cells[1, 0].Formula = "=A1*10"; // A2 -> 50

        Assert.Equal(50d, sheet.Cells[1, 0].Value);  // changed cell recomputed
        Assert.Equal(100d, sheet.Cells[2, 0].Value); // dependent recomputed
    }

    [Fact]
    public void FormulaChange_PicksUpNewlyReferencedCell()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = 2d; // A1
        sheet.Cells[0, 1].Value = 9d; // B1
        sheet.Cells[1, 0].Formula = "=A1"; // A2 = 2

        sheet.Cells[1, 0].Formula = "=B1"; // now references B1 instead

        Assert.Equal(9d, sheet.Cells[1, 0].Value);
    }
}
