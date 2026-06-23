using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

class CsvWriter(Worksheet sheet, CsvExportOptions options)
{
    private readonly string quote = options.QuoteChar.ToString();
    private readonly string doubleQuote = new string(options.QuoteChar, 2);

    public void Write(Stream stream)
    {
        // Write the encoding's BOM (if any) once, manually. Encoding.GetBytes does not
        // emit a BOM by itself, and using StreamWriter would double-emit.
        var preamble = options.Encoding.GetPreamble();
        if (preamble.Length > 0)
        {
            stream.Write(preamble, 0, preamble.Length);
        }

        var content = BuildContent();
        var bytes = options.Encoding.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private string BuildContent()
    {
        var cells = new Dictionary<(int Row, int Column), Cell>();
        var maxRow = -1;
        var maxCol = -1;
        foreach (var cell in sheet.Cells.GetPopulatedCells())
        {
            if (cell.Value is null && string.IsNullOrEmpty(cell.Formula))
            {
                continue;
            }
            cells[(cell.Address.Row, cell.Address.Column)] = cell;
            if (cell.Address.Row > maxRow)
            {
                maxRow = cell.Address.Row;
            }
            if (cell.Address.Column > maxCol)
            {
                maxCol = cell.Address.Column;
            }
        }

        if (cells.Count == 0)
        {
            return string.Empty;
        }

        var sb = StringBuilderCache.Acquire();
        for (var r = 0; r <= maxRow; r++)
        {
            for (var c = 0; c <= maxCol; c++)
            {
                if (c > 0)
                {
                    sb.Append(options.Separator);
                }

                var field = cells.TryGetValue((r, c), out var cell)
                    ? FormatCell(cell)
                    : string.Empty;

                sb.Append(QuoteIfNeeded(field));
            }
            sb.Append(options.LineEnding);
        }

        return StringBuilderCache.GetStringAndRelease(sb);
    }

    private static string FormatCell(Cell cell)
    {
        // Formula cells emit the cached evaluated value, never the formula text.
        // The XLSX writer applies the same rule.
        var value = cell.Value;
        return value switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "TRUE" : "FALSE",
            DateTime d => d.TimeOfDay == TimeSpan.Zero
                ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : d.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            CellError err => CellErrorToCsvString(err),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static string CellErrorToCsvString(CellError error) => error switch
    {
        CellError.Value => "#VALUE!",
        CellError.Div0  => "#DIV/0!",
        CellError.Ref   => "#REF!",
        CellError.Name  => "#NAME?",
        CellError.Num   => "#NUM!",
        CellError.NA    => "#N/A",
        _               => "#NAME?",
    };

    private string QuoteIfNeeded(string field)
    {
        if (options.Quoting == CsvQuoting.Always)
        {
            return Wrap(field);
        }

        if (options.Quoting == CsvQuoting.Never)
        {
            return field;
        }

        // Minimal: quote only when necessary (RFC 4180).
        var needsQuote = false;
        for (var i = 0; i < field.Length; i++)
        {
            var ch = field[i];
            if (ch == options.Separator || ch == options.QuoteChar || ch == '\r' || ch == '\n')
            {
                needsQuote = true;
                break;
            }
        }

        return needsQuote ? Wrap(field) : field;
    }

    private string Wrap(string field)
    {
        return quote + field.Replace(quote, doubleQuote, StringComparison.Ordinal) + quote;
    }
}
