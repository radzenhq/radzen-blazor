using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using System.Linq;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;
using System.Net.Mime;
using Microsoft.AspNetCore.Components.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class of <see cref="RadzenChart" /> series.
    /// </summary>
    /// <typeparam name="TItem">The type of the series data.</typeparam>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2087, Justification = TrimMessages.DataTypePreserved)]
    public abstract class CartesianSeries<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] TItem> : RadzenChartComponentBase, IChartSeries, IChartValueAxisSeries, IChartNavigableSeries, IChartSeriesDataCache, IDisposable
    {
        /// <summary>
        /// Cache for the value returned by <see cref="Category"/> when that value is only dependent on
        /// <see cref="CategoryProperty"/>.
        /// </summary>
        Func<TItem, double>? categoryPropertyCache;

        readonly Dictionary<(string Property, bool EmptyAsZero), Func<TItem, double>> numericGetters = new();

        /// <summary>
        /// Specifies whether a <c>null</c> value contributes zero instead of leaving a gap. Stacked series set it
        /// because the items above an empty one still need a baseline.
        /// </summary>
        private protected virtual bool EmptyValueIsZero => false;

        /// <summary>
        /// Creates a getter that reads a numeric property as <see cref="double"/>. A <c>null</c> value of a nullable
        /// property is returned as <see cref="double.NaN"/>, or as zero when <paramref name="emptyAsZero"/> is set.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="emptyAsZero">Whether <c>null</c> is read as zero.</param>
        private protected Func<TItem, double> NumericGetter(string propertyName, bool emptyAsZero = false)
        {
            if (numericGetters.TryGetValue((propertyName, emptyAsZero), out var cached))
            {
                return cached;
            }

            var propertyType = PropertyAccess.GetPropertyType(typeof(TItem), propertyName);
            Func<TItem, double> result;

            if (propertyType != null && Nullable.GetUnderlyingType(propertyType) != null)
            {
                var getter = PropertyAccess.Getter<TItem, double?>(propertyName);
                var empty = emptyAsZero ? 0 : double.NaN;
                result = item => getter(item) ?? empty;
            }
            else
            {
                result = PropertyAccess.Getter<TItem, double>(propertyName);
            }

            numericGetters[(propertyName, emptyAsZero)] = result;

            return result;
        }

        /// <summary>
        /// Returns <c>true</c> when any of the specified values is <see cref="double.NaN"/>, which is how a
        /// <c>null</c> value of a nullable property is represented.
        /// </summary>
        private protected static bool IsEmpty(double first, double second = 0, double third = 0, double fourth = 0)
        {
            return double.IsNaN(first) || double.IsNaN(second) || double.IsNaN(third) || double.IsNaN(fourth);
        }

        /// <summary>
        /// Splits the points into runs that contain no empty point, so a path is drawn for each run and a
        /// <c>null</c> value leaves a gap. Returns the original list as the only run when no point is empty.
        /// </summary>
        private protected static IList<IList<Point<TItem>>> Segments(IList<Point<TItem>> points, Func<int, bool>? isEmpty = null)
        {
            isEmpty ??= index => IsEmpty(points[index].X, points[index].Y);

            var result = new List<IList<Point<TItem>>>();
            List<Point<TItem>>? segment = null;
            var empty = false;

            for (var index = 0; index < points.Count; index++)
            {
                if (isEmpty(index))
                {
                    empty = true;
                    segment = null;
                    continue;
                }

                if (segment == null)
                {
                    segment = new List<Point<TItem>>();
                    result.Add(segment);
                }

                segment.Add(points[index]);
            }

            if (!empty && points.Count > 0)
            {
                return new List<IList<Point<TItem>>> { points };
            }

            return result;
        }

        /// <summary>
        /// Merges the smallest and largest of the specified values into the scale, ignoring empty ones.
        /// </summary>
        private protected void MergeValues(ScaleBase scale, params Func<TItem, double>[] selectors)
        {
            var range = new ScaleRange();

            foreach (var selector in selectors)
            {
                var selected = ScaleRange.From(Items, selector);
                range.Start = Math.Min(range.Start, selected.Start);
                range.End = Math.Max(range.End, selected.End);
            }

            if (range.Start <= range.End)
            {
                scale.Input.MergeWidth(range);
            }
        }

        /// <summary>
        /// Returns the parent <see cref="RadzenChart"/> instance or throws an <see cref="InvalidOperationException"/> if not present.
        /// </summary>
        /// <returns>The parent <see cref="RadzenChart"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the parent chart is not set.</exception>
        protected RadzenChart RequireChart()
        {
            return Chart ?? throw new InvalidOperationException($"{GetType().Name} requires a parent RadzenChart.");
        }

        /// <summary>
        /// Creates a getter function that returns a value from the specified category scale for the specified data item.
        /// </summary>
        /// <param name="scale">The scale.</param>
        internal Func<TItem, double> Category(ScaleBase scale)
        {
            if (categoryPropertyCache != null)
            {
                return categoryPropertyCache;
            }

            if (IsNumeric(CategoryProperty))
            {
                categoryPropertyCache = NumericGetter(CategoryProperty!);
                return categoryPropertyCache;
            }

            if (IsDate(CategoryProperty))
            {
                var categoryType = PropertyAccess.GetPropertyType(typeof(TItem), CategoryProperty!);

                if (categoryType != null && Nullable.GetUnderlyingType(categoryType) != null)
                {
                    var nullableCategory = PropertyAccess.NullableDateGetter<TItem>(CategoryProperty!);
                    categoryPropertyCache = (item) => nullableCategory(item)?.Ticks ?? double.NaN;
                    return categoryPropertyCache;
                }

                var category = PropertyAccess.DateGetter<TItem>(CategoryProperty!);
                categoryPropertyCache = (item) => category(item).Ticks;
                return categoryPropertyCache;
            }

            if (scale is OrdinalScale ordinal)
            {
                if (String.IsNullOrEmpty(CategoryProperty))
                {
                    return (item) => Items.IndexOf(item);
                }

                var ordinalCategory = PropertyAccess.Getter<TItem, object>(CategoryProperty);

                return (item) => ordinal.Data?.IndexOf(ordinalCategory(item)) ?? -1;
            }

            return (item) => Items.IndexOf(item);
        }

        /// <summary>
        /// Helper function. Used internally.
        /// </summary>
        protected Func<TItem, double> ComposeCategory(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            return scale.Compose(Category(scale));
        }

        /// <summary>
        /// Helper function. Used internally.
        /// </summary>
        protected Func<TItem, double> ComposeValue(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            return scale.Compose(Value);
        }

        /// <summary>
        /// Returns the plot-relative pixel range a scale maps to: <see cref="ScaleBase.Scale(double, bool)"/> yields
        /// coordinates from 0 to <see cref="ScaleBase.OutputSize"/> regardless of the direction of the scale.
        /// Returns <c>false</c> when the scale has not been laid out yet and has no pixel extent.
        /// </summary>
        /// <param name="scale">The scale to read.</param>
        /// <param name="start">The smaller pixel coordinate, always 0.</param>
        /// <param name="end">The larger pixel coordinate.</param>
        protected static bool TryGetPlotRange(ScaleBase scale, out double start, out double end)
        {
            ArgumentNullException.ThrowIfNull(scale);

            start = 0;
            end = scale.OutputSize;

            return !double.IsNaN(end) && end > 0;
        }

        /// <summary>
        /// Returns <c>true</c> when the band <c>[position, position + size]</c> (plus the stroke width on both sides)
        /// overlaps the pixel range <c>[start, end]</c>, so a column or bar outside the zoomed view can be skipped.
        /// </summary>
        protected static bool BandIntersects(double position, double size, double strokeWidth, double start, double end)
        {
            var low = Math.Min(position, position + size) - strokeWidth;
            var high = Math.Max(position, position + size) + strokeWidth;

            return !double.IsNaN(low) && !double.IsNaN(high) && high >= start && low <= end;
        }

        /// <summary>
        /// Creates the points a line or area needs in order to draw the part of it that falls inside the pixel range of
        /// <paramref name="categoryScale"/>. When the items are in ascending category order only the items around the visible range are read;
        /// otherwise every item is read. The result is the same as passing the points of every item to <see cref="ClipToPlotRange"/>.
        /// </summary>
        /// <param name="categoryScale">The category scale whose <see cref="ScaleBase.Output"/> is the visible pixel range.</param>
        /// <param name="x">Returns the horizontal pixel of an item.</param>
        /// <param name="y">Returns the vertical pixel of an item.</param>
        /// <param name="neighbors">How many extra points to keep beyond the last point whose segment is visible.</param>
        /// <param name="clip">Whether to leave out the points outside the visible range.</param>
        private protected IList<Point<TItem>> CreatePoints(ScaleBase categoryScale, Func<TItem, double> x, Func<TItem, double> y, int neighbors, bool clip)
        {
            var first = 0;
            var last = Items.Count - 1;

            if (clip && TryGetPlotRange(categoryScale, out var start, out var end) && TryGetSortedRange(categoryScale, x, start, end, neighbors, out var visibleFirst, out var visibleLast))
            {
                first = visibleFirst;
                last = visibleLast;
            }

            IList<Point<TItem>> points = new List<Point<TItem>>(Math.Max(0, last - first + 1));

            for (var i = first; i <= last; i++)
            {
                var item = Items[i];

                points.Add(new Point<TItem> { X = x(item), Y = y(item), Data = item });
            }

            return clip ? ClipToPlotRange(points, categoryScale, neighbors) : points;
        }

        const int LevelSize = 16;

        /// <summary>
        /// Creates the same points as <see cref="CreatePoints"/> followed by <see cref="SampleMinMax"/>, without reading every visible item.
        /// The category and value of every item are read once and kept together with the lowest and the highest value of blocks of items of
        /// increasing size, so the extremes of the items that share half a pixel are found from a few blocks.
        /// Returns <c>false</c> when the data cannot be sampled this way - the series does not cache its data, the items are not in ascending
        /// category order, a value is missing or a scale is logarithmic - and the points have to be created from the items.
        /// </summary>
        /// <param name="categoryScale">The category scale whose <see cref="ScaleBase.Output"/> is the visible pixel range.</param>
        /// <param name="valueScale">The value scale.</param>
        /// <param name="value">Returns the value of an item, <see cref="double.NaN"/> when it has none.</param>
        /// <param name="neighbors">How many extra points to keep beyond the last point whose segment is visible.</param>
        /// <param name="points">The sampled points.</param>
        private protected bool TryCreateSampledPoints(ScaleBase categoryScale, ScaleBase valueScale, Func<TItem, double> value, int neighbors, out IList<Point<TItem>> points)
        {
            points = Array.Empty<Point<TItem>>();

            var count = Items.Count;

            if (count < 2 || categoryScale.IsLogarithmic || valueScale.IsLogarithmic || !(IsNumeric(CategoryProperty) || IsDate(CategoryProperty)) ||
                !TryGetPlotRange(categoryScale, out var start, out var end) || !IsSortedByCategory(categoryScale))
            {
                return false;
            }

            var cache = GetDataCache();

            if (cache == null)
            {
                return false;
            }

            if (cache.Categories == null)
            {
                BuildSamples(cache, Category(categoryScale), value);
            }

            if (cache.HasGaps)
            {
                return false;
            }

            var categories = cache.Categories!;
            var values = cache.Values!;

            double X(int index) => categoryScale.Scale(categories[index], true);

            var firstX = X(0);
            var lastX = X(count - 1);

            if (double.IsNaN(firstX) || double.IsNaN(lastX))
            {
                return false;
            }

            var direction = lastX >= firstX ? 1 : -1;
            var low = direction > 0 ? start : -end;
            var high = direction > 0 ? end : -start;

            var from = 0;
            var to = count;

            while (from < to)
            {
                var middle = from + (to - from) / 2;

                if (direction * X(middle) >= low)
                {
                    to = middle;
                }
                else
                {
                    from = middle + 1;
                }
            }

            var firstInside = from;

            from = -1;
            to = count - 1;

            while (from < to)
            {
                var middle = from + (to - from + 1) / 2;

                if (direction * X(middle) <= high)
                {
                    from = middle;
                }
                else
                {
                    to = middle - 1;
                }
            }

            var lastInside = from;
            int first;
            int last;

            if (firstInside <= lastInside)
            {
                first = firstInside - 1;
                last = lastInside + 1;
            }
            else if (lastInside < 0 || firstInside > count - 1)
            {
                points = new List<Point<TItem>>();

                return true;
            }
            else
            {
                first = lastInside;
                last = firstInside;
            }

            first = Math.Max(0, first - neighbors);
            last = Math.Min(count - 1, last + neighbors);

            var result = new List<Point<TItem>>(Math.Min(last - first + 1, 8192));

            void Add(int index)
            {
                result.Add(new Point<TItem> { X = X(index), Y = valueScale.Scale(values[index], true), Data = Items[index] });
            }

            var current = first;

            while (current <= last)
            {
                var pixel = Math.Floor(X(current) * 2);
                var bucketEnd = current;
                var step = 1;

                while (bucketEnd + step <= last && Math.Floor(X(bucketEnd + step) * 2) == pixel)
                {
                    bucketEnd += step;
                    step *= 2;
                }

                var limit = Math.Min(last, bucketEnd + step);

                while (bucketEnd < limit)
                {
                    var middle = bucketEnd + (limit - bucketEnd + 1) / 2;

                    if (Math.Floor(X(middle) * 2) == pixel)
                    {
                        bucketEnd = middle;
                    }
                    else
                    {
                        limit = middle - 1;
                    }
                }

                var (lowest, highest) = Extremes(cache, current, bucketEnd);
                var second = Math.Min(lowest, highest);
                var third = Math.Max(lowest, highest);

                Add(current);

                if (second != current)
                {
                    Add(second);
                }

                if (third != second)
                {
                    Add(third);
                }

                if (bucketEnd != third)
                {
                    Add(bucketEnd);
                }

                current = bucketEnd + 1;
            }

            points = result;

            return true;
        }

        void BuildSamples(DataCache cache, Func<TItem, double> category, Func<TItem, double> value)
        {
            var count = Items.Count;
            var categories = new double[count];
            var values = new double[count];
            var gaps = false;

            for (var i = 0; i < count; i++)
            {
                var item = Items[i];

                categories[i] = category(item);
                values[i] = value(item);

                if (double.IsNaN(values[i]) || double.IsInfinity(values[i]))
                {
                    gaps = true;
                }
            }

            cache.Categories = categories;
            cache.Values = values;
            cache.HasGaps = gaps;

            if (gaps)
            {
                return;
            }

            var lowest = new List<int[]>();
            var highest = new List<int[]>();
            var size = count;

            while (size > LevelSize)
            {
                var blocks = (size + LevelSize - 1) / LevelSize;
                var low = new int[blocks];
                var high = new int[blocks];
                var level = lowest.Count;

                for (var block = 0; block < blocks; block++)
                {
                    var start = block * LevelSize;
                    var end = Math.Min(size, start + LevelSize);
                    var min = level == 0 ? start : lowest[level - 1][start];
                    var max = level == 0 ? start : highest[level - 1][start];

                    for (var i = start + 1; i < end; i++)
                    {
                        var candidateMin = level == 0 ? i : lowest[level - 1][i];
                        var candidateMax = level == 0 ? i : highest[level - 1][i];

                        if (values[candidateMin] < values[min])
                        {
                            min = candidateMin;
                        }

                        if (values[candidateMax] > values[max])
                        {
                            max = candidateMax;
                        }
                    }

                    low[block] = min;
                    high[block] = max;
                }

                lowest.Add(low);
                highest.Add(high);
                size = blocks;
            }

            cache.Lowest = lowest.ToArray();
            cache.Highest = highest.ToArray();
        }

        static (int Lowest, int Highest) Extremes(DataCache cache, int first, int last)
        {
            var values = cache.Values!;
            var lowest = first;
            var highest = first;
            var index = first + 1;

            while (index <= last)
            {
                var level = -1;
                var size = 1;

                for (var next = 0; next < cache.Lowest!.Length; next++)
                {
                    var nextSize = size * LevelSize;

                    if (index % nextSize != 0 || index + nextSize - 1 > last)
                    {
                        break;
                    }

                    level = next;
                    size = nextSize;
                }

                var min = level < 0 ? index : cache.Lowest![level][index / size];
                var max = level < 0 ? index : cache.Highest![level][index / size];

                if (values[min] < values[lowest])
                {
                    lowest = min;
                }

                if (values[max] > values[highest])
                {
                    highest = max;
                }

                index += size;
            }

            return (lowest, highest);
        }

        /// <summary>
        /// Specifies whether the series may remember facts about its data between renders - the category and value ranges and
        /// whether the items are in ascending category order - instead of reading every item again.
        /// The cache is dropped when <see cref="Data"/>, its number of items or the bound properties change and when <see cref="RadzenChart.Reload"/> is called.
        /// </summary>
        private protected virtual bool CachesData => false;

        sealed class DataCache
        {
            public IList<TItem>? Items;
            public int Count;
            public string? CategoryProperty;
            public string? ValueProperty;
            public bool? Sorted;
            public (double Start, double End)? CategoryRange;
            public (double Start, double End)? ValueRange;
            public double[]? Categories;
            public double[]? Values;
            public bool HasGaps;
            public int[][]? Lowest;
            public int[][]? Highest;
        }

        DataCache? dataCache;

        DataCache? GetDataCache()
        {
            if (!CachesData)
            {
                dataCache = null;

                return null;
            }

            var cache = dataCache;

            if (cache == null || !ReferenceEquals(cache.Items, Items) || cache.Count != Items.Count || cache.CategoryProperty != CategoryProperty || cache.ValueProperty != ValueProperty)
            {
                cache = dataCache = new DataCache { Items = Items, Count = Items.Count, CategoryProperty = CategoryProperty, ValueProperty = ValueProperty };
            }

            return cache;
        }

        void IChartSeriesDataCache.Invalidate()
        {
            dataCache = null;
        }

        ScaleRange CategoryRange(ScaleBase scale)
        {
            var cache = IsNumeric(CategoryProperty) || IsDate(CategoryProperty) ? GetDataCache() : null;

            if (cache == null)
            {
                return ScaleRange.From(Items, Category(scale));
            }

            if (cache.CategoryRange == null)
            {
                var range = ScaleRange.From(Items, Category(scale));

                cache.CategoryRange = (range.Start, range.End);
            }

            return new ScaleRange { Start = cache.CategoryRange.Value.Start, End = cache.CategoryRange.Value.End };
        }

        bool IsSortedByCategory(ScaleBase categoryScale)
        {
            var cache = GetDataCache();

            if (cache?.Sorted != null)
            {
                return cache.Sorted.Value;
            }

            var sorted = true;
            var category = Category(categoryScale);
            var previous = category(Items[0]);

            if (double.IsNaN(previous))
            {
                sorted = false;
            }

            for (var i = 1; sorted && i < Items.Count; i++)
            {
                var current = category(Items[i]);

                if (!(current >= previous))
                {
                    sorted = false;
                }

                previous = current;
            }

            if (cache != null)
            {
                cache.Sorted = sorted;
            }

            return sorted;
        }

        /// <summary>
        /// Finds the items whose horizontal pixel falls between <paramref name="start"/> and <paramref name="end"/>, plus one item and
        /// <paramref name="neighbors"/> more on each side. Returns <c>false</c> when the items are not in ascending category order,
        /// in which case every item has to be read.
        /// </summary>
        private protected bool TryGetSortedRange(ScaleBase categoryScale, Func<TItem, double> x, double start, double end, int neighbors, out int first, out int last)
        {
            var count = Items.Count;

            first = 0;
            last = count - 1;

            if (count < 2 || !(IsNumeric(CategoryProperty) || IsDate(CategoryProperty)))
            {
                return false;
            }

            if (!IsSortedByCategory(categoryScale))
            {
                return false;
            }

            var firstX = x(Items[0]);
            var lastX = x(Items[count - 1]);

            if (double.IsNaN(firstX) || double.IsNaN(lastX))
            {
                return false;
            }

            var direction = lastX >= firstX ? 1 : -1;
            var low = direction > 0 ? start : -end;
            var high = direction > 0 ? end : -start;

            var from = 0;
            var to = count;

            while (from < to)
            {
                var middle = from + (to - from) / 2;

                if (direction * x(Items[middle]) >= low)
                {
                    to = middle;
                }
                else
                {
                    from = middle + 1;
                }
            }

            var firstInside = from;

            from = -1;
            to = count - 1;

            while (from < to)
            {
                var middle = from + (to - from + 1) / 2;

                if (direction * x(Items[middle]) <= high)
                {
                    from = middle;
                }
                else
                {
                    to = middle - 1;
                }
            }

            var lastInside = from;

            first = Math.Clamp(Math.Min(firstInside, lastInside) - 1 - neighbors, 0, count - 1);
            last = Math.Clamp(Math.Max(firstInside, lastInside) + 1 + neighbors, 0, count - 1);

            return true;
        }

        /// <summary>
        /// Reduces consecutive points that fall on the same half of a horizontal pixel to the first, the lowest, the highest and the last of them,
        /// in their original order. Half pixels are used because the series knows neither where its pixels fall on the screen nor the
        /// pixel density of the screen. Points without a value are kept so gaps in the series are preserved.
        /// Returns the original list when no point can be dropped.
        /// </summary>
        /// <param name="points">The points in series order, in plot-relative pixels.</param>
        private protected static IList<Point<TItem>> SampleMinMax(IList<Point<TItem>> points)
        {
            ArgumentNullException.ThrowIfNull(points);

            var count = points.Count;

            if (count <= 4)
            {
                return points;
            }

            var result = new List<Point<TItem>>(Math.Min(count, 4096));
            Span<int> picked = stackalloc int[4];
            var hasBucket = false;
            var bucket = 0d;
            var first = 0;
            var last = 0;
            var min = 0;
            var max = 0;

            for (var i = 0; i < count; i++)
            {
                var point = points[i];

                if (double.IsNaN(point.X) || double.IsNaN(point.Y))
                {
                    if (hasBucket)
                    {
                        AddBucket(points, result, picked, first, min, max, last);
                        hasBucket = false;
                    }

                    result.Add(point);

                    continue;
                }

                var pixel = Math.Floor(point.X * 2);

                if (hasBucket && pixel == bucket)
                {
                    last = i;

                    if (point.Y < points[min].Y)
                    {
                        min = i;
                    }

                    if (point.Y > points[max].Y)
                    {
                        max = i;
                    }

                    continue;
                }

                if (hasBucket)
                {
                    AddBucket(points, result, picked, first, min, max, last);
                }

                hasBucket = true;
                bucket = pixel;
                first = last = min = max = i;
            }

            if (hasBucket)
            {
                AddBucket(points, result, picked, first, min, max, last);
            }

            return result.Count == count ? points : result;
        }

        static void AddBucket(IList<Point<TItem>> points, List<Point<TItem>> result, Span<int> picked, int first, int min, int max, int last)
        {
            picked[0] = first;
            picked[1] = Math.Min(min, max);
            picked[2] = Math.Max(min, max);
            picked[3] = last;

            var previous = -1;

            for (var i = 0; i < picked.Length; i++)
            {
                if (picked[i] != previous)
                {
                    previous = picked[i];
                    result.Add(points[previous]);
                }
            }
        }

        /// <summary>
        /// Returns the points a line needs in order to draw the part of it that falls inside the pixel range of
        /// <paramref name="scale"/>: every point whose segment to its neighbor reaches the range, plus
        /// <paramref name="neighbors"/> additional points on each side so the curve tangents at the edges do not change.
        /// Returns the original list when every point is needed.
        /// </summary>
        /// <param name="points">The points in series order.</param>
        /// <param name="scale">The category scale whose <see cref="ScaleBase.Output"/> is the visible pixel range.</param>
        /// <param name="neighbors">How many extra points to keep beyond the last point whose segment is visible.</param>
        protected static IList<Point<TItem>> ClipToPlotRange(IList<Point<TItem>> points, ScaleBase scale, int neighbors)
        {
            ArgumentNullException.ThrowIfNull(points);

            var count = points.Count;

            if (count == 0 || !TryGetPlotRange(scale, out var start, out var end))
            {
                return points;
            }

            var keep = new bool[count];
            var kept = 0;

            for (var i = 0; i < count; i++)
            {
                var x = points[i].X;

                if (x >= start && x <= end)
                {
                    if (!keep[i])
                    {
                        keep[i] = true;
                        kept++;
                    }
                }

                if (i > 0)
                {
                    var previous = points[i - 1].X;

                    if (Math.Max(previous, x) >= start && Math.Min(previous, x) <= end)
                    {
                        if (!keep[i - 1])
                        {
                            keep[i - 1] = true;
                            kept++;
                        }

                        if (!keep[i])
                        {
                            keep[i] = true;
                            kept++;
                        }
                    }
                }
            }

            if (neighbors > 0 && kept > 0 && kept < count)
            {
                var expanded = new bool[count];
                var lastKept = int.MinValue;

                for (var i = 0; i < count; i++)
                {
                    if (keep[i])
                    {
                        lastKept = i;
                    }

                    expanded[i] = lastKept != int.MinValue && i - lastKept <= neighbors;
                }

                lastKept = int.MaxValue;

                for (var i = count - 1; i >= 0; i--)
                {
                    if (keep[i])
                    {
                        lastKept = i;
                    }

                    if (!expanded[i] && lastKept != int.MaxValue && lastKept - i <= neighbors)
                    {
                        expanded[i] = true;
                    }
                }

                keep = expanded;
                kept = 0;

                for (var i = 0; i < count; i++)
                {
                    if (keep[i])
                    {
                        kept++;
                    }
                }
            }

            if (kept == count)
            {
                return points;
            }

            var result = new List<Point<TItem>>(kept);

            for (var i = 0; i < count; i++)
            {
                if (keep[i])
                {
                    result.Add(points[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if the ValueProperty type is nullable.
        /// </summary>
        protected bool IsValueNullable()
        {
            if (string.IsNullOrEmpty(ValueProperty))
            {
                return false;
            }

            var propertyType = PropertyAccess.GetPropertyType(typeof(TItem), ValueProperty);
            return propertyType != null && Nullable.GetUnderlyingType(propertyType) != null;
        }

        /// <summary>
        /// Creates a function that returns the raw object value of the ValueProperty (before conversion to double).
        /// Used to detect null values in nullable numeric properties.
        /// </summary>
        protected Func<TItem, object?> GetRawValueGetter()
        {
            return PropertyAccess.Getter<TItem, object?>(ValueProperty!);
        }

        /// <summary>
        /// Determines whether the property with the specified name is <see cref="DateTime" />
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if the specified property name is date; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Property {propertyName} does not exist</exception>
        protected bool IsDate(string? propertyName)
        {
            if (String.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            var property = PropertyAccess.GetPropertyType(typeof(TItem), propertyName);

            if (property == null)
            {
                throw new ArgumentException($"Property {propertyName} does not exist");
            }

            return PropertyAccess.IsDate(property);
        }

        private bool IsDateOnly(string? propertyName)
        {
            return !String.IsNullOrEmpty(propertyName) && PropertyAccess.IsDateOnly(PropertyAccess.GetPropertyType(typeof(TItem), propertyName));
        }

        /// <summary>
        /// Determines whether the property with the specified name is numeric.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if the specified property name is numeric; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Property {propertyName} does not exist</exception>
        protected bool IsNumeric(string? propertyName)
        {
            if (String.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            var property = PropertyAccess.GetPropertyType(typeof(TItem), propertyName);

            if (property == null)
            {
                throw new ArgumentException($"Property {propertyName} does not exist");
            }

            return PropertyAccess.IsNumeric(property);
        }

        /// <summary>
        /// Gets or sets the name of the value axis this series is bound to.
        /// When null or empty, the series uses the primary (default) axis.
        /// </summary>
        [Parameter]
        public string? ValueAxisName { get; set; }

        /// <inheritdoc />
        [Parameter]
        public string Title { get; set; } = null!;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the tooltip template.
        /// </summary>
        /// <value>The tooltip template.</value>
        [Parameter]
        public RenderFragment<TItem>? TooltipTemplate { get; set; }

        /// <summary>
        /// Gets the list of overlays.
        /// </summary>
        /// <value>The Overlays list.</value>
        public IList<IChartSeriesOverlay> Overlays { get; } = new List<IChartSeriesOverlay>();

        /// <summary>
        /// Gets the coordinate system of the series.
        /// </summary>
        /// <value>Coordinate system enum value.</value>
        public virtual CoordinateSystem CoordinateSystem => CoordinateSystem.Cartesian;

        /// <summary>
        /// The name of the property of <typeparamref name="TItem" /> that provides the X axis (a.k.a. category axis) values.
        /// </summary>
        [Parameter]
        public string? CategoryProperty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CartesianSeries{TItem}"/> is visible.
        /// Invisible series do not appear in the legend and cannot be shown by the user.
        /// Use the <c>Visible</c> property to programatically show or hide a series.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CartesianSeries{TItem}"/> is hidden.
        /// Hidden series are initially invisible and the user can show them by clicking on their label in the legend.
        /// Use the <c>Hidden</c> property to hide certain series from your users but still allow them to see them.
        /// </summary>
        /// <value><c>true</c> if hidden; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Hidden { get; set; }

        bool IsVisible { get; set; } = true;

        bool IChartSeries.Visible
        {
            get
            {
                return IsVisible;
            }
        }

        /// <inheritdoc />
        public bool ShowInLegend { get => Visible; }

        /// <summary>
        /// The name of the property of <typeparamref name="TItem" /> that provides the Y axis (a.k.a. value axis) values.
        /// </summary>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <inheritdoc />
        [Parameter]
        public int RenderingOrder { get; set; }

        /// <summary>
        /// Creates a getter function that returns a value from the specified data item. Uses <see cref="ValueProperty" />.
        /// </summary>
        /// <value>The value.</value>
        /// <exception cref="ArgumentException">ValueProperty should not be empty</exception>
        internal Func<TItem, double> Value
        {
            get
            {
                if (String.IsNullOrEmpty(ValueProperty))
                {
                    throw new ArgumentException("ValueProperty should not be empty");
                }

                return NumericGetter(ValueProperty, EmptyValueIsZero);
            }
        }

        /// <summary>
        /// Gets or sets the data of the series. The data is enumerated and its items are displayed by the series.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public IEnumerable<TItem>? Data { get; set; }

        /// <summary>
        /// Stores <see cref="Data" /> as an IList of <typeparamref name="TItem"/>.
        /// </summary>
        /// <value>The items.</value>
        protected IList<TItem> Items { get; set; } = new List<TItem>();

        /// <inheritdoc />
        public RadzenMarkers Markers { get; set; } = new RadzenMarkers();

        /// <inheritdoc />
        public virtual MarkerType MarkerType
        {
            get
            {
                var markerType = MarkerType.None;

                if (Markers != null)
                {
                    markerType = Markers.MarkerType;
                }

                if (markerType == MarkerType.Auto && Chart != null)
                {
                    var index = Chart.Series.IndexOf(this);

                    var types = new[] { MarkerType.Circle, MarkerType.Triangle, MarkerType.Square, MarkerType.Diamond };

                    markerType = types[index % types.Length];
                }

                return markerType;
            }
        }

        /// <summary>
        /// Returns the category values
        /// </summary>
        protected virtual IList<object> GetCategories()
        {
            if (String.IsNullOrEmpty(CategoryProperty))
            {
                return Items.Select((item, index) => (object)index).ToList();
            }

            return Items.Select(PropertyAccess.Getter<TItem, object>(CategoryProperty)).ToList();
        }

        /// <inheritdoc />
        public virtual ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items == null)
            {
                return scale;
            }

            if (Items.Count > 0)
            {
                scale.Input.MergeWidth(CategoryRange(scale));
            }

            if (IsNumeric(CategoryProperty))
            {
                return scale;
            }

            if (IsDate(CategoryProperty))
            {
                return new DateScale
                {
                    Input = scale.Input,
                    Output = scale.Output,
                    WholeDays = IsDateOnly(CategoryProperty) && (scale is not DateScale existing || existing.WholeDays)
                };
            }

            var data = GetCategories();

            if (scale is OrdinalScale ordinal && ordinal.Data != null)
            {
                foreach (var item in ordinal.Data)
                {
                    if (!data.Contains(item))
                    {
                        var index = ordinal.Data.IndexOf(item);
                        if (index <= data.Count)
                        {
                            data.Insert(index, item);
                        }
                        else
                        {
                            data.Add(item);
                        }
                    }
                }
            }

            return new OrdinalScale
            {
                Data = data,
                Input = scale.Input,
                Output = scale.Output,
            };
        }

        /// <inheritdoc />
        public virtual ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items != null)
            {
                var cache = GetDataCache();

                if (cache == null)
                {
                    MergeValues(scale, Value);
                }
                else
                {
                    if (cache.ValueRange == null)
                    {
                        var range = ScaleRange.From(Items, Value);

                        cache.ValueRange = (range.Start, range.End);
                    }

                    if (cache.ValueRange.Value.Start <= cache.ValueRange.Value.End)
                    {
                        scale.Input.MergeWidth(new ScaleRange { Start = cache.ValueRange.Value.Start, End = cache.ValueRange.Value.End });
                    }
                }
            }

            IncludeZeroInValueScale(scale, false);

            return scale;
        }

        /// <summary>
        /// Extends the specified value scale to zero when this series or another series on the same scale requires it: when the
        /// <see cref="RadzenValueAxis.Range" /> of the value axis is <see cref="ValueAxisRange.IncludeZero" />, or when it is <see cref="ValueAxisRange.Auto" />
        /// and a series is zero based outside a <see cref="RadzenSparkline" />. Call it after merging the values of this series into the scale. A scale whose values are all equal is not extended
        /// because its ticks already extend from zero. Logarithmic scales and scales without data are left unchanged.
        /// </summary>
        /// <param name="scale">The value scale.</param>
        /// <param name="zeroBased">Whether this series is filled from a zero baseline, as column, bar and area series are.</param>
        protected void IncludeZeroInValueScale(ScaleBase scale, bool zeroBased)
        {
            ArgumentNullException.ThrowIfNull(scale);

            var range = Chart?.GetValueAxis(ValueAxisName).Range ?? ValueAxisRange.Auto;

            if (range == ValueAxisRange.IncludeZero || (range == ValueAxisRange.Auto && zeroBased && Chart?.IncludesZeroForBaselineSeries != false))
            {
                scale.IncludeZero = true;
            }

            if (!scale.IncludeZero || scale.IsLogarithmic || !double.IsFinite(scale.Input.Start) || !double.IsFinite(scale.Input.End) || scale.Input.Start == scale.Input.End)
            {
                return;
            }

            scale.Input.MergeWidth(new ScaleRange { Start = 0, End = 0 });
        }

        /// <inheritdoc />
        public abstract RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);

        /// <inheritdoc />
        public RenderFragment RenderOverlays(ScaleBase categoryScale, ScaleBase valueScale)
        {
            return new RenderFragment(builder =>
            {
                builder.OpenRegion(0);
                foreach (var overlay in Overlays)
                {
                    if (overlay.Visible && !overlay.RenderOnTop)
                    {
                        builder.AddContent(1, overlay.Render(categoryScale, valueScale));
                    }
                }
                builder.CloseRegion();
            });
        }

        /// <inheritdoc />
        public RenderFragment RenderTopOverlays(ScaleBase categoryScale, ScaleBase valueScale)
        {
            return new RenderFragment(builder =>
            {
                builder.OpenRegion(0);
                foreach (var overlay in Overlays)
                {
                    if (overlay.Visible && overlay.RenderOnTop)
                    {
                        builder.AddContent(1, overlay.Render(categoryScale, valueScale));
                    }
                }
                builder.CloseRegion();
            });
        }

        /// <inheritdoc />
        public abstract string Color { get; }

        /// <summary>
        /// Specifies whether the chart highlights the hovered data point of this series with an active point
        /// dot. Range-style series override this to <c>false</c> - a single dot cannot represent their value range.
        /// </summary>
        public virtual bool ShowActivePoint => true;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldRefresh = parameters.DidParameterChange(nameof(Data), Data);
            var visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);
            var hiddenChanged = parameters.DidParameterChange(nameof(Hidden), Hidden);
            var categoryChanged = parameters.DidParameterChange(nameof(CategoryProperty), CategoryProperty);

            await base.SetParametersAsync(parameters);

            if (hiddenChanged)
            {
                IsVisible = !Hidden;
                shouldRefresh = true;
            }

            if (visibleChanged)
            {
                IsVisible = Visible;
                shouldRefresh = true;
            }

            if (categoryChanged || shouldRefresh)
            {
                categoryPropertyCache = null;
                dataCache = null;
            }

            if (Data != null && Data.Count() != Items.Count)
            {
                shouldRefresh = true;
            }

            if (shouldRefresh)
            {
                if (Data != null)
                {
                    if (Data is IList<TItem> list)
                    {
                        Items = list;
                    }
                    else
                    {
                        Items = Data.ToList();
                    }

                    if (!string.IsNullOrEmpty(CategoryProperty) && (IsDate(CategoryProperty) || IsNumeric(CategoryProperty)))
                    {
                        Items = IsInCategoryOrder(Items) ? new List<TItem>(Items) : Items.AsQueryable().OrderBy(CategoryProperty).ToList();
                    }
                }

                if (Chart != null)
                {
                    await Chart.Refresh(false);
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> when the items are already in the order that sorting them by <see cref="CategoryProperty"/> would produce,
        /// so the series can copy them instead of sorting them.
        /// </summary>
        bool IsInCategoryOrder(IList<TItem> items)
        {
            if (items.Count < 2)
            {
                return true;
            }

            var category = PropertyAccess.Getter<TItem, object>(CategoryProperty!);
            var comparer = Comparer<object>.Default;
            var previous = category(items[0]);

            for (var i = 1; i < items.Count; i++)
            {
                var current = category(items[i]);

                if (comparer.Compare(previous, current) > 0)
                {
                    return false;
                }

                previous = current;
            }

            return true;
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            Chart?.AddSeries(this);
        }

        /// <inheritdoc />
        public virtual bool Contains(double x, double y, double tolerance)
        {
            return false;
        }

        /// <inheritdoc />
        public virtual double MeasureLegend()
        {
            return TextMeasurer.TextWidth(GetTitle());
        }

        /// <inheritdoc />
        public virtual IEnumerable<double> MeasureLegendItems()
        {
            return new[] { TextMeasurer.TextWidth(GetTitle()) };
        }

        /// <summary>
        /// Determines if the provided point is inside the provided polygon.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns><c>true</c> if the polygon contains the point, <c>false</c> otherwise.</returns>
        protected bool InsidePolygon(Point point, Point[] polygon)
        {
            ArgumentNullException.ThrowIfNull(point);
            ArgumentNullException.ThrowIfNull(polygon);

            var minX = polygon[0].X;
            var maxX = polygon[0].X;
            var minY = polygon[0].Y;
            var maxY = polygon[0].Y;

            for (var i = 1; i < polygon.Length; i++)
            {
                var current = polygon[i];
                minX = Math.Min(current.X, minX);
                maxX = Math.Max(current.X, maxX);
                minY = Math.Min(current.Y, minY);
                maxY = Math.Max(current.Y, maxY);
            }

            if (point.X < minX || point.X > maxX || point.Y < minY || point.Y > maxY)
            {
                return false;
            }

            bool inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
                     point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <inheritdoc />
        public virtual RenderFragment RenderTooltip(object data)
        {
            var chart = RequireChart();
            var item = (TItem)data;

            return builder =>
            {
                if (chart.Tooltip.Shared)
                {
                    var category = !string.IsNullOrEmpty(CategoryProperty) ? PropertyAccess.GetValue(item, CategoryProperty) : null;
                    if (category != null)
                    {
                        builder.OpenComponent<ChartSharedTooltip>(0);
                        builder.AddAttribute(1, nameof(ChartSharedTooltip.Class), TooltipClass(item));
                        builder.AddAttribute(2, nameof(ChartSharedTooltip.Title), TooltipTitle(item));
                        builder.AddAttribute(3, nameof(ChartSharedTooltip.ChildContent), RenderSharedTooltipItems(category));
                        builder.CloseComponent();
                    }
                }
                else
                {
                    builder.OpenComponent<ChartTooltip>(0);
                    builder.AddAttribute(1, nameof(ChartTooltip.ChildContent), TooltipTemplate?.Invoke(item));
                    builder.AddAttribute(2, nameof(ChartTooltip.Title), TooltipTitle(item));
                    builder.AddAttribute(3, nameof(ChartTooltip.Label), TooltipLabel(item));
                    builder.AddAttribute(4, nameof(ChartTooltip.Value), TooltipValue(item));
                    builder.AddAttribute(5, nameof(ChartTooltip.Class), TooltipClass(item));
                    builder.AddAttribute(6, nameof(ChartTooltip.Style), TooltipStyle(item));
                    builder.CloseComponent();
                }
            };
        }

        private RenderFragment RenderSharedTooltipItems(object category)
        {
            var chart = RequireChart();

            return builder =>
            {
                var visibleSeries = chart.Series.Where(s => s.Visible).ToList();

                foreach (var series in visibleSeries)
                {
                    builder.AddContent(1, series.RenderSharedTooltipItem(category));
                }
            };
        }

        /// <inheritdoc />
        public virtual RenderFragment RenderSharedTooltipItem(object category)
        {
            return builder =>
            {
                var item = Items.FirstOrDefault(i => !string.IsNullOrEmpty(CategoryProperty) && object.Equals(PropertyAccess.GetValue(i, CategoryProperty), category));

                if (item != null)
                {
                    builder.OpenComponent<ChartSharedTooltipItem>(0);
                    builder.AddAttribute(1, nameof(ChartSharedTooltipItem.Value), TooltipValue(item));
                    builder.AddAttribute(2, nameof(ChartSharedTooltipItem.ChildContent), TooltipTemplate?.Invoke(item));
                    builder.AddAttribute(3, nameof(ChartSharedTooltipItem.LegendItem), RenderLegendItem(false));
                    builder.CloseComponent();
                }
            };
        }

        int IChartNavigableSeries.ItemCount => Items.Count;

        object? IChartNavigableSeries.ItemAt(int index) => index >= 0 && index < Items.Count ? Items[index] : null;

        string IChartNavigableSeries.DescribeItem(object data)
        {
            if (data is not TItem item)
            {
                return string.Empty;
            }

            var parts = new[] { TooltipLabel(item), TooltipTitle(item), TooltipValue(item) };

            return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Distinct());
        }

        /// <inheritdoc />
        public Point GetTooltipPosition(object data)
        {
            var item = (TItem)data;
            var x = TooltipX(item);
            var y = TooltipY(item);

            return new Point { X = x, Y = y };
        }

        /// <summary>
        /// Gets the tooltip inline style.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual string TooltipStyle(TItem item)
        {
            return Chart?.Tooltip?.Style ?? string.Empty;
        }

        /// <summary>
        /// Gets the tooltip CSS class.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual string TooltipClass(TItem item)
        {
            var chart = Chart;
            if (chart == null)
            {
                return "rz-series-tooltip";
            }

            return chart.ColorScheme.SeriesTooltipClass(chart.Series.IndexOf(this));
        }

        internal string SeriesClass
        {
            get
            {
                var chart = Chart;

                return chart == null ? "rz-series--1" : chart.ColorScheme.SeriesClass(chart.Series.IndexOf(this));
            }
        }

        internal string SeriesItemClass(int index) => (Chart?.ColorScheme ?? default).SeriesItemClass(index);

        internal string SeriesItemTooltipClass(int index) => (Chart?.ColorScheme ?? default).SeriesItemTooltipClass(index);

        /// <inheritdoc />
        public virtual RenderFragment RenderLegendItem()
        {
            return RenderLegendItem(true);
        }

        /// <summary>
        /// Renders the legend item for this series.
        /// </summary>
        protected virtual RenderFragment RenderLegendItem(bool clickable)
        {
            var chart = RequireChart();
            var index = chart.Series.IndexOf(this);
            var style = new List<string>();

            if (IsVisible == false)
            {
                style.Add("text-decoration: line-through");
            }

            return builder =>
            {
                builder.OpenComponent<LegendItem>(0);
                builder.AddAttribute(1, nameof(LegendItem.Index), index);
                builder.AddAttribute(2, nameof(LegendItem.Color), Color);
                builder.AddAttribute(3, nameof(LegendItem.MarkerType), MarkerType);
                builder.AddAttribute(4, nameof(LegendItem.Style), string.Join(";", style));
                builder.AddAttribute(5, nameof(LegendItem.MarkerSize), MarkerSize);
                builder.AddAttribute(6, nameof(LegendItem.Text), GetTitle());
                builder.AddAttribute(7, nameof(LegendItem.Click), EventCallback.Factory.Create(this, OnLegendItemClick));
                builder.AddAttribute(8, nameof(LegendItem.Clickable), clickable);
                builder.AddAttribute(9, nameof(LegendItem.ShowLine), ShowLineInLegend);
                builder.AddAttribute(10, nameof(LegendItem.ChildContent), RenderLegendItemContent(clickable, GetTitle(), Data, Color));
                builder.AddAttribute(11, nameof(LegendItem.Class), chart.ColorScheme.ColorClass(index) ?? string.Empty);
                builder.CloseComponent();
            };
        }

        /// <summary>
        /// Renders the content of a legend item via <see cref="RadzenLegend.ItemTemplate" />.
        /// Returns <c>null</c> when there is no template or the item is not rendered in the legend (e.g. in a shared tooltip).
        /// </summary>
        internal RenderFragment? RenderLegendItemContent(bool inLegend, string text, object? data, string? color)
        {
            var template = inLegend ? Chart?.Legend?.ItemTemplate : null;

            return template?.Invoke(new LegendItemContext { Text = text, Data = data, Series = this, Color = color });
        }

        /// <summary>
        /// Specifies whether the legend swatch displays a line indicator instead of a filled marker. Overridden by line series.
        /// </summary>
        protected internal virtual bool ShowLineInLegend => false;

        /// <inheritdoc />
        public double MarkerSize
        {
            get
            {
                double markerSize = 5;

                if (Markers != null)
                {
                    markerSize = Markers.Size;
                }

                return markerSize;
            }
        }

        /// <inheritdoc />
        public double GetMedian()
        {
            var values = Items.Select(Value).Where(e => !double.IsNaN(e)).OrderBy(e => e).ToList();
            if (values.Count == 0)
            {
                return 0;
            }

            return values[values.Count / 2];
        }

        /// <inheritdoc />
        public double GetMean()
        {
            var values = Items.Select(Value).Where(e => !double.IsNaN(e)).ToList();

            return values.Count > 0 ? values.Average() : double.NaN;
        }

        /// <inheritdoc />
        public double GetMode()
        {
            var values = Items.Select(Value).Where(e => !double.IsNaN(e)).ToList();

            if (values.Count == 0)
            {
                return double.NaN;
            }

            return values
                .GroupBy(value => value)
                .Select(g => new { Value = g.Key, Count = g.Count() })
                .OrderByDescending(e => e.Count)
                .First()
                .Value;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Simple_linear_regression#Fitting_the_regression_line
        /// </summary>
        public (double a, double b) GetTrend()
        {
            double a = double.NaN, b = double.NaN;

            var chart = Chart;
            if (chart == null)
            {
                return (a, b);
            }

            if (Items.Count > 0)
            {
                Func<TItem, double> X;
                Func<TItem, double> Y;
                var vs = chart.GetValueScale(ValueAxisName);
                if (chart.ShouldInvertAxes())
                {
                    var invertedValueScale = chart.GetInvertedValueScale(ValueAxisName);
                    var categoryAccessor = Category(chart.ValueScale);
                    X = e => invertedValueScale.Scale(Value(e));
                    Y = e => chart.ValueScale.Scale(categoryAccessor(e));
                }
                else
                {
                    var categoryAccessor = Category(chart.CategoryScale);
                    X = e => chart.CategoryScale.Scale(categoryAccessor(e));
                    Y = e => vs.Scale(Value(e));
                }

                var data = Items.Where(e => !IsEmpty(X(e), Y(e))).ToList();

                if (data.Count == 0)
                {
                    return (a, b);
                }

                var avgX = data.Select(e => X(e)).Average();
                var avgY = data.Select(e => Y(e)).Average();
                var sumXY = data.Sum(e => (X(e) - avgX) * (Y(e) - avgY));
                if (chart.ShouldInvertAxes())
                {
                    var sumYSq = data.Sum(e => (Y(e) - avgY) * (Y(e) - avgY));
                    b = sumXY / sumYSq;
                    a = avgX - b * avgY;
                }
                else
                {
                    var sumXSq = data.Sum(e => (X(e) - avgX) * (X(e) - avgX));
                    b = sumXY / sumXSq;
                    a = avgY - b * avgX;
                }
            }

            return (a, b);
        }

        /// <inheritdoc />
        public IList<Point> GetScaledDataPoints()
        {
            var result = new List<Point>();
            var chart = Chart;
            if (chart == null || Items == null || Items.Count == 0)
            {
                return result;
            }

            var vs = chart.GetValueScale(ValueAxisName);
            if (chart.ShouldInvertAxes())
            {
                var invertedValueScale = chart.GetInvertedValueScale(ValueAxisName);
                var categoryAccessor = Category(chart.ValueScale);
                foreach (var item in Items)
                {
                    var px = invertedValueScale.Scale(Value(item));
                    var py = chart.ValueScale.Scale(categoryAccessor(item));

                    if (!IsEmpty(px, py))
                    {
                        result.Add(new Point { X = px, Y = py });
                    }
                }
            }
            else
            {
                var categoryAccessor = Category(chart.CategoryScale);
                foreach (var item in Items)
                {
                    var px = chart.CategoryScale.Scale(categoryAccessor(item));
                    var py = vs.Scale(Value(item));

                    if (!IsEmpty(px, py))
                    {
                        result.Add(new Point { X = px, Y = py });
                    }
                }
            }

            return result;
        }

        private async Task OnLegendItemClick()
        {
            IsVisible = !IsVisible;

            var chart = Chart;

            if (chart?.LegendClick.HasDelegate == true)
            {
                var args = new LegendClickEventArgs
                {
                    Data = this.Data,
                    Title = GetTitle(),
                    IsVisible = IsVisible,
                };

                await chart.LegendClick.InvokeAsync(args);

                IsVisible = args.IsVisible;
            }

            if (chart != null)
            {
                await chart.Refresh();
            }
        }

        /// <inheritdoc />
        public string GetTitle()
        {
            var chart = Chart;
            if (string.IsNullOrEmpty(Title))
            {
                var index = chart?.Series.IndexOf(this) ?? 0;
                return $"Series {index + 1}";
            }

            return Title;
        }

        /// <summary>
        /// Gets the label of the tooltip displayed for this item.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual string TooltipLabel(TItem item)
        {
            return GetTitle();
        }

        /// <summary>
        /// Gets the title of the tooltip displayed for this item.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual string TooltipTitle(TItem item)
        {
            var chart = RequireChart();
            var category = Category(chart.CategoryScale);
            return chart.CategoryAxis.Format(chart.CategoryScale, chart.CategoryScale.Value(category(item)));
        }

        /// <summary>
        /// Gets the value of the tooltip displayed for this item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string TooltipValue(TItem item)
        {
            var chart = RequireChart();
            var valueAxis = chart.GetValueAxis(ValueAxisName);
            var valueScale = chart.GetValueScale(ValueAxisName);
            return valueAxis.Format(valueScale, valueScale.Value(Value(item)));
        }

        /// <summary>
        /// Gets the X coordinate of the tooltip of the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal virtual double TooltipX(TItem item)
        {
            var chart = RequireChart();
            var category = Category(chart.CategoryScale);
            return chart.CategoryScale.Scale(category(item), true);
        }

        /// <summary>
        /// Gets the Y coordinate of the tooltip of the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal virtual double TooltipY(TItem item)
        {
            var chart = RequireChart();
            return chart.GetValueScale(ValueAxisName).Scale(Value(item), true);
        }

        /// <inheritdoc />
        public virtual (object, Point) DataAt(double x, double y)
        {
            var chart = RequireChart();
            var zoomed = chart.CategoryScale.IsZoomed;
            var plotWidth = chart.CategoryScale.OutputSize;

            TItem closest = default!;
            var found = false;
            var closestDistance = double.MaxValue;
            var firstIndex = 0;
            var lastIndex = Items.Count - 1;

            if (CachesData)
            {
                var probe = zoomed && plotWidth > 0 ? Math.Clamp(x, 0, plotWidth) : x;

                if (TryGetSortedRange(chart.CategoryScale, TooltipX, probe, probe, 0, out var firstNear, out var lastNear))
                {
                    while (lastNear < Items.Count - 1 && TooltipX(Items[lastNear + 1]) == TooltipX(Items[lastNear]))
                    {
                        lastNear++;
                    }

                    firstIndex = firstNear;
                    lastIndex = lastNear;
                }
            }

            for (var index = firstIndex; index <= lastIndex; index++)
            {
                var item = Items[index];
                var itemX = TooltipX(item);

                if (zoomed && (itemX < 0 || itemX > plotWidth))
                {
                    continue;
                }

                var distance = Math.Abs(itemX - x);

                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closest = item;
                    found = true;
                }
            }

            if (found && CachesData && TryGetSortedRange(chart.CategoryScale, TooltipX, x - 0.5, x + 0.5, 0, out var firstInColumn, out var lastInColumn))
            {
                var closestY = double.MaxValue;

                for (var index = firstInColumn; index <= lastInColumn; index++)
                {
                    var item = Items[index];
                    var itemX = TooltipX(item);

                    if (Math.Abs(itemX - x) > 0.5 || (zoomed && (itemX < 0 || itemX > plotWidth)))
                    {
                        continue;
                    }

                    var distance = Math.Abs(TooltipY(item) - y);

                    if (distance < closestY)
                    {
                        closestY = distance;
                        closest = item;
                    }
                }
            }

            if (found)
            {
                var point = new Point() { X = TooltipX(closest), Y = TooltipY(closest) };

                if (!IsEmpty(point.X, point.Y))
                {
                    return (closest!, point);
                }
            }

            return (default!, new Point());
        }

        /// <inheritdoc />
        public virtual IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var chart = RequireChart();
            var list = new List<ChartDataLabel>();

            foreach (var d in Items)
            {
                var value = Value(d);
                list.Add(new ChartDataLabel
                {
                    Position = new Point { X = TooltipX(d) + offsetX, Y = TooltipY(d) + offsetY },
                    Anchor = new Point { X = TooltipX(d), Y = TooltipY(d) },
                    Value = value,
                    TextAnchor = "middle",
                    Text = chart.GetValueAxis(ValueAxisName).Format(chart.GetValueScale(ValueAxisName), value)
                });
            }

            return list;
        }

        /// <inheritdoc />
        public virtual IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY, DataLabelPosition position)
        {
            if (position == DataLabelPosition.Auto)
            {
                return GetDataLabels(offsetX, offsetY);
            }

            var chart = RequireChart();
            var list = new List<ChartDataLabel>();
            const double gap = 16;

            foreach (var d in Items)
            {
                var anchorX = TooltipX(d);
                var anchorY = TooltipY(d);
                var y = position switch
                {
                    DataLabelPosition.Top => anchorY - gap,
                    DataLabelPosition.Bottom => anchorY + gap,
                    _ => anchorY,
                };
                var value = Value(d);

                list.Add(new ChartDataLabel
                {
                    Position = new Point { X = anchorX + offsetX, Y = y + offsetY },
                    Anchor = new Point { X = anchorX, Y = anchorY },
                    Value = value,
                    TextAnchor = "middle",
                    Text = chart.GetValueAxis(ValueAxisName).Format(chart.GetValueScale(ValueAxisName), value)
                });
            }

            return list;
        }

        /// <summary>
        /// Returns a color from the specified list of colors. Rotates colors.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="colorRange">The color range value.</param>
        /// <param name="value">The value of the item.</param>
        protected string? PickColor(int index, IEnumerable<string>? colors, string? defaultValue = null, IList<SeriesColorRange>? colorRange = null, double value = 0.0)
        {
            if (colorRange != null)
            {
                var result = colorRange.Where(r => r.Min <= value && r.Max >= value).FirstOrDefault<SeriesColorRange>();
                return result?.Color ?? defaultValue;
            }
            else
            {
                if (colors == null || !colors.Any())
                {
                    return defaultValue;
                }

                return colors.ElementAt(index % colors.Count());
            }
        }
        /// <inheritdoc />
        public void Dispose()
        {
            Chart?.RemoveSeries(this);
        }

        /// <inheritdoc />
        public async Task InvokeClick(EventCallback<SeriesClickEventArgs> handler, object data)
        {
            var chart = RequireChart();
            var category = Category(chart.CategoryScale);
            var dataItem = (TItem)data;

            await handler.InvokeAsync(new SeriesClickEventArgs
            {
                Data = data,
                Title = GetTitle(),
                Category = !string.IsNullOrEmpty(CategoryProperty) ? PropertyAccess.GetValue(data, CategoryProperty) : null,
                Value = !string.IsNullOrEmpty(ValueProperty) ? PropertyAccess.GetValue(data, ValueProperty) : null,
                Point = new SeriesPoint
                {
                    Category = category(dataItem),
                    Value = Value(dataItem)
                }
            });
        }
    }
}