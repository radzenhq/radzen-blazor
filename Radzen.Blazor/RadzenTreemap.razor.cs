using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A standalone treemap chart component that displays hierarchical data as nested rectangles.
    /// </summary>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenTreemap : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the data items.
        /// </summary>
        [Parameter]
        public IEnumerable<object>? Data { get; set; }

        /// <summary>
        /// Gets or sets the property name for the numeric value (determines rectangle area).
        /// </summary>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the property name for the display text.
        /// </summary>
        [Parameter]
        public string? TextProperty { get; set; }

        /// <summary>
        /// Gets or sets custom colors for treemap items.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Colors { get; set; }

        /// <summary>
        /// Gets or sets the color scheme.
        /// </summary>
        [Parameter]
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Palette;

        /// <summary>
        /// Gets or sets the padding between rectangles in pixels.
        /// </summary>
        [Parameter]
        public double Padding { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether to show text labels.
        /// </summary>
        [Parameter]
        public bool ShowLabels { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show values.
        /// </summary>
        [Parameter]
        public bool ShowValues { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the click event callback.
        /// </summary>
        [Parameter]
        public EventCallback<TreemapClickEventArgs> ItemClick { get; set; }

        private double? Width { get; set; }
        private double? Height { get; set; }
        private DotNetObjectReference<RadzenTreemap>? reference;

        /// <summary>
        /// Resizes the treemap.
        /// </summary>
        [JSInvokable("RadzenTreemap.Resize")]
        public void Resize(double width, double height)
        {
            Width = width;
            Height = height;
            StateHasChanged();
        }

        /// <inheritdoc />
        protected override async System.Threading.Tasks.Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                reference = DotNetObjectReference.Create(this);
                var rect = await JSRuntime!.InvokeAsync<Rect>("Radzen.createResizable", Element, reference);
                Width = rect.Width;
                Height = rect.Height;
                StateHasChanged();
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-treemap rz-scheme-{ColorScheme.ToString().ToLowerInvariant()}";
        }

        internal List<TreemapLayoutItem> LayoutItems { get; set; } = new();

        internal void ComputeLayout()
        {
            LayoutItems.Clear();

            if (Data == null || !Width.HasValue || !Height.HasValue
                || string.IsNullOrEmpty(ValueProperty) || string.IsNullOrEmpty(TextProperty))
            {
                return;
            }

            var items = Data.Cast<object>()
                .Select((item, index) => new TreemapLayoutItem
                {
                    Data = item,
                    Value = Convert.ToDouble(PropertyAccess.GetValue(item, ValueProperty), System.Globalization.CultureInfo.InvariantCulture),
                    Text = PropertyAccess.GetValue(item, TextProperty)?.ToString() ?? "",
                    Index = index
                })
                .Where(i => i.Value > 0)
                .OrderByDescending(i => i.Value)
                .ToList();

            if (items.Count == 0) return;

            Squarify(items, new TreemapRect { X = 0, Y = 0, Width = Width.Value, Height = Height.Value });

            LayoutItems = items;
        }

        private void Squarify(List<TreemapLayoutItem> items, TreemapRect rect)
        {
            if (items.Count == 0) return;

            if (items.Count == 1)
            {
                items[0].X = rect.X;
                items[0].Y = rect.Y;
                items[0].Width = rect.Width;
                items[0].Height = rect.Height;
                return;
            }

            var totalValue = items.Sum(i => i.Value);
            var row = new List<TreemapLayoutItem>();
            var remaining = new List<TreemapLayoutItem>(items);

            while (remaining.Count > 0)
            {
                var item = remaining[0];
                var testRow = new List<TreemapLayoutItem>(row) { item };

                if (row.Count == 0 || WorstAspectRatio(testRow, rect, totalValue) <= WorstAspectRatio(row, rect, totalValue))
                {
                    row.Add(item);
                    remaining.RemoveAt(0);
                }
                else
                {
                    var usedRect = LayoutRow(row, rect, totalValue);
                    rect = RemainingRect(rect, usedRect);
                    totalValue -= row.Sum(i => i.Value);
                    row.Clear();
                }
            }

            if (row.Count > 0)
            {
                LayoutRow(row, rect, totalValue);
            }
        }

        private double WorstAspectRatio(List<TreemapLayoutItem> row, TreemapRect rect, double totalValue)
        {
            if (row.Count == 0 || totalValue == 0) return double.MaxValue;

            var rowSum = row.Sum(i => i.Value);
            var areaFraction = rowSum / totalValue;
            var isWide = rect.Width >= rect.Height;

            double stripLength = isWide ? rect.Height : rect.Width;
            double stripThickness = isWide
                ? rect.Width * areaFraction
                : rect.Height * areaFraction;

            if (stripThickness == 0 || stripLength == 0) return double.MaxValue;

            double worst = 0;
            foreach (var item in row)
            {
                var itemFraction = item.Value / rowSum;
                var itemLength = stripLength * itemFraction;
                var aspect = Math.Max(stripThickness / itemLength, itemLength / stripThickness);
                worst = Math.Max(worst, aspect);
            }

            return worst;
        }

        private TreemapRect LayoutRow(List<TreemapLayoutItem> row, TreemapRect rect, double totalValue)
        {
            var rowSum = row.Sum(i => i.Value);
            var areaFraction = totalValue > 0 ? rowSum / totalValue : 0;
            var isWide = rect.Width >= rect.Height;

            double stripThickness;
            double offset = 0;

            if (isWide)
            {
                stripThickness = rect.Width * areaFraction;
                foreach (var item in row)
                {
                    var itemFraction = rowSum > 0 ? item.Value / rowSum : 0;
                    var itemLength = rect.Height * itemFraction;
                    item.X = rect.X;
                    item.Y = rect.Y + offset;
                    item.Width = stripThickness;
                    item.Height = itemLength;
                    offset += itemLength;
                }
                return new TreemapRect { X = rect.X, Y = rect.Y, Width = stripThickness, Height = rect.Height };
            }
            else
            {
                stripThickness = rect.Height * areaFraction;
                foreach (var item in row)
                {
                    var itemFraction = rowSum > 0 ? item.Value / rowSum : 0;
                    var itemLength = rect.Width * itemFraction;
                    item.X = rect.X + offset;
                    item.Y = rect.Y;
                    item.Width = itemLength;
                    item.Height = stripThickness;
                    offset += itemLength;
                }
                return new TreemapRect { X = rect.X, Y = rect.Y, Width = rect.Width, Height = stripThickness };
            }
        }

        private TreemapRect RemainingRect(TreemapRect rect, TreemapRect used)
        {
            if (rect.Width >= rect.Height)
            {
                return new TreemapRect
                {
                    X = rect.X + used.Width,
                    Y = rect.Y,
                    Width = rect.Width - used.Width,
                    Height = rect.Height
                };
            }
            else
            {
                return new TreemapRect
                {
                    X = rect.X,
                    Y = rect.Y + used.Height,
                    Width = rect.Width,
                    Height = rect.Height - used.Height
                };
            }
        }

        internal string PickColor(int index)
        {
            if (Colors != null)
            {
                var colorList = Colors.ToList();
                if (colorList.Count > 0)
                {
                    return colorList[index % colorList.Count];
                }
            }
            return $"var(--rz-series-{(index % 10) + 1})";
        }

        internal RenderFragment RenderSvgText(double x, double y, string textAnchor, string dominantBaseline, string style, string content)
        {
            return builder =>
            {
                builder.OpenElement(0, "text");
                builder.AddAttribute(1, "x", x.ToInvariantString());
                builder.AddAttribute(2, "y", y.ToInvariantString());
                builder.AddAttribute(3, "text-anchor", textAnchor);
                builder.AddAttribute(4, "dominant-baseline", dominantBaseline);
                builder.AddAttribute(5, "style", style);
                builder.AddContent(6, content);
                builder.CloseElement();
            };
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (IsJSRuntimeAvailable)
            {
                JSRuntime!.InvokeVoidAsync("Radzen.disposeElement", Element);
            }

            base.Dispose();
            reference?.Dispose();
        }
    }

    /// <summary>
    /// Event args for treemap item click.
    /// </summary>
    public class TreemapClickEventArgs
    {
        /// <summary>
        /// The clicked data item.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// The text of the clicked item.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// The value of the clicked item.
        /// </summary>
        public double Value { get; set; }
    }

    internal class TreemapLayoutItem
    {
        public object Data { get; set; } = default!;
        public double Value { get; set; }
        public string Text { get; set; } = "";
        public int Index { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    internal class TreemapRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
