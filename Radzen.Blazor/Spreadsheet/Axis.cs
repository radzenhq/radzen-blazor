using System;
using System.Collections.Generic;

using Radzen.Blazor.Spreadsheet;
namespace Radzen.Documents.Spreadsheet;
#nullable enable

/// <summary>
/// Represents an axis in a spreadsheet, which can be used to manage the layout of rows or columns.
/// </summary>
/// <param name="size"></param>
/// <param name="count"></param>
public class Axis(double size, int count)
{
    /// <summary>
    /// The default size of an item of the axis.
    /// </summary>
    public double Size => size;

    /// <summary>
    /// The total number of items along this axis.
    /// </summary>
    private int count = count;

    /// <summary>
    /// Gets or sets the total number of items along this axis.
    /// Setting this property triggers a change event.
    /// </summary>
    public int Count
    {
        get => count;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Count cannot be negative.");
            }

            count = value;
            TriggerChange();
        }
    }

    /// <summary>
    /// Fires when an axis property changes, such as when a row or column is hidden or shown, or when the size of a row or column changes.
    /// </summary>
    public event Action? Changed;

    private readonly Dictionary<int, double> data = [];

    private readonly HashSet<int> hidden = [];

    private bool isUpdating;

    /// <summary>
    /// Suspend updates to the axis. This is useful when making multiple changes at once to prevent unnecessary updates.
    /// </summary>
    public void BeginUpdate()
    {
        isUpdating = true;
    }

    /// <summary>
    /// Resume updates to the axis and trigger a change event.
    /// </summary>
    public void EndUpdate()
    {
        isUpdating = false;
        Changed?.Invoke();
    }

    private void TriggerChange()
    {
        if (!isUpdating)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Checks if the specified index is hidden.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool IsHidden(int index)
    {
        return hidden.Contains(index);
    }

    /// <summary>
    /// Hides the specified index.
    ///</summary>

    public void Hide(int index)
    {
        if (!IsHidden(index))
        {
            hidden.Add(index);
            TriggerChange();
        }
    }

    /// <summary>
    /// Shows the specified index if it is hidden.
    /// </summary>
    public void Show(int index)
    {
        if (IsHidden(index))
        {
            hidden.Remove(index);
            TriggerChange();
        }
    }

    /// <summary>
    /// Shows all hidden indices in the axis.
    /// </summary>
    public void ShowAll()
    {
        if (hidden.Count > 0)
        {
            hidden.Clear();
            TriggerChange();
        }
    }

    /// <summary>
    /// Gets or sets the size of the axis at the specified index.
    /// </summary>
    public double this[int index]
    {
        get
        {
            if (data.TryGetValue(index, out var value))
            {
                return value;
            }

            return size;
        }
        set
        {
            data[index] = value;
            TriggerChange();
        }
    }

    private int frozen;

    /// <summary>
    /// Gets or sets the number of frozen items in the axis.
    /// </summary>
    public int Frozen
    {
        get => frozen;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Frozen cannot be negative.");
            }

            frozen = value;
            TriggerChange();
        }
    }

    /// <summary>
    /// Shifts custom sizes and hidden state up after a row/column is deleted.
    /// Entries at the deleted index are removed; entries above are unchanged;
    /// entries below are decremented by one.
    /// </summary>
    internal void ShiftUp(int deletedIndex)
    {
        var newData = new Dictionary<int, double>();
        foreach (var kvp in data)
        {
            if (kvp.Key < deletedIndex)
            {
                newData[kvp.Key] = kvp.Value;
            }
            else if (kvp.Key > deletedIndex)
            {
                newData[kvp.Key - 1] = kvp.Value;
            }
            // kvp.Key == deletedIndex is dropped
        }
        data.Clear();
        foreach (var kvp in newData)
        {
            data[kvp.Key] = kvp.Value;
        }

        var newHidden = new HashSet<int>();
        foreach (var idx in hidden)
        {
            if (idx < deletedIndex)
            {
                newHidden.Add(idx);
            }
            else if (idx > deletedIndex)
            {
                newHidden.Add(idx - 1);
            }
            // idx == deletedIndex is dropped
        }
        hidden.Clear();
        hidden.UnionWith(newHidden);
    }

    /// <summary>
    /// Shifts custom sizes and hidden state down after rows/columns are inserted.
    /// Entries at or after the insert point are incremented by count.
    /// </summary>
    internal void ShiftDown(int fromIndex, int count)
    {
        var newData = new Dictionary<int, double>();
        foreach (var kvp in data)
        {
            if (kvp.Key < fromIndex)
            {
                newData[kvp.Key] = kvp.Value;
            }
            else
            {
                newData[kvp.Key + count] = kvp.Value;
            }
        }
        data.Clear();
        foreach (var kvp in newData)
        {
            data[kvp.Key] = kvp.Value;
        }

        var newHidden = new HashSet<int>();
        foreach (var idx in hidden)
        {
            if (idx < fromIndex)
            {
                newHidden.Add(idx);
            }
            else
            {
                newHidden.Add(idx + count);
            }
        }
        hidden.Clear();
        hidden.UnionWith(newHidden);
    }

    internal IndexRange GetIndexRange(double start, double end, bool includeFrozen = false)
    {
        var currentPosition = 0d;
        var startOffset = 0d;

        if (!includeFrozen)
        {
            if (Frozen > 0)
            {
                // Calculate position after frozen items
                for (int index = 0; index < Frozen; index++)
                {
                    if (!IsHidden(index))
                    {
                        currentPosition += this[index];
                    }
                }
            }
        }

        // Find start index - include items that start before the viewport end
        int startIndex = includeFrozen ? 0 : Frozen;

        for (; startIndex < Count - 1; startIndex++)
        {
            if (IsHidden(startIndex))
            {
                continue;
            }

            var segmentSize = this[startIndex];

            if (currentPosition + segmentSize > start)
            {
                startOffset = start - currentPosition;
                break;
            }

            currentPosition += segmentSize;
        }

        // Find end index - include items that end after the viewport start
        int endIndex;
        for (endIndex = startIndex; endIndex < Count - 1; endIndex++)
        {
            if (IsHidden(endIndex))
            {
                continue;
            }

            var segmentSize = this[endIndex];
            currentPosition += segmentSize;

            if (currentPosition >= end)
            {
                break;
            }
        }

        return new IndexRange(startIndex, endIndex, startOffset);
    }

    /// <summary>
    /// Gets the total size of the axis, including all visible items, default values for hidden items, and the offset.
    /// </summary>
    public double Total
    {
        get
        {
            var total = 0d;

            foreach (var item in data)
            {
                if (!IsHidden(item.Key))
                {
                    total += item.Value;
                }
            }

            return total + size * (Count - data.Count - hidden.Count);
        }
    }

    /// <summary>
    /// Gets the pixel range for the specified start and end indices.
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    public PixelRange GetPixelRange(int startIndex, int endIndex)
    {
        double start;
        double end;
        var currentPosition = 0d;

        for (var index = 0; index < startIndex; index++)
        {
            if (!IsHidden(index))
            {
                var segmentSize = this[index];
                currentPosition += segmentSize;
            }
        }
        start = currentPosition;

        for (var index = startIndex; index <= endIndex; index++)
        {
            if (!IsHidden(index))
            {
                var segmentSize = this[index];
                currentPosition += segmentSize;
            }
        }

        end = currentPosition;

        return new PixelRange(start, end);
    }

    /// <summary>
    /// Gets the pixel range for a single index, which is equivalent to calling GetPixelRange with the same start and end index.
    /// </summary>
    public PixelRange GetPixelRange(int startIndex) => GetPixelRange(startIndex, startIndex);
}