using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Linq.Dynamic.Core;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;
using System.Collections;
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
        Func<TItem, double> categoryPropertyCache;

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
                categoryPropertyCache = PropertyAccess.Getter<TItem, double>(CategoryProperty);
                return categoryPropertyCache;
            }

            if (IsDate(CategoryProperty))
            {
                var category = PropertyAccess.Getter<TItem, DateTime>(CategoryProperty);
                categoryPropertyCache = (item) => category(item).Ticks;
                return categoryPropertyCache;
            }

            if (scale is OrdinalScale ordinal)
            {
                Func<TItem, object> category = String.IsNullOrEmpty(CategoryProperty) ? (item) => string.Empty : PropertyAccess.Getter<TItem, object>(CategoryProperty);

                return (item) => ordinal.Data.IndexOf(category(item));
            }

            return (item) => Items.IndexOf(item);
        }

        /// <summary>
        /// Helper function. Used internally.
        /// </summary>
        protected Func<TItem, double> ComposeCategory(ScaleBase scale)
        {
            return scale.Compose(Category(scale));
        }

        /// <summary>
        /// Helper function. Used internally.
        /// </summary>
        protected Func<TItem, double> ComposeValue(ScaleBase scale)
        {
            return scale.Compose(Value);
        }

        /// <summary>
        /// Determines whether the property with the specified name is <see cref="DateTime" />
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if the specified property name is date; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Property {propertyName} does not exist</exception>
        protected bool IsDate(string propertyName)
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

        /// <summary>
        /// Determines whether the property with the specified name is numeric.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if the specified property name is numeric; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Property {propertyName} does not exist</exception>
        protected bool IsNumeric(string propertyName)
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
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the tooltip template.
        /// </summary>
        /// <value>The tooltip template.</value>
        [Parameter]
        public RenderFragment<TItem> TooltipTemplate { get; set; }

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
        public string CategoryProperty { get; set; }

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
        public string ValueProperty { get; set; }

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
        public IEnumerable<TItem> Data { get; set; }

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

            if (scale is OrdinalScale ordinal)
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
                    if (Data is IList<TItem>)
                    {
                        Items = Data as IList<TItem>;
                    }
                    else
                    {
                        Items = Data.ToList();
                    }

                    if (IsDate(CategoryProperty) || IsNumeric(CategoryProperty))
                    {
                        Items = Items.AsQueryable().OrderBy(CategoryProperty).ToList();
                    }
                }

                await Chart.Refresh(false);
            }
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            Chart.AddSeries(this);
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
        public virtual RenderFragment RenderTooltip(object data, double marginLeft, double marginTop, double chartHeight)
        {
            var item = (TItem)data;

            var x = TooltipX(item);
            var y = TooltipY(item);

            return builder =>
            {
                builder.OpenComponent<ChartTooltip>(0);
                builder.AddAttribute(1, nameof(ChartTooltip.X), x + marginLeft);
                builder.AddAttribute(2, nameof(ChartTooltip.Y), y + marginTop);

                builder.AddAttribute(3, nameof(ChartTooltip.ChildContent), TooltipTemplate?.Invoke(item));

                builder.AddAttribute(4, nameof(ChartTooltip.Title), TooltipTitle(item));
                builder.AddAttribute(5, nameof(ChartTooltip.Label), TooltipLabel(item));
                builder.AddAttribute(6, nameof(ChartTooltip.Value), TooltipValue(item));
                builder.AddAttribute(7, nameof(ChartTooltip.Class), TooltipClass(item));
                builder.AddAttribute(8, nameof(ChartTooltip.Style), TooltipStyle(item));
                builder.CloseComponent();
            };
        }

        /// <summary>
        /// Gets the tooltip inline style.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual string TooltipStyle(TItem item)
        {
            return Chart.Tooltip.Style;
        }

        /// <summary>
        /// Gets the tooltip CSS class.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual string TooltipClass(TItem item)
        {
            return $"rz-series-{Chart.Series.IndexOf(this)}-tooltip";
        }

        /// <inheritdoc />
        public virtual RenderFragment RenderLegendItem()
        {
            var style = new List<string>();

            if (IsVisible == false)
            {
                style.Add("text-decoration: line-through");
            }

            return builder =>
            {
                builder.OpenComponent<LegendItem>(0);
                builder.AddAttribute(1, nameof(LegendItem.Index), Chart.Series.IndexOf(this));
                builder.AddAttribute(2, nameof(LegendItem.Color), Color);
                builder.AddAttribute(3, nameof(LegendItem.MarkerType), MarkerType);
                builder.AddAttribute(4, nameof(LegendItem.Style), string.Join(";", style));
                builder.AddAttribute(5, nameof(LegendItem.MarkerSize), MarkerSize);
                builder.AddAttribute(6, nameof(LegendItem.Text), GetTitle());
                builder.AddAttribute(7, nameof(LegendItem.Click), EventCallback.Factory.Create(this, OnLegendItemClick));
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
            return Data.Select(e => Value(e)).OrderBy(e => e).Skip(Data.Count() / 2).FirstOrDefault();
        }

        /// <inheritdoc />
        public double GetMean()
        {
            return Data.Select(e => Value(e)).Average();
        }

        /// <inheritdoc />
        public double GetMode()
        {
            return Data.GroupBy(e => Value(e)).Select(g => new { Value = g.Key, Count = g.Count() }).OrderByDescending(e => e.Count).FirstOrDefault().Value;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Simple_linear_regression#Fitting_the_regression_line
        /// </summary>
        public (double a, double b) GetTrend()
        {
            double a, b;

            Func<TItem, double> X;
            Func<TItem, double> Y;
            if (Chart.ShouldInvertAxes())
            {
                X = e => Chart.CategoryScale.Scale(Value(e));
                Y = e => Chart.ValueScale.Scale(Category(Chart.ValueScale)(e));
            }
            else
            {
                X = e => Chart.CategoryScale.Scale(Category(Chart.CategoryScale)(e));
                Y = e => Chart.ValueScale.Scale(Value(e));
            }

            var avgX = Data.Select(e => X(e)).Average();
            var avgY = Data.Select(e => Y(e)).Average();
            var sumXY = Data.Sum(e => (X(e) - avgX) * (Y(e) - avgY));
            if (Chart.ShouldInvertAxes())
            {
                var sumYSq = Data.Sum(e => (Y(e) - avgY) * (Y(e) - avgY));
                b = sumXY / sumYSq;
                a = avgX - b * avgY;
            }
            else
            {
                var sumXSq = Data.Sum(e => (X(e) - avgX) * (X(e) - avgX));
                b = sumXY / sumXSq;
                a = avgY - b * avgX;
            }

            return (a, b);
        }

        private async Task OnLegendItemClick()
        {
            IsVisible = !IsVisible;

            if (Chart.LegendClick.HasDelegate)
            {
                var args = new LegendClickEventArgs
                {
                    Data = this.Data,
                    Title = GetTitle(),
                    IsVisible = IsVisible,
                };

                await Chart.LegendClick.InvokeAsync(args);

                IsVisible = args.IsVisible;
            }

            await Chart.Refresh();
        }

        /// <inheritdoc />
        public string GetTitle()
        {
            return String.IsNullOrEmpty(Title) ? $"Series {Chart.Series.IndexOf(this) + 1}" : Title;
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
            var category = Category(Chart.CategoryScale);
            return Chart.CategoryAxis.Format(Chart.CategoryScale, Chart.CategoryScale.Value(category(item)));
        }

        /// <summary>
        /// Gets the value of the tooltip displayed for this item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string TooltipValue(TItem item)
        {
            return Chart.ValueAxis.Format(Chart.ValueScale, Chart.ValueScale.Value(Value(item)));
        }

        /// <summary>
        /// Gets the X coordinate of the tooltip of the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal virtual double TooltipX(TItem item)
        {
            var category = Category(Chart.CategoryScale);
            return Chart.CategoryScale.Scale(category(item), true);
        }

        /// <summary>
        /// Gets the Y coordinate of the tooltip of the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal virtual double TooltipY(TItem item)
        {
            return Chart.ValueScale.Scale(Value(item), true);
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

                return (retObject,
                    new Point() { X = TooltipX(retObject), Y = TooltipY(retObject)});
            }

            return (null, null);
        }

        /// <inheritdoc />
        public virtual IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();

            foreach (var d in Data)
            {
                list.Add(new ChartDataLabel
                {
                    Position = new Point { X = TooltipX(d) + offsetX, Y = TooltipY(d) + offsetY },
                    TextAnchor = "middle",
                    Text = Chart.ValueAxis.Format(Chart.ValueScale, Value(d))
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
        protected string PickColor(int index, IEnumerable<string> colors, string defaultValue = null)
        {
            if (colors == null || !colors.Any())
            {
                return defaultValue;
            }

            return colors.ElementAt(index % colors.Count());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Chart?.RemoveSeries(this);
        }

        /// <inheritdoc />
        public async Task InvokeClick(EventCallback<SeriesClickEventArgs> handler, object data)
        {
            var category = Category(Chart.CategoryScale);

            await handler.InvokeAsync(new SeriesClickEventArgs
            {
                Data = data,
                Title = GetTitle(),
                Category = PropertyAccess.GetValue(data, CategoryProperty),
                Value = PropertyAccess.GetValue(data, ValueProperty),
                Point = new SeriesPoint
                {
                    Category = category((TItem)data),
                    Value = Value((TItem)data)
                }
            });
        }
    }
}