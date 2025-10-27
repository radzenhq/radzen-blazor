using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;

namespace Radzen.Blazor
{
    static class ClassListExtensions
    {
        private static int GetSize(string name, int value)
        {
            if (value < 0 || value > 12)
            {
                throw new ArgumentOutOfRangeException($"Property {name} value should be between 0 and 12.");
            }

            return value;
        }

        private static string GetOrder(string name, string value)
        {
            if (int.TryParse(value, out int result))
            {
                if (result >= 0 && result <= 12)
                {
                    return value;
                }
            }

            if (value == "first" || value == "last")
            {
                return value;
            }

            throw new ArgumentOutOfRangeException($"Property {name} value should be between 0 and 12 or first/last.");
        }

        public static ClassList AddSize(this ClassList classList, string prefix, string name, string size, int? value)
        {
            if (value.HasValue)
            {
                classList.Add($"rz-{prefix}-{size}-{GetSize(name, value.Value)}");
            }

            return classList;
        }

        public static ClassList AddSize(this ClassList classList, string prefix, string name, int? value)
        {
            if (value.HasValue)
            {
                classList.Add($"rz-{prefix}-{GetSize(name, value.Value)}");
            }

            return classList;
        }

        public static ClassList AddOrder(this ClassList classList, string prefix, string name, string size, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                classList.Add($"rz-{prefix}-{size}-{GetOrder(name, value)}");
            }

            return classList;
        }

        public static ClassList AddOrder(this ClassList classList, string prefix, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                classList.Add($"rz-{prefix}-{GetOrder(name, value)}");
            }

            return classList;
        }
    }

    /// <summary>
    /// A responsive grid column component used within RadzenRow to create flexible, responsive layouts based on a 12-column grid system.
    /// RadzenColumn provides breakpoint-specific sizing, offsetting, and ordering capabilities for building adaptive interfaces.
    /// Must be used inside a RadzenRow component. The column width is specified as a value from 1-12, representing the number of grid columns to span.
    /// Supports responsive design through breakpoint-specific properties including Size for default column width (1-12), SizeXS/SM/MD/LG/XL/XX for breakpoint-specific widths,
    /// Offset for number of columns to skip before this column (creates left margin), OffsetXS/SM/MD/LG/XL/XX for breakpoint-specific offsets,
    /// Order to control visual order of columns (useful for reordering on different screen sizes), and OrderXS/SM/MD/LG/XL/XX for breakpoint-specific ordering.
    /// Columns automatically fill available space when no size is specified, and wrap to new lines when the total exceeds 12.
    /// </summary>
    /// <example>
    /// Basic columns with fixed sizes:
    /// <code>
    /// &lt;RadzenRow&gt;
    ///     &lt;RadzenColumn Size="6"&gt;Half width&lt;/RadzenColumn&gt;
    ///     &lt;RadzenColumn Size="6"&gt;Half width&lt;/RadzenColumn&gt;
    /// &lt;/RadzenRow&gt;
    /// </code>
    /// Responsive columns that adapt to screen size:
    /// <code>
    /// &lt;RadzenRow&gt;
    ///     &lt;RadzenColumn SizeXS="12" SizeSM="6" SizeMD="4" SizeLG="3"&gt;
    ///         Responsive content: Full width on mobile, half on tablet, third on desktop, quarter on large screens
    ///     &lt;/RadzenColumn&gt;
    /// &lt;/RadzenRow&gt;
    /// </code>
    /// Columns with offset and ordering:
    /// <code>
    /// &lt;RadzenRow&gt;
    ///     &lt;RadzenColumn Size="6" Offset="3"&gt;Centered with offset&lt;/RadzenColumn&gt;
    ///     &lt;RadzenColumn Size="6" Order="2" OrderMD="1"&gt;Reordered on medium screens&lt;/RadzenColumn&gt;
    /// &lt;/RadzenRow&gt;
    /// </code>
    /// </example>
    public partial class RadzenColumn : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the default column width as a value from 1-12 in the grid system.
        /// If not specified, the column will automatically expand to fill available space.
        /// </summary>
        /// <value>The number of grid columns to span (1-12), or null for auto width.</value>
        [Parameter]
        public int? Size { get; set; }

        /// <summary>
        /// Gets or sets the column width for extra small screens (breakpoint &lt; 576px).
        /// Overrides the default Size on mobile devices.
        /// </summary>
        /// <value>The number of grid columns to span (1-12) on XS screens.</value>
        [Parameter]
        public int? SizeXS { get; set; }

        /// <summary>
        /// Gets or sets the column width for small screens (breakpoint ≥ 576px).
        /// Overrides the default Size on small tablets and larger devices.
        /// </summary>
        /// <value>The number of grid columns to span (1-12) on SM screens.</value>
        [Parameter]
        public int? SizeSM { get; set; }

        /// <summary>
        /// Gets or sets the column width for medium screens (breakpoint ≥ 768px).
        /// Overrides the default Size on tablets and larger devices.
        /// </summary>
        /// <value>The number of grid columns to span (1-12) on MD screens.</value>
        [Parameter]
        public int? SizeMD { get; set; }

        /// <summary>
        /// Gets or sets the column width for large screens (breakpoint ≥ 1024px).
        /// Overrides the default Size on desktops and larger devices.
        /// </summary>
        /// <value>The number of grid columns to span (1-12) on LG screens.</value>
        [Parameter]
        public int? SizeLG { get; set; }

        /// <summary>
        /// Gets or sets the column width for extra large screens (breakpoint ≥ 1280px).
        /// Overrides the default Size on large desktops and larger devices.
        /// </summary>
        /// <value>The number of grid columns to span (1-12) on XL screens.</value>
        [Parameter]
        public int? SizeXL { get; set; }

        /// <summary>
        /// Gets or sets the column width for extra extra large screens (breakpoint ≥ 1536px).
        /// Overrides the default Size on very large displays.
        /// </summary>
        /// <value>The number of grid columns to span (1-12) on XX screens.</value>
        [Parameter]
        public int? SizeXX { get; set; }

        /// <summary>
        /// Gets or sets the number of columns to skip before this column (left margin spacing).
        /// Creates empty space to the left by pushing the column to the right.
        /// </summary>
        /// <value>The number of grid columns to offset (0-12). Default is null (no offset).</value>
        [Parameter]
        public int? Offset { get; set; }

        /// <summary>
        /// Gets or sets the offset for extra small screens (breakpoint &lt; 576px).
        /// </summary>
        /// <value>The number of grid columns to offset (0-12) on XS screens.</value>
        [Parameter]
        public int? OffsetXS { get; set; }

        /// <summary>
        /// Gets or sets the offset for small screens (breakpoint ≥ 576px).
        /// </summary>
        /// <value>The number of grid columns to offset (0-12) on SM screens.</value>
        [Parameter]
        public int? OffsetSM { get; set; }

        /// <summary>
        /// Gets or sets the offset for medium screens (breakpoint ≥ 768px).
        /// </summary>
        /// <value>The number of grid columns to offset (0-12) on MD screens.</value>
        [Parameter]
        public int? OffsetMD { get; set; }

        /// <summary>
        /// Gets or sets the offset for large screens (breakpoint ≥ 1024px).
        /// </summary>
        /// <value>The number of grid columns to offset (0-12) on LG screens.</value>
        [Parameter]
        public int? OffsetLG { get; set; }

        /// <summary>
        /// Gets or sets the offset for extra large screens (breakpoint ≥ 1280px).
        /// </summary>
        /// <value>The number of grid columns to offset (0-12) on XL screens.</value>
        [Parameter]
        public int? OffsetXL { get; set; }

        /// <summary>
        /// Gets or sets the offset for extra extra large screens (breakpoint ≥ 1536px).
        /// </summary>
        /// <value>The number of grid columns to offset (0-12) on XX screens.</value>
        [Parameter]
        public int? OffsetXX { get; set; }

        /// <summary>
        /// Gets or sets the visual display order of this column within its row.
        /// Allows reordering columns without changing their position in markup. Values can be 0-12 or "first"/"last".
        /// </summary>
        /// <value>The column order (0-12, "first", or "last"). Default is null (document order).</value>
        [Parameter]
        public string Order { get; set; }

        /// <summary>
        /// Gets or sets the column order for extra small screens (breakpoint &lt; 576px).
        /// </summary>
        /// <value>The column order (0-12, "first", or "last") on XS screens.</value>
        [Parameter]
        public string OrderXS { get; set; }

        /// <summary>
        /// Gets or sets the column order for small screens (breakpoint ≥ 576px).
        /// </summary>
        /// <value>The column order (0-12, "first", or "last") on SM screens.</value>
        [Parameter]
        public string OrderSM { get; set; }

        /// <summary>
        /// Gets or sets the column order for medium screens (breakpoint ≥ 768px).
        /// </summary>
        /// <value>The column order (0-12, "first", or "last") on MD screens.</value>
        [Parameter]
        public string OrderMD { get; set; }

        /// <summary>
        /// Gets or sets the column order for large screens (breakpoint ≥ 1024px).
        /// </summary>
        /// <value>The column order (0-12, "first", or "last") on LG screens.</value>
        [Parameter]
        public string OrderLG { get; set; }

        /// <summary>
        /// Gets or sets the column order for extra large screens (breakpoint ≥ 1280px).
        /// </summary>
        /// <value>The column order (0-12, "first", or "last") on XL screens.</value>
        [Parameter]
        public string OrderXL { get; set; }

        /// <summary>
        /// Gets or sets the column order for extra extra large screens (breakpoint ≥ 1536px).
        /// </summary>
        /// <value>The column order (0-12, "first", or "last") on XX screens.</value>
        [Parameter]
        public string OrderXX { get; set; }

        /// <summary>
        /// Gets the final CSS style rendered by the component. Combines it with a <c>style</c> custom attribute.
        /// </summary>
        protected string GetStyle()
        {
            if (Attributes != null && Attributes.TryGetValue("style", out var style) && !string.IsNullOrEmpty(Convert.ToString(@style)))
            {
                return $"{GetComponentStyle()} {@style}";
            }

            return GetComponentStyle();
        }

        /// <summary>
        /// Gets the component CSS style.
        /// </summary>
        protected string GetComponentStyle()
        {
            return $"{Style}{(!string.IsNullOrEmpty(Style) && !Style.EndsWith(";") ? ";" : "")}";
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create()
                .Add("rz-col", Size == null)
                .AddSize("col", nameof(Size), Size)
                .AddSize("col", nameof(SizeXS), "xs", SizeXS)
                .AddSize("col", nameof(SizeSM), "sm", SizeSM)
                .AddSize("col", nameof(SizeMD), "md", SizeMD)
                .AddSize("col", nameof(SizeLG), "lg", SizeLG)
                .AddSize("col", nameof(SizeXL), "xl", SizeXL)
                .AddSize("col", nameof(SizeXX), "xx", SizeXX)
                .AddSize("offset", nameof(Offset), Offset)
                .AddSize("offset", nameof(OffsetXS), "xs", OffsetXS)
                .AddSize("offset", nameof(OffsetSM), "sm", OffsetSM)
                .AddSize("offset", nameof(OffsetMD), "md", OffsetMD)
                .AddSize("offset", nameof(OffsetLG), "lg", OffsetLG)
                .AddSize("offset", nameof(OffsetXL), "xl", OffsetXL)
                .AddSize("offset", nameof(OffsetXX), "xx", OffsetXX)
                .AddOrder("order", nameof(Order), Order)
                .AddOrder("order", nameof(OrderXS), "xs", OrderXS)
                .AddOrder("order", nameof(OrderSM), "sm", OrderSM)
                .AddOrder("order", nameof(OrderMD), "md", OrderMD)
                .AddOrder("order", nameof(OrderLG), "lg", OrderLG)
                .AddOrder("order", nameof(OrderXL), "xl", OrderXL)
                .AddOrder("order", nameof(OrderXX), "xx", OrderXX)
                .ToString();
    }
}