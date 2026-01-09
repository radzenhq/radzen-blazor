using System;
using System.Collections.Generic;
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
    public abstract class CartesianSeries<TItem> : RadzenChartComponentBase, IChartSeries, IDisposable
    {
        /// <summary>
        /// Cache for the value returned by <see cref="Category"/> when that value is only dependent on
        /// <see cref="CategoryProperty"/>.
        /// </summary>
        Func<TItem, double>? categoryPropertyCache;

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
                categoryPropertyCache = PropertyAccess.Getter<TItem, double>(CategoryProperty!);
                return categoryPropertyCache;
            }

            if (IsDate(CategoryProperty))
            {
                var category = PropertyAccess.Getter<TItem, DateTime>(CategoryProperty!);
                categoryPropertyCache = (item) => category(item).Ticks;
                return categoryPropertyCache;
            }

            if (scale is OrdinalScale ordinal)
            {
                Func<TItem, object> category = String.IsNullOrEmpty(CategoryProperty) ? (item) => string.Empty : PropertyAccess.Getter<TItem, object>(CategoryProperty);

                return (item) => ordinal.Data?.IndexOf(category(item)) ?? -1;
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

#if NET6_0_OR_GREATER
            if(PropertyAccess.IsDateOnly(property))
            {
                return false;
            }
#endif
            return PropertyAccess.IsDate(property);
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

                return PropertyAccess.Getter<TItem, double>(ValueProperty);
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
            Func<TItem, object> category = String.IsNullOrEmpty(CategoryProperty) ? (item) => string.Empty : PropertyAccess.Getter<TItem, object>(CategoryProperty);

            return Items.Select(category).ToList();
        }

        /// <inheritdoc />
        public virtual ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items == null)
            {
                return scale;
            }

            if (Items.Any())
            {
                scale.Input.MergeWidth(ScaleRange.From(Items, Category(scale)));
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
                    Output = scale.Output
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
                if (Items.Any())
                {
                    scale.Input.MergeWidth(ScaleRange.From(Items, Value));
                }
            }

            return scale;
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
                    if (overlay.Visible)
                    {
                        builder.AddContent(1, overlay.Render(categoryScale, valueScale));
                    }
                }
                builder.CloseRegion();
            });
        }

        /// <inheritdoc />
        public abstract string Color { get; }

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
                        Items = Items.AsQueryable().OrderBy(CategoryProperty).ToList();
                    }
                }

                if (Chart != null)
                {
                    await Chart.Refresh(false);
                }
            }
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

            return $"rz-series-{chart.Series.IndexOf(this)}-tooltip";
        }

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
                builder.CloseComponent();
            };
        }

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
            var values = Items.Select(Value).OrderBy(e => e).ToList();
            if (values.Count == 0)
            {
                return 0;
            }

            return values[values.Count / 2];
        }

        /// <inheritdoc />
        public double GetMean()
        {
            return Items.Any() ? Items.Select(Value).Average() : double.NaN;
        }

        /// <inheritdoc />
        public double GetMode()
        {
            if (!Items.Any())
            {
                return double.NaN;
            }

            return Items
                .GroupBy(item => Value(item))
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

            if (Items.Any())
            {
                Func<TItem, double> X;
                Func<TItem, double> Y;
                if (chart.ShouldInvertAxes())
                {
                    var valueScale = chart.ValueScale;
                    var categoryAccessor = Category(chart.ValueScale);
                    X = e => chart.CategoryScale.Scale(Value(e));
                    Y = e => valueScale.Scale(categoryAccessor(e));
                }
                else
                {
                    var categoryAccessor = Category(chart.CategoryScale);
                    X = e => chart.CategoryScale.Scale(categoryAccessor(e));
                    Y = e => chart.ValueScale.Scale(Value(e));
                }

                var data = Items.ToList();
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
            return chart.ValueAxis.Format(chart.ValueScale, chart.ValueScale.Value(Value(item)));
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
            return chart.ValueScale.Scale(Value(item), true);
        }

        /// <inheritdoc />
        public virtual (object, Point) DataAt(double x, double y)
        {
            if (Items.Any())
            {
                var retObject = Items.Select(item =>
                {
                    var distance = Math.Abs(TooltipX(item) - x);
                    return new { Item = item, Distance = distance };
                }).Aggregate((a, b) => a.Distance < b.Distance ? a : b).Item;

                return (retObject!,
                    new Point() { X = TooltipX(retObject), Y = TooltipY(retObject)});
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
                list.Add(new ChartDataLabel
                {
                    Position = new Point { X = TooltipX(d) + offsetX, Y = TooltipY(d) + offsetY },
                    TextAnchor = "middle",
                    Text = chart.ValueAxis.Format(chart.ValueScale, Value(d))
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