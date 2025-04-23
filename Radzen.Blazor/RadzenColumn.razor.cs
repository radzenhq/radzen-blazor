using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Radzen.Blazor
{
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
        protected override string GetComponentCssClass()
        {
            var list = new List<string>
            {
                Size != null ? $"rz-col-{GetColumnValue("Size", Size)}" : "rz-col"
            };

            if (Offset != null)
            {
                list.Add($"rz-offset-{GetColumnValue("Offset", Offset)}");
            }

            if (!string.IsNullOrEmpty(Order))
            {
                list.Add($"rz-order-{GetOrderValue("Order", Order)}");
            }

            var breakPoints = new string[] { "xs", "sm", "md", "lg", "xl", "xx" };

            var properties = GetType().GetProperties()
                .Where(p => breakPoints.Any(bp => p.Name.ToLower().EndsWith(bp)))
                .Select(p => new { p.Name, BreakPoint = string.Concat(p.Name.ToLower().TakeLast(2)), Value = p.GetValue(this) });

            foreach (var p in properties) 
            {
                if (p.Value != null)
                {
                    list.Add($"rz-{(!p.Name.StartsWith("Size") ? p.Name.ToLower().Replace(p.BreakPoint, "") + "-" : "col-")}{p.BreakPoint}-{GetColumnValue(p.Name, p.Value)}");
                }
            }

            return string.Join(" ", list);
        }

        string GetColumnValue(string name, object value)
        {
            if (name.StartsWith("Order"))
            {
                return GetOrderValue(name, value.ToString());
            }

            if ((int)value < 0 || (int)value > 12)
            {
                throw new Exception($"Property {name} value should be between 0 and 12.");
            }

            return $"{value}";
        }

        string GetOrderValue(string name, string value)
        {
            var orders = Enumerable.Range(0, 12).Select(i => $"{i}").ToArray().Concat(new string[] { "first", "last" }); 

            if (!orders.Contains(value))
            {
                throw new Exception($"Property {name} value should be between 0 and 12 or first/last.");
            }

            return value;
        }
    }
}
