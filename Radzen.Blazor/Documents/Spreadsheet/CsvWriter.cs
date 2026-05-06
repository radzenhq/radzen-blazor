using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>Writes a single <see cref="Worksheet"/> to a stream in CSV format.</summary>
class CsvWriter(Worksheet sheet, CsvExportOptions options)
{
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
        var populated = sheet.Cells.GetPopulatedCells()
            .Where(c => c.Value is not null || !string.IsNullOrEmpty(c.Formula))
            .ToList();

        if (populated.Count == 0) return string.Empty;

        var maxRow = populated.Max(c => c.Address.Row);
        var maxCol = populated.Max(c => c.Address.Column);

        // row -> col -> cell, for O(1) lookups during emission
        var rowMap = populated
            .GroupBy(c => c.Address.Row)
            .ToDictionary(g => g.Key, g => g.ToDictionary(c => c.Address.Column));

        var sb = new StringBuilder();
        for (var r = 0; r <= maxRow; r++)
        {
            rowMap.TryGetValue(r, out var cols);
            for (var c = 0; c <= maxCol; c++)
            {
                if (c > 0) sb.Append(options.Separator);

                var field = cols is not null && cols.TryGetValue(c, out var cell)
                    ? FormatCell(cell)
                    : string.Empty;

                sb.Append(QuoteIfNeeded(field));
            }
            sb.Append(options.LineEnding);
        }

        return sb.ToString();
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
            return Wrap(field);

        if (options.Quoting == CsvQuoting.Never)
            return field;

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
        var q = options.QuoteChar.ToString();
        return q + field.Replace(q, q + q, StringComparison.Ordinal) + q;
    }
}
