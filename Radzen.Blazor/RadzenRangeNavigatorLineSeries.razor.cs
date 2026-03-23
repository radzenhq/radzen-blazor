using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders a filled area with a line stroke inside a <see cref="RadzenRangeNavigator" />.
    /// </summary>
    /// <typeparam name="TItem">The type of the data items.</typeparam>
    public partial class RadzenRangeNavigatorLineSeries<TItem> : ComponentBase, IRangeNavigatorSeries, IDisposable
    {
        /// <summary>
        /// Gets or sets the parent navigator.
        /// </summary>
        [CascadingParameter]
        public RadzenRangeNavigator? Navigator { get; set; }

        /// <summary>
        /// Gets or sets the data source.
        /// </summary>
        [Parameter]
        public IEnumerable<TItem>? Data { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the category (X axis) values.
        /// </summary>
        [Parameter]
        public string? CategoryProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the value (Y axis) values.
        /// </summary>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the stroke (line) color.
        /// </summary>
        [Parameter]
        public string Stroke { get; set; } = "currentColor";

        /// <summary>
        /// Gets or sets the area fill color.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets the stroke width.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether to use smooth (spline) interpolation.
        /// </summary>
        [Parameter]
        public bool Smooth { get; set; }

        /// <summary>
        /// Gets or sets the fill opacity.
        /// </summary>
        [Parameter]
        public double FillOpacity { get; set; } = 0.3;

        private IList<TItem> Items { get; set; } = new List<TItem>();

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (Data != null)
            {
                Items = Data.ToList();
            }

            Navigator?.AddSeries(this);
        }

        /// <inheritdoc />
        public ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items == null || !Items.Any())
            {
                return scale;
            }

            scale.Input.MergeWidth(ScaleRange.From(Items, Category(scale)));

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

            Func<TItem, object> categoryGetter = string.IsNullOrEmpty(CategoryProperty) ? (item) => string.Empty : PropertyAccess.Getter<TItem, object>(CategoryProperty);
            var data = Items.Select(categoryGetter).ToList();

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
        public ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items != null && Items.Any())
            {
                scale.Input.MergeWidth(ScaleRange.From(Items, Value));
            }

            return scale;
        }

        /// <inheritdoc />
        public RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale)
        {
            return builder =>
            {
                if (Items == null || !Items.Any())
                {
                    return;
                }

                var category = categoryScale.Compose(Category(categoryScale));
                var value = valueScale.Compose(Value);

                IPathGenerator pathGenerator = Smooth ? new SplineGenerator() : new LineGenerator();

                var data = Items.Select(item => new Point
                {
                    X = category(item),
                    Y = value(item)
                }).ToList();

                var x1 = category(Items.First()).ToInvariantString();
                var x2 = category(Items.Last()).ToInvariantString();

                var valueTicks = valueScale.Ticks(0);
                var baseline = Math.Max(0, valueTicks.Start);
                var y = valueScale.Scale(baseline).ToInvariantString();

                var path = pathGenerator.Path(data);
                var area = $"M {x1} {y} {path} L {x2} {y}";
                var line = $"M {path}";

                var fill = Fill ?? Stroke;

                builder.OpenElement(0, "g");
                builder.AddAttribute(1, "class", "rz-range-nav-series");

                builder.OpenElement(2, "path");
                builder.AddAttribute(3, "d", area);
                builder.AddAttribute(4, "fill", fill);
                builder.AddAttribute(5, "stroke", "none");
                builder.AddAttribute(6, "opacity", FillOpacity.ToInvariantString());
                builder.CloseElement();

                builder.OpenElement(7, "path");
                builder.AddAttribute(8, "d", line);
                builder.AddAttribute(9, "fill", "none");
                builder.AddAttribute(10, "stroke", Stroke);
                builder.AddAttribute(11, "stroke-width", StrokeWidth.ToInvariantString());
                builder.CloseElement();

                builder.CloseElement();
            };
        }

        private Func<TItem, double> Category(ScaleBase scale)
        {
            if (IsNumeric(CategoryProperty))
            {
                return PropertyAccess.Getter<TItem, double>(CategoryProperty!);
            }

            if (IsDate(CategoryProperty))
            {
                var getter = PropertyAccess.Getter<TItem, DateTime>(CategoryProperty!);
                return (item) => getter(item).Ticks;
            }

            if (scale is OrdinalScale ordinal)
            {
                Func<TItem, object> getter = string.IsNullOrEmpty(CategoryProperty) ? (item) => string.Empty : PropertyAccess.Getter<TItem, object>(CategoryProperty);
                return (item) => ordinal.Data?.IndexOf(getter(item)) ?? -1;
            }

            return (item) => Items.IndexOf(item);
        }

        private Func<TItem, double> Value
        {
            get
            {
                if (string.IsNullOrEmpty(ValueProperty))
                {
                    throw new ArgumentException("ValueProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(ValueProperty);
            }
        }

        private bool IsDate(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            var property = PropertyAccess.GetPropertyType(typeof(TItem), propertyName);
            if (property == null)
            {
                return false;
            }

            if (PropertyAccess.IsDateOnly(property))
            {
                return false;
            }

            return PropertyAccess.IsDate(property);
        }

        private bool IsNumeric(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            var property = PropertyAccess.GetPropertyType(typeof(TItem), propertyName);
            return property != null && PropertyAccess.IsNumeric(property);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Navigator?.RemoveSeries(this);
        }
    }
}
