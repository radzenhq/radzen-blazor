using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Linq.Dynamic.Core;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public abstract class CartesianSeries<TItem> : RadzenChartComponentBase, IChartSeries, IDisposable
    {
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

        protected Func<TItem, double> ComposeCategory(ScaleBase scale)
        {
            return scale.Compose(Category(scale));
        }

        protected Func<TItem, double> ComposeValue(ScaleBase scale)
        {
            return scale.Compose(Value);
        }

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

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment<TItem> TooltipTemplate { get; set; }

        [Parameter]
        public string CategoryProperty { get; set; }

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

        public bool ShowInLegend { get => Visible; }

        [Parameter]
        public string ValueProperty { get; set; }

        [Parameter]
        public int RenderingOrder { get; set; }

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

        [Parameter]
        public IEnumerable<TItem> Data { get; set; }

        protected IList<TItem> Items { get; set; } = new List<TItem>();

        public RadzenMarkers Markers { get; set; } = new RadzenMarkers();

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

        public abstract RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);
        public abstract string Color { get; }

        double MinValue { get; set; }
        double MaxValue { get; set; }

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

                Chart.Refresh(false);
                Chart.DisplayTooltip();
            }
        }

        protected override void Initialize()
        {
            Chart.AddSeries(this);
        }

        public virtual bool Contains(double x, double y, double tolerance)
        {
            return false;
        }

        public virtual double MeasureLegend()
        {
            return TextMeasurer.TextWidth(GetTitle());
        }

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

        protected virtual string TooltipStyle(TItem item)
        {
            return Chart.Tooltip.Style;
        }

        protected virtual string TooltipClass(TItem item)
        {
            return $"rz-series-{Chart.Series.IndexOf(this)}-tooltip";
        }

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

        private void OnLegendItemClick()
        {
            IsVisible = !IsVisible;
            Chart.Refresh();
        }

        public string GetTitle()
        {
            return String.IsNullOrEmpty(Title) ? $"Series {Chart.Series.IndexOf(this) + 1}" : Title;
        }

        protected virtual string TooltipLabel(TItem item)
        {
            return GetTitle();
        }

        protected virtual string TooltipTitle(TItem item)
        {
            var category = Category(Chart.CategoryScale);
            return Chart.CategoryAxis.Format(Chart.CategoryScale, Chart.CategoryScale.Value(category(item)));
        }

        protected virtual string TooltipValue(TItem item)
        {
            return Chart.ValueAxis.Format(Chart.ValueScale, Chart.ValueScale.Value(Value(item)));
        }

        protected virtual double TooltipX(TItem item)
        {
            var category = Category(Chart.CategoryScale);
            return Chart.CategoryScale.Scale(category(item), true);
        }

        protected virtual double TooltipY(TItem item)
        {
            return Chart.ValueScale.Scale(Value(item), true);
        }

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

        protected string PickColor(int index, IEnumerable<string> colors, string defaultValue = null)
        {
            if (colors == null || !colors.Any())
            {
                return defaultValue;
            }

            return colors.ElementAt(index % colors.Count());
        }

        public void Dispose()
        {
            Chart?.RemoveSeries(this);
        }

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