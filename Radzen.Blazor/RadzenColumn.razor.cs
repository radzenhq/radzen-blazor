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
    /// RadzenColumn component.
    /// </summary>
    public partial class RadzenColumn : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public int? Size { get; set; }

        /// <summary>
        /// Gets or sets the XS size.
        /// </summary>
        /// <value>The XS size.</value>
        [Parameter]
        public int? SizeXS { get; set; }

        /// <summary>
        /// Gets or sets the SM size.
        /// </summary>
        /// <value>The SM size.</value>
        [Parameter]
        public int? SizeSM { get; set; }

        /// <summary>
        /// Gets or sets the MD size.
        /// </summary>
        /// <value>The MD size.</value>
        [Parameter]
        public int? SizeMD { get; set; }

        /// <summary>
        /// Gets or sets the LG size.
        /// </summary>
        /// <value>The LG size.</value>
        [Parameter]
        public int? SizeLG { get; set; }

        /// <summary>
        /// Gets or sets the XL size.
        /// </summary>
        /// <value>The XL size.</value>
        [Parameter]
        public int? SizeXL { get; set; }

        /// <summary>
        /// Gets or sets the XX size.
        /// </summary>
        /// <value>The XX size.</value>
        [Parameter]
        public int? SizeXX { get; set; }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>The offset.</value>
        [Parameter]
        public int? Offset { get; set; }

        /// <summary>
        /// Gets or sets the XS offset.
        /// </summary>
        /// <value>The XS offset.</value>
        [Parameter]
        public int? OffsetXS { get; set; }

        /// <summary>
        /// Gets or sets the SM offset.
        /// </summary>
        /// <value>The SM offset.</value>
        [Parameter]
        public int? OffsetSM { get; set; }

        /// <summary>
        /// Gets or sets the MD offset.
        /// </summary>
        /// <value>The MD offset.</value>
        [Parameter]
        public int? OffsetMD { get; set; }

        /// <summary>
        /// Gets or sets the LG offset.
        /// </summary>
        /// <value>The LG offset.</value>
        [Parameter]
        public int? OffsetLG { get; set; }

        /// <summary>
        /// Gets or sets the XL offset.
        /// </summary>
        /// <value>The XL offset.</value>
        [Parameter]
        public int? OffsetXL { get; set; }

        /// <summary>
        /// Gets or sets the XX offset.
        /// </summary>
        /// <value>The XX offset.</value>
        [Parameter]
        public int? OffsetXX { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>The order.</value>
        [Parameter]
        public string Order { get; set; }

        /// <summary>
        /// Gets or sets the XS order.
        /// </summary>
        /// <value>The XS order.</value>
        [Parameter]
        public string OrderXS { get; set; }

        /// <summary>
        /// Gets or sets the SM order.
        /// </summary>
        /// <value>The SM order.</value>
        [Parameter]
        public string OrderSM { get; set; }

        /// <summary>
        /// Gets or sets the MD order.
        /// </summary>
        /// <value>The MD order.</value>
        [Parameter]
        public string OrderMD { get; set; }

        /// <summary>
        /// Gets or sets the LG order.
        /// </summary>
        /// <value>The LG order.</value>
        [Parameter]
        public string OrderLG { get; set; }

        /// <summary>
        /// Gets or sets the XL order.
        /// </summary>
        /// <value>The XL order.</value>
        [Parameter]
        public string OrderXL { get; set; }

        /// <summary>
        /// Gets or sets the XX order.
        /// </summary>
        /// <value>The XX order.</value>
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