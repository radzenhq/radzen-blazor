using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Undoable command that resizes a column to a new width.
/// </summary>
public sealed class ResizeColumnCommand(Worksheet sheet, int column, double oldWidth, double newWidth, bool isAutoFit = false) : ICommand
{
    private bool wasAutoFit;

    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => null;

    /// <inheritdoc/>
    public bool Execute()
    {
        wasAutoFit = sheet.Columns.IsAutoFit(column);

        sheet.Columns[column] = newWidth;

        if (isAutoFit)
        {
            sheet.Columns.SetAutoFit(column);
        }
        else
        {
            sheet.Columns.ClearAutoFit(column);
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.Columns[column] = oldWidth;

        if (wasAutoFit)
        {
            sheet.Columns.SetAutoFit(column);
        }
        else
        {
            sheet.Columns.ClearAutoFit(column);
        }
    }
}

/// <summary>
/// Undoable command that resizes a row to a new height.
/// </summary>
public sealed class ResizeRowCommand(Worksheet sheet, int row, double oldHeight, double newHeight) : ICommand
{
    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => null;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.Rows[row] = newHeight;
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.Rows[row] = oldHeight;
    }
}
