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
    /// Class CartesianSeries.
    /// Implements the <see cref="Radzen.Blazor.RadzenChartComponentBase" />
    /// Implements the <see cref="Radzen.Blazor.IChartSeries" />
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.Blazor.RadzenChartComponentBase" />
    /// <seealso cref="Radzen.Blazor.IChartSeries" />
    /// <seealso cref="IDisposable" />
    public abstract class CartesianSeries<TItem> : RadzenChartComponentBase, IChartSeries, IDisposable
    {
        /// <summary>
        /// Categories the specified scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>Func&lt;TItem, System.Double&gt;.</returns>
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
        /// Composes the category.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>Func&lt;TItem, System.Double&gt;.</returns>
        protected Func<TItem, double> ComposeCategory(ScaleBase scale)
        {
            return scale.Compose(Category(scale));
        }

        /// <summary>
        /// Composes the value.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>Func&lt;TItem, System.Double&gt;.</returns>
        protected Func<TItem, double> ComposeValue(ScaleBase scale)
        {
            return scale.Compose(Value);
        }

        /// <summary>
        /// Determines whether the specified property name is date.
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
        /// Determines whether the specified property name is numeric.
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

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
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
        /// Gets or sets the category property.
        /// </summary>
        /// <value>The category property.</value>
        [Parameter]
        public string CategoryProperty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CartesianSeries{TItem}"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is visible.
        /// </summary>
        /// <value><c>true</c> if this instance is visible; otherwise, <c>false</c>.</value>
        bool IsVisible { get; set; } = true;

        /// <summary>
        /// Gets a value indicating whether this <see cref="CartesianSeries{TItem}"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        bool IChartSeries.Visible
        {
            get
            {
                return IsVisible;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [show in legend].
        /// </summary>
        /// <value><c>true</c> if [show in legend]; otherwise, <c>false</c>.</value>
        public bool ShowInLegend { get => Visible; }

        /// <summary>
        /// Gets or sets the value property.
        /// </summary>
        /// <value>The value property.</value>
        [Parameter]
        public string ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the rendering order.
        /// </summary>
        /// <value>The rendering order.</value>
        [Parameter]
        public int RenderingOrder { get; set; }

        /// <summary>
        /// Gets the value.
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
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public IEnumerable<TItem> Data { get; set; }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        protected IList<TItem> Items { get; set; } = new List<TItem>();

        /// <summary>
        /// Gets or sets the markers.
        /// </summary>
        /// <value>The markers.</value>
        public RadzenMarkers Markers { get; set; } = new RadzenMarkers();

        /// <summary>
        /// Gets the type of the marker.
        /// </summary>
        /// <value>The type of the marker.</value>
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
        /// Transforms the category scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>ScaleBase.</returns>
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

        /// <summary>
        /// Transforms the value scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>ScaleBase.</returns>
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

        /// <summary>
        /// Renders the specified category scale.
        /// </summary>
        /// <param name="categoryScale">The category scale.</param>
        /// <param name="valueScale">The value scale.</param>
        /// <returns>RenderFragment.</returns>
        public abstract RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);
        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <value>The color.</value>
        public abstract string Color { get; }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        /// <value>The minimum value.</value>
        double MinValue { get; set; }
        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        double MaxValue { get; set; }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

                    if (Items.Any())
                    {
                        MinValue = Items.Min(Value);
                        MaxValue = Items.Max(Value);
                    }
                }

                await Chart.Refresh(false);
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            Chart.AddSeries(this);
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if [contains] [the specified x]; otherwise, <c>false</c>.</returns>
        public virtual bool Contains(double x, double y, double tolerance)
        {
            return false;
        }

        /// <summary>
        /// Measures the legend.
        /// </summary>
        /// <returns>System.Double.</returns>
        public virtual double MeasureLegend()
        {
            return TextMeasurer.TextWidth(GetTitle());
        }

        /// <summary>
        /// Insides the polygon.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
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

        /// <summary>
        /// Renders the tooltip.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="marginLeft">The margin left.</param>
        /// <param name="marginTop">The margin top.</param>
        /// <returns>RenderFragment.</returns>
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
        /// Tooltips the style.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string TooltipStyle(TItem item)
        {
            return Chart.Tooltip.Style;
        }

        /// <summary>
        /// Tooltips the class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string TooltipClass(TItem item)
        {
            return $"rz-series-{Chart.Series.IndexOf(this)}-tooltip";
        }

        /// <summary>
        /// Renders the legend item.
        /// </summary>
        /// <returns>RenderFragment.</returns>
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

        /// <summary>
        /// Gets the size of the marker.
        /// </summary>
        /// <value>The size of the marker.</value>
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

        /// <summary>
        /// Called when [legend item click].
        /// </summary>
        private async Task OnLegendItemClick()
        {
            IsVisible = !IsVisible;
            await Chart.Refresh();
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetTitle()
        {
            return String.IsNullOrEmpty(Title) ? $"Series {Chart.Series.IndexOf(this) + 1}" : Title;
        }

        /// <summary>
        /// Tooltips the label.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string TooltipLabel(TItem item)
        {
            return GetTitle();
        }

        /// <summary>
        /// Tooltips the title.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string TooltipTitle(TItem item)
        {
            var category = Category(Chart.CategoryScale);
            return Chart.CategoryAxis.Format(Chart.CategoryScale, Chart.CategoryScale.Value(category(item)));
        }

        /// <summary>
        /// Tooltips the value.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string TooltipValue(TItem item)
        {
            return Chart.ValueAxis.Format(Chart.ValueScale, Chart.ValueScale.Value(Value(item)));
        }

        /// <summary>
        /// Tooltips the x.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Double.</returns>
        protected virtual double TooltipX(TItem item)
        {
            var category = Category(Chart.CategoryScale);
            return Chart.CategoryScale.Scale(category(item), true);
        }

        /// <summary>
        /// Tooltips the y.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Double.</returns>
        protected virtual double TooltipY(TItem item)
        {
            return Chart.ValueScale.Scale(Value(item), true);
        }

        /// <summary>
        /// Datas at.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Object.</returns>
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
        /// Picks the color.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>System.String.</returns>
        protected string PickColor(int index, IEnumerable<string> colors, string defaultValue = null)
        {
            if (colors == null || !colors.Any())
            {
                return defaultValue;
            }

            return colors.ElementAt(index % colors.Count());
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Chart?.RemoveSeries(this);
        }

        /// <summary>
        /// Invokes the click.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="data">The data.</param>
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