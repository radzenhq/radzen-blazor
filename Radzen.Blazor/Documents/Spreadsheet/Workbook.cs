using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a workbook in a spreadsheet.
/// </summary>
public class Workbook
{
    private readonly List<Worksheet> sheets = [];

    /// <summary>
    /// Gets the collection of sheets in the workbook.
    /// </summary>
    public IReadOnlyList<Worksheet> Sheets => sheets;

    internal Workbook(Worksheet sheet)
    {
        AddSheet(sheet);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Workbook"/> class.
    /// </summary>
    public Workbook()
    {
    }

    /// <summary>
    /// Adds a new sheet to the workbook with the specified name, rows, and columns.
    /// </summary>
    public Worksheet AddSheet(string name, int rows, int columns)
    {
        var sheet = new Worksheet(rows, columns)
        {
            Name = name
        };
        AddSheet(sheet);
        return sheet;
    }

    /// <summary>
    /// Adds an existing sheet to the workbook.
    /// </summary>
    /// <param name="sheet"></param>
    public void AddSheet(Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        sheets.Add(sheet);
        sheet.Workbook = this;
    }

    /// <summary>
    /// Gets the sheet with the specified name or null if not found.
    /// </summary>
    /// <param name="name"></param>
    public Worksheet? GetSheet(string name)
    {
        foreach (var sheet in sheets)
        {
            if (string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return sheet;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes a sheet from the workbook.
    /// </summary>
    /// <param name="sheet">The sheet to remove.</param>
    /// <returns><c>true</c> if the sheet was removed; otherwise <c>false</c>.</returns>
    public bool RemoveSheet(Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        return sheets.Remove(sheet);
    }

    /// <summary>
    /// Gets the index of the specified sheet in the workbook.
    /// </summary>
    /// <param name="sheet">The sheet to find.</param>
    /// <returns>The zero-based index of the sheet, or -1 if not found.</returns>
    public int IndexOf(Worksheet sheet)
    {
        return sheets.IndexOf(sheet);
    }

    /// <summary>
    /// Moves a sheet from one position to another.
    /// </summary>
    /// <param name="fromIndex">The current index of the sheet.</param>
    /// <param name="toIndex">The target index for the sheet.</param>
    public void MoveSheet(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= sheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(fromIndex));
        }

        if (toIndex < 0 || toIndex >= sheets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        }

        var sheet = sheets[fromIndex];
        sheets.RemoveAt(fromIndex);
        sheets.Insert(toIndex, sheet);
    }

    /// <summary>
    /// Saves the workbook to the specified stream in the Open XML Spreadsheet format (XLSX).
    /// </summary>
    /// <param name="stream"></param>
    public void SaveToStream(Stream stream)
    {
        new XlsxWriter(this).Write(stream);
    }

    /// <summary>
    /// Loads a workbook from the specified stream in the Open XML Spreadsheet format (XLSX).
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static Workbook LoadFromStream(Stream stream)
    {
        return XlsxReader.Read(stream);
    }
}
