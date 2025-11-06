using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a table in a Markdown document.
/// </summary>
public class Table : Leaf
{
    private readonly List<TableRow> rows = [];

    /// <summary>
    /// Gets the rows of the table.
    /// </summary>
    public IReadOnlyList<TableRow> Rows => rows;

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitTable(this);
    }

    private static readonly Regex DelimiterRegex = new(@"^\s*(\|?\s*:?-{1,}:?\s*)+(\|+\s*:?-{1,}:?\s*)*\|?\s*$");

    internal override void Close(BlockParser parser)
    {
        base.Close(parser);

        var header = rows[0];
        var headerCells = header.Cells;

        var dataLines = Value.Split('\n');

        foreach (var line in dataLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var row = new TableRow();
                var cells = ParseRow(line);

                // Trim excess cells
                var count = Math.Min(cells.Count, headerCells.Count);

                for (int cellIndex = 0; cellIndex < count; cellIndex++)
                {
                    var alignment = cellIndex < headerCells.Count ? headerCells[cellIndex].Alignment : TableCellAlignment.None;

                    row.Add(cells[cellIndex], alignment);
                }

                for (int missingCellIndex = 0; missingCellIndex < header.Cells.Count - cells.Count; missingCellIndex++)
                {
                    row.Add("", TableCellAlignment.None);
                }

                rows.Add(row);
            }
        }
    }

    private static List<string> ParseRow(string line)
    {
        // Remove leading and trailing pipes if present
        line = line.Trim();

        if (line.StartsWith('|'))
        {
            line = line[1..];
        }
        if (line.EndsWith('|'))
        {
            line = line[..^1];
        }

        // Split by pipe character, but not by escaped pipes
        var cells = new List<string>();
        var currentCell = "";
        var escaped = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (escaped)
            {
                // Add the escaped character (including escaped pipes)
                if (c == '|')
                {
                    currentCell += '|'; // Replace \| with |
                }
                else
                {
                    currentCell += $"\\{c}"; // Keep the escape character for other escaped chars
                }
                escaped = false;
            }
            else if (c == '\\')
            {
                escaped = true;
            }
            else if (c == '|')
            {
                // End of cell
                cells.Add(currentCell.Trim());
                currentCell = "";
            }
            else
            {
                currentCell += c;
            }
        }

        // Add the last cell
        if (!string.IsNullOrEmpty(currentCell) || cells.Count > 0)
        {
            cells.Add(currentCell.Trim());
        }

        return cells;
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        return parser.Blank ? BlockMatch.Skip : BlockMatch.Match;
    }

    internal static BlockStart Start(BlockParser parser, Block block)
    {
        if (parser.Indented || block is not Paragraph paragraph)
        {
            return BlockStart.Skip;
        }

        var line = parser.CurrentLine[parser.NextNonSpace..];

        // Check if the line contains a pipe character to be more specific about table delimiters
        // This helps avoid misinterpreting heading delimiters as table delimiters
        if (!line.Contains('|') && !line.Contains(':'))
        {
            return BlockStart.Skip;
        }

        var match = DelimiterRegex.Match(line);

        if (match.Success)
        {
            // Parse the delimiter row to determine alignments
            var delimiterRow = line.Trim();

            // Parse the header row from the paragraph text
            var headerLine = paragraph.Value.Trim();

            // Parse header cells and delimiter cells
            var headerCells = ParseRow(headerLine);

            // Parse delimiter cells to determine alignments
            var cleanDelimiterRow = delimiterRow;
            if (cleanDelimiterRow.StartsWith('|'))
            {
                cleanDelimiterRow = cleanDelimiterRow[1..];
            }
            if (cleanDelimiterRow.EndsWith('|'))
            {
                cleanDelimiterRow = cleanDelimiterRow[..^1];
            }

            // Split by pipe character
            var delimiters = cleanDelimiterRow.Split('|');

            // If the number of header cells and delimiter cells don't match, don't create a table
            if (headerCells.Count != delimiters.Length)
            {
                return BlockStart.Skip;
            }

            // Initialize alignments list
            var alignments = new List<TableCellAlignment>(headerCells.Count);
            for (int i = 0; i < headerCells.Count; i++)
            {
                alignments.Add(TableCellAlignment.None);
            }

            // Determine alignments from delimiters
            for (var i = 0; i < delimiters.Length && i < alignments.Count; i++)
            {
                var trimmed = delimiters[i].Trim();

                if (trimmed.StartsWith(':') && trimmed.EndsWith(':'))
                {
                    alignments[i] = TableCellAlignment.Center;
                }
                else if (trimmed.EndsWith(':'))
                {
                    alignments[i] = TableCellAlignment.Right;
                }
                else if (trimmed.StartsWith(':'))
                {
                    alignments[i] = TableCellAlignment.Left;
                }
            }

            parser.CloseUnmatchedBlocks();

            // resolve reference links
            while (paragraph.Value.Peek() == '[' && parser.TryParseLinkReference(paragraph.Value, out var position))
            {
                paragraph.Value = paragraph.Value[position..];
            }

            if (paragraph.Value.Length > 0)
            {
                var table = new Table();

                // Create header row
                var header = new TableHeaderRow();
                table.rows.Add(header);

                // Add header cells with alignments
                for (int cellindex = 0; cellindex < headerCells.Count; cellindex++)
                {
                    var alignment = cellindex < alignments.Count ? alignments[cellindex] : TableCellAlignment.None;
                    header.Add(headerCells[cellindex], alignment);
                }

                paragraph.Parent.Replace(paragraph, table);
                parser.Tip = table;
                parser.AdvanceOffset(parser.CurrentLine.Length - parser.Offset, false);

                return BlockStart.Leaf;
            }
        }

        return BlockStart.Skip;
    }
}
