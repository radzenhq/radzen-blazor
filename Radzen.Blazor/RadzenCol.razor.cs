using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCard component.
    /// </summary>
    public partial class RadzenCol : RadzenFlexComponent
    {
        /// <summary>
        /// Gets or sets the XS size.
        /// </summary>
        /// <value>The XS size.</value>
        [Parameter]
        public int SizeXS { get; set; }

        /// <summary>
        /// Gets or sets the SM size.
        /// </summary>
        /// <value>The SM size.</value>
        [Parameter]
        public int SizeSM { get; set; }

        /// <summary>
        /// Gets or sets the MD size.
        /// </summary>
        /// <value>The MD size.</value>
        [Parameter]
        public int SizeMD { get; set; }

        /// <summary>
        /// Gets or sets the LG size.
        /// </summary>
        /// <value>The LG size.</value>
        [Parameter]
        public int SizeLG { get; set; }

        /// <summary>
        /// Gets or sets the XL size.
        /// </summary>
        /// <value>The XL size.</value>
        [Parameter]
        public int SizeXL { get; set; }

        /// <summary>
        /// Gets or sets the XS offset.
        /// </summary>
        /// <value>The XS offset.</value>
        [Parameter]
        public int OffsetXS { get; set; }

        /// <summary>
        /// Gets or sets the SM offset.
        /// </summary>
        /// <value>The SM offset.</value>
        [Parameter]
        public int OffsetSM { get; set; }

        /// <summary>
        /// Gets or sets the MD offset.
        /// </summary>
        /// <value>The MD offset.</value>
        [Parameter]
        public int OffsetMD { get; set; }

        /// <summary>
        /// Gets or sets the LG offset.
        /// </summary>
        /// <value>The LG offset.</value>
        [Parameter]
        public int OffsetLG { get; set; }

        /// <summary>
        /// Gets or sets the XL offset.
        /// </summary>
        /// <value>The XL offset.</value>
        [Parameter]
        public int OffsetXL { get; set; }

        /// <summary>
        /// Gets or sets the XS order.
        /// </summary>
        /// <value>The XS order.</value>
        [Parameter]
        public int OrderXS { get; set; }

        /// <summary>
        /// Gets or sets the SM order.
        /// </summary>
        /// <value>The SM order.</value>
        [Parameter]
        public int OrderSM { get; set; }

        /// <summary>
        /// Gets or sets the MD order.
        /// </summary>
        /// <value>The MD order.</value>
        [Parameter]
        public int OrderMD { get; set; }

        /// <summary>
        /// Gets or sets the LG order.
        /// </summary>
        /// <value>The LG order.</value>
        [Parameter]
        public int OrderLG { get; set; }

        /// <summary>
        /// Gets or sets the XL order.
        /// </summary>
        /// <value>The XL order.</value>
        [Parameter]
        public int OrderXL { get; set; }

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
            var list = new List<string>() { "rz-display-flex" };

            var breakPoints = new string[] { "xs", "sm", "md", "lg", "xl" };

            var properties = GetType().GetProperties()
                .Where(p => breakPoints.Any(bp => p.Name.ToLower().EndsWith(bp)))
                .Select(p => new { p.Name, BreakPoint = string.Concat(p.Name.ToLower().TakeLast(2)), Value = (int)p.GetValue(this) });

            foreach(var p in properties) 
            {
                if (p.Value != 0)
                {
                    list.Add($"rz-col-{(!p.Name.StartsWith("Size") ? p.Name.ToLower().Replace(p.BreakPoint, "") + "-" : "")}{p.BreakPoint}-{GetColumnValue(p.Name, p.Value)}");
                }
            }

            list.Add($"rz-align-items-{GetFlexCSSClass<AlignItems>(AlignItems)}");
            list.Add($"rz-justify-content-{GetFlexCSSClass<JustifyContent>(JustifyContent)}");

            return string.Join(" ", list);
        }

        int GetColumnValue(string name, int value)
        {
            if (value < 0 || value > 12)
            {
                throw new Exception($"Property {name} value should be between 0 and 12.");
            }

            return value;
        }
    }
}
