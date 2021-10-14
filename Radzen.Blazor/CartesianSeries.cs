using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Linq.Dynamic.Core;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class of <see cref="RadzenChart" /> series.
    /// </summary>
    /// <typeparam name="TItem">The type of the series data.</typeparam>
    public abstract class CartesianSeries<TItem> : RadzenChartComponentBase, IChartSeries, IDisposable
    {
        /// <summary>
        /// Creates a getter function that returns a value from the specified category scale for the specified data item.
        /// </summary>
        /// <param name="scale">The scale.</param>
        protected Func<TItem, double> Category(ScaleBase scale)
        {
            if (IsNumeric(CategoryProperty))
            {
                return PropertyAccess.Getter<TItem, double>(CategoryProperty);
            }

            if (IsDate(CategoryProperty))
            {
                var category = PropertyAccess.Getter<TItem, DateTime>(CategoryProperty);

                return (item) => category(item).Ticks;
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
        /// The name of the property of <typeparamref name="TItem" /> that provides the X axis (a.k.a. category axis) values.
        /// </summary>
        [Parameter]
        public string CategoryProperty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CartesianSeries{TItem}"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

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
        /// Creates a getter function that returns a value from the specifid data item. Uses <see cref="ValueProperty" />.
        /// </summary>
        /// <value>The value.</value>
        /// <exception cref="ArgumentException">ValueProperty shoud not be empty</exception>
        protected Func<TItem, double> Value
        {
            get
            {
                if (String.IsNullOrEmpty(ValueProperty))
                {
                    throw new ArgumentException("ValueProperty shoud not be empty");
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

            Func<TItem, object> category = String.IsNullOrEmpty(CategoryProperty) ? (item) => string.Empty : PropertyAccess.Getter<TItem, object>(CategoryProperty);

            var data = Items.Select(category).ToList();

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
        public abstract string Color { get; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldRefresh = parameters.DidParameterChange(nameof(Data), Data);
            var visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (visibleChanged)
            {
                IsVisible = Visible;
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
        public virtual RenderFragment RenderTooltip(object data, double marginLeft, double marginTop)
        {
            var item = (TItem)data;

            var x = TooltipX(item);
            var y = TooltipY(item);

            return builder =>
            {
                builder.OpenComponent<ChartTooltip>(0);
                builder.AddAttribute(1, nameof(ChartTooltip.X), x + marginLeft);
                builder.AddAttribute(2, nameof(ChartTooltip.Y), y + marginTop);

                if (TooltipTemplate != null)
                {
                    builder.AddAttribute(3, nameof(ChartTooltip.ChildContent), TooltipTemplate(item));
                }

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

        private async Task OnLegendItemClick()
        {
            IsVisible = !IsVisible;
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
        protected virtual double TooltipX(TItem item)
        {
            var category = Category(Chart.CategoryScale);
            return Chart.CategoryScale.Scale(category(item), true);
        }

        /// <summary>
        /// Gets the Y coordinate of the tooltip of the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        protected virtual double TooltipY(TItem item)
        {
            return Chart.ValueScale.Scale(Value(item), true);
        }

        /// <inheritdoc />
        public virtual object DataAt(double x, double y)
        {
            var first = Items.FirstOrDefault();
            var last = Items.LastOrDefault();

            var category = Category(Chart.CategoryScale);

            var startX = Chart.CategoryScale.Scale(category(first), true);
            var endX = Chart.CategoryScale.Scale(category(last), true);

            var count = Math.Max(Items.Count() - 1, 1);
            var index = Convert.ToInt32((x - startX) / ((endX - startX) / count));

            return Items.ElementAtOrDefault(index);
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