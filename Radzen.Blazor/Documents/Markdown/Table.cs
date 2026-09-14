using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Radzen.Documents.Markdown;

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

    internal void InsertRow(int index, TableRow row) => rows.Insert(index, row);

    internal void RemoveRow(TableRow row) => rows.Remove(row);

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.VisitTable(this);
    }

    private static readonly Regex DelimiterRegex = new(@"^\s*(\|?\s*:?-{1,}:?\s*)+(\|+\s*:?-{1,}:?\s*)*\|?\s*$");

    internal override void Close(BlockParser parser)
    {
        base.Close(parser);

        var header = rows[0];
        var headerCells = header.Cells;

        var dataLines = Value.Split('\n');
        var lineStart = 0;

        foreach (var line in dataLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var row = new TableRow();
                var cells = ParseRow(line, index => Content.ToSource(lineStart + index));

                // Trim excess cells
                var count = Math.Min(cells.Count, headerCells.Count);

                for (int cellIndex = 0; cellIndex < count; cellIndex++)
                {
                    var alignment = cellIndex < headerCells.Count ? headerCells[cellIndex].Alignment : TableCellAlignment.None;

                    row.Add(cells[cellIndex].Value, alignment, cells[cellIndex].Content);
                }

                for (int missingCellIndex = 0; missingCellIndex < header.Cells.Count - cells.Count; missingCellIndex++)
                {
                    row.Add("", TableCellAlignment.None);
                }

                rows.Add(row);
            }
            lineStart += line.Length + 1;
        }
    }

    private readonly record struct CellText(string Value, ContentMap Content);

    private static List<CellText> ParseRow(string line, Func<int, int> toSource)
    {
        var start = line.Length - line.TrimStart().Length;
        var end = line.TrimEnd().Length;

        if (end > start && line[start] == '|')
        {
            start++;
        }

        if (end > start && line[end - 1] == '|')
        {
            end--;
        }

        var cells = new List<CellText>();
        var chars = new StringBuilder();
        var indexes = new List<int>();

        void Flush(int pipe)
        {
            var first = 0;
            var last = chars.Length;

            while (first < last && char.IsWhiteSpace(chars[first]))
            {
                first++;
            }

            while (last > first && char.IsWhiteSpace(chars[last - 1]))
            {
                last--;
            }

            var content = new ContentMap();
            var runStart = first;

            if (first == last)
            {
                content.Append(0, toSource(indexes.Count > 0 ? indexes[^1] : pipe), 0);
            }

            for (var k = first + 1; k <= last; k++)
            {
                if (k == last || indexes[k] != indexes[k - 1] + 1)
                {
                    var sourceStart = toSource(indexes[runStart]);
                    content.Append(k - runStart, sourceStart, toSource(indexes[k - 1] + 1) - sourceStart);
                    runStart = k;
                }
            }

            cells.Add(new CellText(chars.ToString(first, last - first), content));
            chars.Clear();
            indexes.Clear();
        }

        var escaped = false;

        for (var i = start; i < end; i++)
        {
            var c = line[i];

            if (escaped)
            {
                if (c != '|')
                {
                    chars.Append('\\');
                    indexes.Add(i - 1);
                }

                chars.Append(c);
                indexes.Add(i);
                escaped = false;
            }
            else if (c == '\\')
            {
                escaped = true;
            }
            else if (c == '|')
            {
                Flush(i);
            }
            else
            {
                chars.Append(c);
                indexes.Add(i);
            }
        }

        if (chars.Length > 0 || cells.Count > 0)
        {
            Flush(end);
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
        if (!line.Contains('|', StringComparison.Ordinal) && !line.Contains(':', StringComparison.Ordinal))
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
            var headerCells = ParseRow(headerLine, index => paragraph.Content.ToSource(paragraph.Value.Length - paragraph.Value.TrimStart().Length + index));

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
                parser.RecordLinkReferenceDefinition(paragraph.Value[..position]);
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
                    header.Add(headerCells[cellindex].Value, alignment, headerCells[cellindex].Content);
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
