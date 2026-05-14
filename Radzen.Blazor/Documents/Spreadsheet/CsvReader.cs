using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>Reads a CSV stream into a <see cref="Workbook"/> with a single sheet.</summary>
static class CsvReader
{
    public static Workbook Read(Stream stream, CsvImportOptions options)
    {
        using var reader = new StreamReader(stream, options.Encoding, detectEncodingFromByteOrderMarks: true);
        var content = reader.ReadToEnd();

        var rows = ParseCsv(content, options.Separator, options.QuoteChar);

        var rowCount = rows.Count == 0 ? 1 : rows.Count;
        var maxCols = 1;
        foreach (var r in rows)
        {
            if (r.Count > maxCols)
            {
                maxCols = r.Count;
            }
        }

        var wb = new Workbook();
        var sheet = wb.AddSheet(options.SheetName, rowCount, maxCols);

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Count; c++)
            {
                ApplyValue(sheet.Cells[r, c], row[c], options);
            }
        }

        return wb;
    }

    // RFC 4180 parser.
    private static List<List<string>> ParseCsv(string text, char sep, char quote)
    {
        var rows = new List<List<string>>();
        var current = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        void EndField() { current.Add(field.ToString()); field.Clear(); }
        void EndRow() { EndField(); rows.Add(current); current = []; }

        while (i < text.Length)
        {
            var ch = text[i];

            if (inQuotes)
            {
                if (ch == quote)
                {
                    if (i + 1 < text.Length && text[i + 1] == quote)
                    {
                        field.Append(quote);
                        i += 2;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    field.Append(ch);
                    i++;
                }
                continue;
            }

            if (ch == quote && field.Length == 0)
            {
                inQuotes = true;
                i++;
            }
            else if (ch == sep)
            {
                EndField();
                i++;
            }
            else if (ch == '\r')
            {
                EndRow();
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i += 2;
                }
                else
                {
                    i++;
                }
            }
            else if (ch == '\n')
            {
                EndRow();
                i++;
            }
            else
            {
                field.Append(ch);
                i++;
            }
        }

        // Final unterminated row (file doesn't end with a newline).
        if (field.Length > 0 || current.Count > 0)
        {
            EndRow();
        }

        return rows;
    }

    private static void ApplyValue(Cell cell, string raw, CsvImportOptions options)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        if (options.ParseFormulas && raw.Length > 1 && raw[0] == '=')
        {
            cell.Formula = raw;
            return;
        }

        if (options.ParseValues)
        {
            // Value setter goes through CellData, which auto-detects numeric/date/boolean.
            cell.Value = raw;
        }
        else
        {
            // Bypass auto-detection: store as a typed string CellData.
            cell.Data = CellData.FromString(raw);
        }
    }
}
