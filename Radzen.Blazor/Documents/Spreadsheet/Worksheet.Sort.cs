using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;
#nullable enable

/// <summary>
/// Specifies the value the sort comparison runs against — the cell's stored
/// value, its background color, or its font color.
/// </summary>
public enum SortOn
{
    /// <summary>Compare the cell's stored value (default).</summary>
    Values,
    /// <summary>Group cells whose <c>BackgroundColor</c> matches <see cref="SortKey.CustomColor"/>.</summary>
    CellColor,
    /// <summary>Group cells whose <c>Color</c> (font color) matches <see cref="SortKey.CustomColor"/>.</summary>
    FontColor,
}

/// <summary>
/// Describes one sort level used by the multi-key
/// <see cref="Worksheet.Sort(RangeRef, SortKey[])"/> overload.
/// </summary>
public sealed class SortKey
{
    /// <summary>
    /// Column index relative to the sort range's left edge (0 = first column of the range).
    /// </summary>
    public int ColumnIndex { get; init; }

    /// <summary>Ascending or descending order. Defaults to ascending.</summary>
    public SortOrder Order { get; init; } = SortOrder.Ascending;

    /// <summary>What aspect of each cell drives the comparison. Defaults to <see cref="SortOn.Values"/>.</summary>
    public SortOn SortOn { get; init; } = SortOn.Values;

    /// <summary>
    /// When set, sorts using this list's order rather than natural ordering.
    /// Items not present in the list sort after items in the list, in natural order.
    /// </summary>
    public string[]? CustomList { get; init; }

    /// <summary>
    /// When set with <see cref="SortOn.CellColor"/> or <see cref="SortOn.FontColor"/>,
    /// cells whose color matches sort first (or last when descending).
    /// </summary>
    public string? CustomColor { get; init; }

    /// <summary>
    /// When true, value comparisons are case-sensitive (ordinal). Default false (ordinal-ignore-case).
    /// </summary>
    public bool CaseSensitive { get; init; }
}

public partial class Worksheet
{
    /// <summary>
    /// Sorts the specified range using the given sort levels. Column indices in
    /// <paramref name="keys"/> are <em>relative</em> to the range's left edge.
    /// Stable: rows with equal keys keep their input order.
    /// </summary>
    public void Sort(RangeRef range, params SortKey[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (range == RangeRef.Invalid || keys.Length == 0)
        {
            return;
        }

        foreach (var key in keys)
        {
            if (key.ColumnIndex < 0 || key.ColumnIndex >= range.Columns)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(keys),
                    $"SortKey.ColumnIndex {key.ColumnIndex} is outside range columns 0..{range.Columns - 1}.");
            }
        }

        var rows = new List<(int originalIndex, List<Cell> cells)>();
        for (var r = range.Start.Row; r <= range.End.Row; r++)
        {
            rows.Add((rows.Count, Cells.GetRow(r, range.Start.Column, range.End.Column)));
        }

        // Use OrderBy to keep stability across multiple keys.
        IOrderedEnumerable<(int originalIndex, List<Cell> cells)>? ordered = null;
        foreach (var key in keys)
        {
            var k = key;
            ordered = ordered is null
                ? rows.OrderBy(row => row, BuildComparer(k))
                : ordered.ThenBy(row => row, BuildComparer(k));
        }

        var sortedRows = ordered!.ToList();

        for (var i = 0; i < sortedRows.Count; i++)
        {
            var (_, cells) = sortedRows[i];
            var targetRow = range.Start.Row + i;
            for (var c = 0; c < cells.Count; c++)
            {
                Cells[targetRow, range.Start.Column + c].CopyFrom(cells[c]);
            }
        }
    }

    private static IComparer<(int originalIndex, List<Cell> cells)> BuildComparer(SortKey key)
    {
        return Comparer<(int originalIndex, List<Cell> cells)>.Create((a, b) =>
        {
            var ca = a.cells[key.ColumnIndex];
            var cb = b.cells[key.ColumnIndex];

            // Empty cells always go last, regardless of sort direction (Excel parity).
            // Color sort: a cell with a null target color is treated as "non-empty value".
            if (key.SortOn == SortOn.Values)
            {
                var aEmpty = ca?.Value is null;
                var bEmpty = cb?.Value is null;
                if (aEmpty && bEmpty) return 0;
                if (aEmpty) return 1;
                if (bEmpty) return -1;
            }

            int cmp = key.SortOn switch
            {
                SortOn.CellColor => CompareByColor(ca, cb, key.CustomColor, fontColor: false),
                SortOn.FontColor => CompareByColor(ca, cb, key.CustomColor, fontColor: true),
                _ => CompareByValue(ca?.Value, cb?.Value, key),
            };

            return key.Order == SortOrder.Descending ? -cmp : cmp;
        });
    }

    private static int CompareByValue(object? x, object? y, SortKey key)
    {
        if (key.CustomList is { Length: > 0 } list)
        {
            var xs = x?.ToString();
            var ys = y?.ToString();
            int xi = IndexInList(list, xs, key.CaseSensitive);
            int yi = IndexInList(list, ys, key.CaseSensitive);

            // List members come first, in list order; non-members fall through to natural compare.
            if (xi >= 0 && yi >= 0) return xi.CompareTo(yi);
            if (xi >= 0) return -1;
            if (yi >= 0) return 1;
            // Both outside the list — natural string compare
            return string.Compare(xs, ys, key.CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase);
        }

        return Compare(x, y, key.CaseSensitive);
    }

    private static int CompareByColor(Cell? a, Cell? b, string? targetColor, bool fontColor)
    {
        var ac = ColorOf(a, fontColor);
        var bc = ColorOf(b, fontColor);

        var aMatch = string.Equals(ac, targetColor, StringComparison.OrdinalIgnoreCase);
        var bMatch = string.Equals(bc, targetColor, StringComparison.OrdinalIgnoreCase);

        // Matching colors come first.
        if (aMatch == bMatch) return 0;
        return aMatch ? -1 : 1;
    }

    private static string? ColorOf(Cell? c, bool fontColor)
    {
        if (c is null) return null;
        return fontColor ? c.Format.Color : c.Format.BackgroundColor;
    }

    private static int Compare(object? x, object? y, bool caseSensitive = false)
    {
        if (x is null && y is null) return 0;
        if (x is null) return 1;
        if (y is null) return -1;

        if (x is double dx && y is double dy) return dx.CompareTo(dy);

        var sx = x.ToString();
        var sy = y.ToString();
        return string.Compare(sx, sy, caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase);
    }

    private static int Compare(object? x, object? y) => Compare(x, y, caseSensitive: false);

    private static int IndexInList(string[] list, string? value, bool caseSensitive)
    {
        if (value is null) return -1;
        var cmp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        for (var i = 0; i < list.Length; i++)
        {
            if (string.Equals(list[i], value, cmp)) return i;
        }
        return -1;
    }

    /// <summary>
    /// Single-key sort by an absolute column index. Kept for back-compat;
    /// new code should prefer the <see cref="Sort(RangeRef, SortKey[])"/> overload.
    /// </summary>
    /// <param name="range">The range to sort.</param>
    /// <param name="order">Ascending or descending.</param>
    /// <param name="keyIndex">Absolute column index of the key (must be inside <paramref name="range"/>).</param>
    public void Sort(RangeRef range, SortOrder order, int keyIndex = 0)
    {
        if (range == RangeRef.Invalid) return;

        if (keyIndex < range.Start.Column || keyIndex > range.End.Column)
        {
            throw new ArgumentOutOfRangeException(nameof(keyIndex));
        }

        Sort(range, new SortKey
        {
            ColumnIndex = keyIndex - range.Start.Column,
            Order = order,
        });
    }

}