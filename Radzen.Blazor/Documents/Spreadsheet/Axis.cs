using System;
using System.Collections.Generic;

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
            InvalidateIndex();
            TriggerChange();
        }
    }

    /// <summary>
    /// Fires when an axis property changes (row/column hidden, shown, resized) or when an update batch completes via <see cref="EndUpdate"/>. Fires unconditionally on <c>EndUpdate</c> even if no state changed during the batch.
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
        InvalidateIndex();
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
            InvalidateIndex();
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
            InvalidateIndex();
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
            InvalidateIndex();
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
            InvalidateIndex();
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
            InvalidateIndex();
            TriggerChange();
        }
    }

    internal void ShiftUp(int deletedIndex)
    {
        int? Remap(int k) => k < deletedIndex ? k : k == deletedIndex ? null : k - 1;

        DictionaryShift.Remap(data, Remap);
        DictionaryShift.Remap(hidden, Remap);

        InvalidateIndex();
    }

    internal void ShiftDown(int fromIndex, int count)
    {
        int? Remap(int k) => k < fromIndex ? k : k + count;

        DictionaryShift.Remap(data, Remap);
        DictionaryShift.Remap(hidden, Remap);

        InvalidateIndex();
    }

    internal IEnumerable<int> GetCustomSizedIndices() => data.Keys;

    internal IEnumerable<int> GetHiddenIndices() => hidden;

    /// <summary>
    /// Gets the total size of the axis, including all visible items, default values for hidden items, and the offset.
    /// </summary>
    public double Total
    {
        get
        {
            var total = 0d;
            var hiddenCustomCount = 0;

            foreach (var item in data)
            {
                if (!IsHidden(item.Key))
                {
                    total += item.Value;
                }
                else
                {
                    hiddenCustomCount++;
                }
            }

            return total + size * (Count - data.Count - hidden.Count + hiddenCustomCount);
        }
    }

    private int[]? stopIndices;
    private double[]? stopPositions;
    private bool indexDirty = true;

    internal void InvalidateIndex()
    {
        indexDirty = true;
    }

    private void EnsureIndex()
    {
        if (!indexDirty)
        {
            return;
        }

        // Collect indices that need a stop: any custom-sized or hidden index in [0, Count).
        var stopSet = new SortedSet<int>();
        foreach (var key in data.Keys)
        {
            if (key >= 0 && key < count)
            {
                stopSet.Add(key);
            }
        }
        foreach (var key in hidden)
        {
            if (key >= 0 && key < count)
            {
                stopSet.Add(key);
            }
        }

        var stopCount = stopSet.Count;
        stopIndices = new int[stopCount];
        stopPositions = new double[stopCount];

        var position = 0d;
        var prevIndex = 0;
        var i = 0;
        foreach (var stop in stopSet)
        {
            // Add the default-sized contribution for indices in [prevIndex, stop).
            position += size * (stop - prevIndex);
            stopIndices[i] = stop;
            stopPositions[i] = position;

            // Add the contribution of `stop` itself if visible.
            var stopSize = data.TryGetValue(stop, out var custom) ? custom : size;
            if (!hidden.Contains(stop))
            {
                position += stopSize;
            }

            prevIndex = stop + 1;
            i++;
        }

        indexDirty = false;
    }

    /// <summary>
    /// Gets the pixel position of the start of the given index. Returns the total size when <paramref name="index"/> equals <see cref="Count"/>.
    /// </summary>
    public double GetPositionOf(int index)
    {
        if (index <= 0)
        {
            return 0;
        }

        EnsureIndex();

        var stops = stopIndices!;
        var positions = stopPositions!;

        if (stops.Length == 0)
        {
            return size * index;
        }

        // Find the largest stop index strictly less than `index`.
        var lo = 0;
        var hi = stops.Length - 1;
        var found = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            if (stops[mid] < index)
            {
                found = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (found < 0)
        {
            // `index` is before the first stop: all preceding indices are default-sized.
            return size * index;
        }

        var basePosition = positions[found];
        var baseIndex = stops[found];

        // Add the stop's own visible contribution.
        var baseSize = data.TryGetValue(baseIndex, out var custom) ? custom : size;
        if (!hidden.Contains(baseIndex))
        {
            basePosition += baseSize;
        }

        // Add default-sized indices in (baseIndex, index).
        basePosition += size * (index - baseIndex - 1);
        return basePosition;
    }

    /// <summary>
    /// Gets the index containing the given pixel position. Returns <see cref="Count"/> when <paramref name="pixel"/> is past the total size.
    /// </summary>
    public int GetIndexAt(double pixel)
    {
        if (pixel <= 0 || count == 0)
        {
            return 0;
        }

        EnsureIndex();

        var stops = stopIndices!;
        var positions = stopPositions!;
        var total = Total;

        if (pixel >= total)
        {
            return count;
        }

        if (stops.Length == 0)
        {
            if (size <= 0)
            {
                return 0;
            }

            var idx = (int)(pixel / size);
            return idx >= count ? count : idx;
        }

        // Binary search for the largest stop whose stored position is <= pixel.
        var lo = 0;
        var hi = stops.Length - 1;
        var found = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            if (positions[mid] <= pixel)
            {
                found = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (found < 0)
        {
            // pixel is before the first stop's position: all preceding indices are default-sized.
            if (size <= 0)
            {
                return 0;
            }

            var idx = (int)(pixel / size);
            return idx >= count ? count : idx;
        }

        var baseIndex = stops[found];
        var basePosition = positions[found];

        // Is `pixel` inside the stop itself?
        var stopSize = data.TryGetValue(baseIndex, out var custom) ? custom : size;
        var isHidden = hidden.Contains(baseIndex);
        var stopEnd = basePosition + (isHidden ? 0 : stopSize);

        if (pixel < stopEnd)
        {
            return baseIndex;
        }

        // Past the stop — walk default-sized indices.
        if (size <= 0)
        {
            // Avoid divide-by-zero; return the next index after the stop.
            var nextIdx = baseIndex + 1;
            return nextIdx >= count ? count : nextIdx;
        }

        var offset = pixel - stopEnd;
        var stepsBeyond = (int)(offset / size);
        var result = baseIndex + 1 + stepsBeyond;
        return result >= count ? count : result;
    }
}
